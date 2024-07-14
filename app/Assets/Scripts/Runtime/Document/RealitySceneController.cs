//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cuboid.Models;

namespace Cuboid
{
    /// <summary>
    /// Is responsible for loading and unloading a scene,
    /// because a file can contain multiple scenes, but not active at the same time
    ///
    /// Because RealityObjects can be instantiated asynchronously, it's important
    /// to keep track somehow of which objects are being instantiated, and canceling
    /// that operation when the add command gets undone.
    ///
    /// In SketchVR, all objects were also loaded / instantiated asynchronously, and
    /// there was no progress bar for each of these objects. Same goes for Tilt Brush.
    /// It simply pops them into existance.
    ///
    /// Only for dragging an object out of the asset library, it might be useful to have a preview.
    /// But for loading and unloading a scene not. 
    /// </summary>
    public sealed class RealitySceneController : MonoBehaviour
    {
        private static RealitySceneController _instance;
        public static RealitySceneController Instance => _instance;

        private RealityDocumentController _realityDocumentController;
        private Action<RealityDocument> _onRealityDocumentChanged;

        public int OpenedRealitySceneIndex;

        private Scene? _scene = null;

        // Scene appearance properties, refactor into RealityEnvironment. 
        [SerializeField] private Material _skyboxMaterial;
        [SerializeField] private float _ambientIntensity;
        [SerializeField] private GameObject _lightSourcePrefab;
        private GameObject _instantiatedLightSourcePrefab;

        [NonSerialized] public Binding<Dictionary<Guid, RealityObject>> InstantiatedRealityObjects = new();

        public Action<RealityObjectData> OnInstantiatedRealityObject;

        /// <summary>
        /// All coroutines that are currently instantiating an object, when undoing, it should check this
        /// </summary>
        internal Dictionary<Guid, IEnumerator> _instantiateCoroutines = new Dictionary<Guid, IEnumerator>();

        private void Awake()
        {
            // Singleton implementation
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }
        }

        private void Start()
        {
            _realityDocumentController = RealityDocumentController.Instance;
            _onRealityDocumentChanged = OnRealityDocumentChanged;

            RegisterActions();
        }

        public RealitySceneData GetOpenedRealityScene()
        {
            return _realityDocumentController.RealityDocument.Value.ScenesData[OpenedRealitySceneIndex];
        }

        private void OnRealityDocumentChanged(RealityDocument realityDocument)
        {
            OpenedRealitySceneIndex = 0;
            UnloadRealityScene(() =>
            {
                LoadRealityScene(realityDocument.ScenesData[OpenedRealitySceneIndex]);
            });
        }

        private void UnloadRealityScene(Action onSceneUnloaded)
        {
            // Stop all loading coroutines
            foreach (IEnumerator coroutine in _instantiateCoroutines.Values)
            {
                StopCoroutine(coroutine);
            }
            _instantiateCoroutines.Clear();

            if (_scene == null)
            {
                onSceneUnloaded.Invoke();
            }
            else
            {
                AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(_scene.Value);
                unloadOperation.completed += (AsyncOperation operation) =>
                {
                    onSceneUnloaded.Invoke();
                };
            }
        }

        private void LoadRealityScene(RealitySceneData data)
        {
            // Create the scene
            _scene = SceneManager.CreateScene(data.Name);

            SceneManager.SetActiveScene(_scene.Value);

            _instantiatedLightSourcePrefab = null;

            Debug.Log("Skybox matertial");
            Debug.Log(_skyboxMaterial.shader);

            RenderSettings.skybox = _skyboxMaterial;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = _ambientIntensity;

            // The environment cubemap gets scheduled to be updated using this function.
            // https://docs.unity3d.com/ScriptReference/DynamicGI.UpdateEnvironment.html
            DynamicGI.UpdateEnvironment();

            _instantiatedLightSourcePrefab = Instantiate(_lightSourcePrefab, null, false);

            InstantiatedRealityObjects.Value = new();

            foreach (RealityObjectData realityObjectData in data.RealityObjects.Values)
            {
                Guid guid = realityObjectData.Guid;
                Instantiate(realityObjectData);
            }

            //InstantiatedRealityObjects.ValueChanged();
        }

