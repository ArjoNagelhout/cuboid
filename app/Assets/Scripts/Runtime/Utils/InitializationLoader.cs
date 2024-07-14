// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Cuboid
{
    /// <summary>
    /// The <see cref="InitializationLoader"/> is solely meant for app startup.
    /// It loads a list of scenes asynchronously and invokes <see cref="allScenesLoaded"/>
    /// on completion.
    ///
    /// Don't use this for any other purposes. 
    /// </summary>
    public sealed class InitializationLoader : MonoBehaviour
    {
        [SerializeField] private AssetReference[] _scenes;

        public Action allScenesLoaded;

        private void Start()
        {
            LoadScenes(_scenes, () =>
            {
                allScenesLoaded?.Invoke();
                OnAllScenesLoaded();
            });
        }

        private void OnAllScenesLoaded()
        {
            // Destroy the Initialization scene
            SceneManager.UnloadSceneAsync(0);
        }

        /// <summary>
        /// Load a list of scenes asynchronously and execute onScenesLoaded when all
        /// scenes are loaded
        /// </summary>
        /// <param name="scenes"></param>
        /// <param name="onScenesLoaded"></param>
        private static void LoadScenes(AssetReference[] scenes, Action onScenesLoaded)
        {
            int requiredAmountOfLoadedScenes = scenes.Length;
            int amountOfLoadedScenes = 0;
            foreach (AssetReference scene in scenes)
            {
#if UNITY_EDITOR
                // Test if the scene is already loaded
                string sceneName = scene.editorAsset.name;
                Scene potentiallyLoadedScene = SceneManager.GetSceneByName(sceneName);

                if (potentiallyLoadedScene.isLoaded)
                {
                    amountOfLoadedScenes++;
                    if (amountOfLoadedScenes >= requiredAmountOfLoadedScenes)
                    {
                        onScenesLoaded.Invoke();
                    }
                    continue;
                }
#endif
                LoadScene(scene, () =>
                {
                    amountOfLoadedScenes++;
                    if (amountOfLoadedScenes >= requiredAmountOfLoadedScenes)
                    {
                        onScenesLoaded.Invoke();
                    }
                });
            }
        }

        /// <summary>
        /// Load a given addressable scene asynchronously and execute onSceneLoaded
        /// when the scene is loaded
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="onSceneLoaded"></param>
        private static void LoadScene(AssetReference scene, Action onSceneLoaded)
        {
            AsyncOperationHandle<SceneInstance> loadSceneOperationHandle = scene.LoadSceneAsync(UnityEngine.SceneManagement.LoadSceneMode.Additive, true);
            loadSceneOperationHandle.Completed += (operationHandle) =>
            {
                onSceneLoaded.Invoke();
            };
        }
    }
}