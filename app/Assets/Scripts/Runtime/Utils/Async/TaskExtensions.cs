//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Cuboid
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Extend Task. Returns a Task which completes once the given task is complete and the given
        /// continuation function is called from the main thread in Unity.
        /// </summary>
        /// <param name="task">The task to continue with.</param>
        /// <param name="continuation">The continuation function to be executed on the main thread
        /// once the given task completes.</param>
        /// <returns>A new Task that is complete after the continuation is executed on the main
        /// thread.</returns>
        public static Task ContinueWithOnMainThread(this Task task, Action<Task> continuation)
        {
            return task.ContinueWith(t =>
            {
                MainThreadDispatcher.Instance.EnqueueAsync(() =>
                {
                    continuation(t);
                });
            });
        }

        public static Task<T> ContinueWithOnMainThread<T>(this Task<T> task, Action<Task<T>> continuation)
        {
            return task.ContinueWith<T>((a) =>
            {
                MainThreadDispatcher.Instance.EnqueueAsync<T>((b) =>
                {
                    continuation(a);
                }, a.Result);
                return a.Result;
            });
        }
    }
}