        /// <summary>
        /// Instantiates the given reality objects asynchronously, and if already instantiated, simply add them to the scene.
        ///
        /// Note: Will set the RealityObjectData on the instantiated RealityObject. 
        /// </summary>
        /// <param name="realityObjectData"></param>
        /// <param name="beforeDataBinding">The action that is executed *before* RealityObjectData will be set on the instantiated RealityObject</param>
        /// <param name="onInstantiate">The action that is executed *after* the RealityObjectData is set on the instantiated RealityObject</param>
        public void Instantiate(RealityObjectData realityObjectData,
            Action<RealityObject> onInstantiate = null,
            Action<RealityObject> beforeDataBinding = null)
        {
            Guid guid = realityObjectData.Guid;

            if (InstantiatedRealityObjects.Value.ContainsKey(guid))
            {
                // this means it has already been instantiated, so we don't need to do this again.
                return;
            }

            if (_instantiateCoroutines.ContainsKey(guid))
            {
                // this means it's currently being instantiated, so we can skip this object. 
                return;
            }

            IEnumerator instantiateCoroutine = realityObjectData.InstantiateAsync((result) =>
            {
                _instantiateCoroutines.Remove(guid);
                //InstantiatedRealityObjects.Value[guid] = result;
                InstantiatedRealityObjects.Value.TryAdd(guid, result);

                beforeDataBinding?.Invoke(result);
                result.RealityObjectData = realityObjectData; // (1)
                result.gameObject.name = realityObjectData.Name.Value;
                onInstantiate?.Invoke(result);

                InstantiatedRealityObjects.ValueChanged();
                OnInstantiatedRealityObject?.Invoke(realityObjectData);
            });
            _instantiateCoroutines.Add(guid, instantiateCoroutine);
            //InstantiatedRealityObjects.Value.Add(guid, null);

            // start the instantiation
            StartCoroutine(instantiateCoroutine);
        }

        /// <summary>
        /// Destroys the given reality objects and adds them to the scene
        /// </summary>
        /// <param name="realityObjectData"></param>
        public void Destroy(RealityObjectData realityObjectData)
        {
            Guid guid = realityObjectData.Guid;

            // check both if it already exists as a coroutine and as a realityobject in the array.

            // 1. Stop instantiating coroutine
            if (_instantiateCoroutines.ContainsKey(guid))
            {
                // this means that a coroutine is currently instantiating the object
                // so it should stop instantiation.

                StopCoroutine(_instantiateCoroutines[guid]);
                _instantiateCoroutines.Remove(guid);
            }

            // 2. Destroy instantiated GameObject
            if (InstantiatedRealityObjects.Value.ContainsKey(guid))
            {
                // check if the game object is already instantiated
                RealityObject realityObject = InstantiatedRealityObjects.Value[guid];

                if (realityObject != null)
                {
                    // this means it needs to be destroyed
                    GameObject.Destroy(realityObject.gameObject);
                }
                InstantiatedRealityObjects.Value.Remove(guid);
            }

            InstantiatedRealityObjects.ValueChanged();
        }

        #region Action registration

        private void OnEnable()
        {
            RegisterActions();
        }

        private void OnDisable()
        {
            UnregisterActions();
        }

        private void OnDestroy()
        {
            UnregisterActions();
        }

        private void RegisterActions()
        {
            if (_realityDocumentController != null)
            {
                _realityDocumentController.RealityDocument.Register(_onRealityDocumentChanged);
            }
        }

        private void UnregisterActions()
        {
            if (_realityDocumentController != null)
            {
                _realityDocumentController.RealityDocument.Unregister(_onRealityDocumentChanged);
            }
        }

        #endregion
    }
}

