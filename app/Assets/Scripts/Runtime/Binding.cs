//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Cuboid.Models;
using UnityEngine;

namespace Cuboid
{
    public interface IBinding<T>
    {
        public T Value { get; set; }
        public Action<T> OnValueChanged { get; set; }

        public void Register(Action<T> action);
        public void Unregister(Action<T> action);

        public void ValueChanged();
    }

    public interface ICanSetBinding<T> where T : new()
    {
        public void SetBinding(IBinding<T> binding);
    }

    /// <summary>
    /// The StoredBinding class is an alternative to the Binding class that uses
    /// <see cref="UserData"/> to store data across sessions in a json file.
    ///
    /// T should be serializable using the Newtonsoft json library, otherwise
    /// it will probably throw errors. 
    /// 
    /// Similar to the Binding<T> class it allows for registering and unregistering
    /// to listen to changes in its data. 
    ///
    /// Because Application.persistentDataPath is not allowed to be called from a constructor,
    /// please initialize StoredBindings inside the Awake or Start methods. 
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class StoredBinding<T> : IBinding<T>
    {
        private string _key;

        private T _value;
        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                UserData.Set<T>(_key, _value);
                OnValueChanged?.Invoke(_value);
            }
        }

        public void Register(Action<T> action)
        {
            if (action != null)
            {
                OnValueChanged += action;
                action?.Invoke(Value); // always execute a first time
            }
        }

        public void Unregister(Action<T> action)
        {
            if (action != null)
            {
                OnValueChanged -= action;
            }
        }

        public StoredBinding(string key, T value = default(T))
        {
            _key = key;

            // try get the value from the user preferences, otherwise, use the supplied value in the constructor parameter value
            Value = UserData.TryGetValue<T>(_key, out T storedValue) ? storedValue : value;
        }

        [JsonIgnore]
        public Action<T> OnValueChanged { get; set; }

        public void ValueChanged()
        {
            OnValueChanged?.Invoke(Value);
        }
    }


    /// <summary>
    /// This is to make sure strings and MonoBehaviours can also be used with Binding
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class BindingWithoutNew<T> : IBinding<T>
    {
        private T _value;
        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }

        public void Register(Action<T> action)
        {
            if (action != null)
            {
                OnValueChanged += action;
                action?.Invoke(Value); // always execute a first time
            }
        }

        public void Unregister(Action<T> action)
        {
            if (action != null)
            {
                OnValueChanged -= action;
            }
        }

        public BindingWithoutNew()
        {
            Value = default(T);
        }

        public BindingWithoutNew(T value)
        {
            Value = value;
        }

        [JsonIgnore]
        public Action<T> OnValueChanged { get; set; }

        public void ValueChanged()
        {
            OnValueChanged?.Invoke(Value);
        }
    }

    [Serializable]
    public class Binding<T> : IBinding<T> where T : new()
    {
        private T _value;
        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }

        public void Register(Action<T> action)
        {
            if (action != null)
            {
                OnValueChanged += action;
                action?.Invoke(Value); // always execute a first time
            }
        }

        public void Unregister(Action<T> action)
        {
            if (action != null)
            {
                OnValueChanged -= action;
            }
        }

        public Binding()
        {
            Value = new T();
        }

        public Binding(T value)
        {
            Value = value;
        }

        [JsonIgnore]
        public Action<T> OnValueChanged { get; set; }

        public void ValueChanged()
        {
            OnValueChanged?.Invoke(Value);
        }
    }
}

