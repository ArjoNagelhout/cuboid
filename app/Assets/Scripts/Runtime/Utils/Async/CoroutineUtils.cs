//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cuboid.UI;

namespace Cuboid
{
    public enum StatusType
    {
        None,
        Running,
        Success,
        Failed
    }

    public class CoroutineTask<T>
    {
        public StatusType Status = StatusType.None;
        public T Result;
        public Exception Exception;

        public bool Done => Status == StatusType.Success || Status == StatusType.Failed;
        public bool Failed => Status == StatusType.Failed;
    }

    public static class CoroutineUtils
    {
        public static CustomYieldInstruction Execute<T>(this IEnumerator<object> coroutine, string identifier, out CoroutineTask<T> task)
        {
            return new Instruction<T>(coroutine, identifier, out task);
        }

        public static void StopAndClearCoroutine(MonoBehaviour component, ref IEnumerator coroutine)
        {
            if (component == null || coroutine == null) { return; }
            component.StopCoroutine(coroutine);
            coroutine = null;
        }
    }

    /// <summary>
    /// Custom yield instruction that allows for multiple operations to call the same coroutine,
    /// where the coroutine only gets run the first time, and the other calls will simply
    /// wait on the OnCompleted action to get the result of the singular coroutine.
    /// </summary>
    /// <typeparam name="T">Result type of the task</typeparam>
    public class Instruction<T> : CustomYieldInstruction
    {
        private class CoroutineData
        {
            public IEnumerator<object> Coroutine;
            public Action<object> OnCompleted;

            public CoroutineData(IEnumerator<object> coroutine)
            {
                Coroutine = coroutine;
            }
        }

        private static Dictionary<string, CoroutineData> _coroutines = new();

        // reset this dictionary on application close

        private bool _completed = false;
        private string _identifier;
        private CoroutineTask<T> _result;
        
        public override bool keepWaiting => !_completed;

        private static Action _onApplicationQuit;
        private void OnApplicationQuit()
        {
            _coroutines = new();
            _onApplicationQuit = null;
        }

        public Instruction(IEnumerator<object> coroutine, string identifier, out CoroutineTask<T> task)
        {
            Debug.Assert(coroutine != null);

            if (_onApplicationQuit == null)
            {
                _onApplicationQuit = OnApplicationQuit;
                CoroutineDispatcher.Instance.onApplicationQuit += _onApplicationQuit;
            }

            // register for the application play start

            task = new CoroutineTask<T>()
            {
                Status = StatusType.Running,
                Result = default
            };
            _result = task;

            _identifier = identifier;

            // if the coroutine already exists, register to listen to the on completed action
            // that gets called by the existing coroutine once it is done executing. 
            if (_identifier != null && _coroutines.TryGetValue(_identifier, out CoroutineData value))
            {
                _completed = false;
                value.OnCompleted += (obj) =>
                {
                    OnComplete(obj);
                };
                return;
            }

            CoroutineData data = new CoroutineData(coroutine);
            CoroutineDispatcher.Instance.StartCoroutine(Run(data));
            if (_identifier != null)
            {
                _coroutines.Add(_identifier, data);
            }
        }

        /// <summary>
        /// Wraps the given IEnumerator<T> coroutine inside the CoroutineData
        /// so that _loaded and the result can be set from the coroutine
        /// return value,
        ///
        /// this in its turn is then propagated to the Task, which
        /// allows the user to very simply get the return value of the
        /// coroutine. 
        /// </summary>
        private IEnumerator Run(CoroutineData data)
        {
            // iterate over the coroutine enumerator
            while (data.Coroutine.MoveNext())
            {
                // if the returned value is an exception, fail. 
                object current = data.Coroutine.Current;
                if (current is Exception)
                {
                    Complete(data);
                    yield break;
                }
                else
                {
                    _result.Result = (T)current;
                }
                yield return null;
            }
            Complete(data);
        }

        private void Complete(CoroutineData data)
        {
            object obj = data.Coroutine.Current;

            OnComplete(obj);

            data.OnCompleted?.Invoke(obj);

            if (_identifier != null)
            {
                _coroutines.Remove(_identifier);
            }
        }

        // execute fail or succeed command
        private void OnComplete(object obj)
        {
            Exception e = obj as Exception;
            if (obj == null || e != null)
            {
                // failed
                _result.Exception = e;
                _result.Result = default;
                _result.Status = StatusType.Failed;
                //Debug.Log(_result.Exception);

                //NotificationsController.Instance.OpenNotification(new Notification.Data()
                //{
                //    Title = nameof(_result.Exception),
                //    Description = _result.Exception.Message,
                //    IconColor = Color.red,
                //    Icon = Icons.Data.Warning,
                //    DisplayDurationInSeconds = 3
                //});
            }
            else
            {
                // succeed
                _result.Result = (T)obj;
                _result.Status = StatusType.Success;
            }
            _completed = true;
        }
    }
}

