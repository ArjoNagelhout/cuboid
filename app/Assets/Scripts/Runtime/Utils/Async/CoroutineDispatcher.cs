using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid
{
    public class CoroutineDispatcher : MonoBehaviour
    {
        private static CoroutineDispatcher _instance;
        public static CoroutineDispatcher Instance => _instance;

        public Action onApplicationQuit;

        private void Awake()
        {
            // Singleton implementation
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }
        }

        private void OnApplicationQuit()
        {
            onApplicationQuit?.Invoke();
        }
    }
}

