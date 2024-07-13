// 
// PrefabDictionary.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;

namespace Cuboid.Utils
{
    /// <summary>
    /// The PrefabDictionary can be used to store
    /// prefabs associated with a certain value
    /// </summary>
    [Serializable]
    public class PrefabDictionary<T>
    {
        private IEnumerator _instantiateCoroutine = null;

        [Serializable]
        public struct KeyPrefab
        {
            public T Key;
            public AssetReference Prefab;
        }

        [SerializeField] private KeyPrefab[] _prefabsList = new KeyPrefab[] { };

        /// <summary>
        /// Dictionary of prefabs, can be accessed by a certain enum value
        /// </summary>
        private Dictionary<T, AssetReference> _prefabs;
        public Dictionary<T, AssetReference> Prefabs
        {
            get
            {
                if (_prefabs == null)
                {
                    _prefabs = PopulatePrefabs(_prefabsList);
                }
                return _prefabs;
            }
        }

        /// <summary>
        /// Instantiate a given key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="parent"></param>
        /// <param name="onInstantiate"></param>
        /// <returns></returns>
        public void InstantiateAsync(T key, Transform parent, Action<GameObject> onInstantiate = null)
        {
            // first stop the currently running coroutine
            if (_instantiateCoroutine != null)
            {
                PrefabDictionaryHelper.Instance.StopCoroutine(_instantiateCoroutine);
            }

            // then create a new
            _instantiateCoroutine = InstantiateCoroutine(key, parent, onInstantiate);
            PrefabDictionaryHelper.Instance.StartCoroutine(_instantiateCoroutine);
        }

        private IEnumerator InstantiateCoroutine(T key, Transform parent, Action<GameObject> onInstantiate)
        {
            AssetReference assetReference = Prefabs[key];
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(assetReference);

            while (!handle.IsDone) { yield return null; }

            GameObject instantiatedResult = null;

            if (handle.Result != null)
            {
                instantiatedResult = UnityEngine.Object.Instantiate(handle.Result, parent, false);
            }

            onInstantiate?.Invoke(instantiatedResult);
        }

        //private async Task<GameObject> InstantiateAsyncInternal(T key, Transform parent, CancellationToken cancellationToken)
        //{
        //    try
        //    {
        //        cancellationToken.ThrowIfCancellationRequested();

        //        GameObject instantiatedPrefab = null;
        //        AssetReference assetReference = Prefabs[key];
        //        AsyncOperationHandle<GameObject> loadToolOperationHandle = Addressables.LoadAssetAsync<GameObject>(assetReference);
                
        //        await loadToolOperationHandle.Task;
                
        //        //await Task.Delay(1000); // 1 second delay to test for cancellation

        //        cancellationToken.ThrowIfCancellationRequested();
        //        if (loadToolOperationHandle.Result != null)
        //        {
        //            instantiatedPrefab = UnityEngine.Object.Instantiate(loadToolOperationHandle.Result, parent, false);
        //            return instantiatedPrefab;
        //        }
        //        return null;
        //    }
        //    catch (OperationCanceledException)
        //    {
        //        return null;
        //    }
        //}

        public AssetReference this[T key]
        {
            get => Prefabs[key]; 
        }

        public bool TryGetValue(T key, out AssetReference prefab)
        {
            return Prefabs.TryGetValue(key, out prefab);
        }

        /// <summary>
        /// Creates a dictionary from the list of prefabs, so that they can be easily
        /// accessed through the dictionary instead of iterating through the list every
        /// time. 
        /// </summary>
        /// <param name="prefabsList"></param>
        /// <returns></returns>
        private static Dictionary<T, AssetReference> PopulatePrefabs(
            KeyPrefab[] prefabsList)
        {
            Dictionary<T, AssetReference> prefabs = new Dictionary<T, AssetReference>();
            foreach (KeyPrefab keyPrefab in prefabsList)
            {
                prefabs.TryAdd(keyPrefab.Key, keyPrefab.Prefab);
            }

            return prefabs;
        }
    }
}

