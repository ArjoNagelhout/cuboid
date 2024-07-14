// Copyright (c) 2023 Arjo Nagelhout

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XInput;
using UnityEngine.InputSystem.XR;
using UnityEngine.Serialization;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Cuboid.Utils;

namespace Cuboid.Input
{
    /// <summary>
    /// Adapted from ActionBasedController from the
    /// XR Interaction Toolkit. Removed unnecessary actions. 
    /// </summary>
    public class XRController : MonoBehaviour
    {
        private bool _applicationFocus = true;
        private Action<bool> _onApplicationFocusChanged;
        private App _app;

        private InputController _inputController;
        private Action<Handedness.Hand> _onDominantHandChanged;

        /// <summary>
        /// To disable and enable this ray interactor on application focus.
        /// And enable and disable on handedness change
        /// </summary>
        [SerializeField] private RayInteractor _rayInteractor;

        public Transform MenuTransform;

        private XRControllerState _controllerState;

        private InputController.ControllerInputData _inputData;

        public enum ControllerType
        {
            Default,
            MetaQuest2,
            MetaQuestPro
        }

        /// <summary>
        /// Controller prefabs that should be instantiated for each controller type. 
        /// </summary>
        [SerializeField] private PrefabDictionary<ControllerType> _controllerPrefabs;

        /// <summary>
        /// Allow for an already instantiated prefab,
        /// only if this is not set it will instantiate the new controller
        /// </summary>
        [SerializeField] private GameObject _instantiatedControllerPrefab;

        public BindingWithoutNew<XRControllerVisual> LoadedXRControllerVisual = new(null);

        /// <summary>
        /// In the AppRuntime scene the tracking is already being done by the OVR Rig.
        /// In that case, we don't have to perform tracking. 
        /// </summary>
        public bool TrackingEnabled = true;

        public Handedness.Hand Hand;

        /// <summary>
        /// Should be set via some API call to determine which VR system is connected. 
        /// </summary>
        private ControllerType _controllerType;

        /// <summary>
        /// Gets the controller type it should instantiate, for now only supports Meta Quest 2 and Meta Quest Pro
        /// </summary>
        /// <returns></returns>
        private ControllerType GetControllerType()
        {
            OVRPlugin.SystemHeadset headset = OVRPlugin.GetSystemHeadsetType();
            switch (headset)
            {
                case OVRPlugin.SystemHeadset.Oculus_Quest_2:
                    return ControllerType.MetaQuest2;
                case OVRPlugin.SystemHeadset.Meta_Quest_Pro:
                    return ControllerType.MetaQuestPro;
                default:
                    return ControllerType.Default;
            }
        }

        private void Awake()
        {
            _controllerState = new XRControllerState();

            _controllerType = GetControllerType();

            if (_instantiatedControllerPrefab == null)
            {
                // instantiate the controller based on the set controller type
                _controllerPrefabs.InstantiateAsync(_controllerType, transform, (result) =>
                {
                    if (result == null) { return; }

                    _instantiatedControllerPrefab = result;

                    if (_instantiatedControllerPrefab.TryGetComponent<XRControllerVisual>(out XRControllerVisual xrControllerVisual))
                    {
                        LoadedXRControllerVisual.Value = xrControllerVisual;
                    }

                    // do post processing / data binding on the instantiated controller prefab.
                    _instantiatedControllerPrefab.SetActive(_applicationFocus);
                });
            }
        }

        private void Start()
        {
            _inputController = InputController.Instance;
            _inputData = _inputController.ControllersInputData[Hand];

            _onDominantHandChanged = OnDominantHandChanged;

            _app = App.Instance;
            // Make sure the controller and ray interactor is hidden / shown when the application focus is lost or regained by the OVRManager,
            // as per VRC.Quest.Input.4 (https://developer.oculus.com/resources/vrc-quest-input-4/)
            _onApplicationFocusChanged = (focus) =>
            {
                if (_instantiatedControllerPrefab != null)
                {
                    _instantiatedControllerPrefab.SetActive(focus);
                }
                _rayInteractor.enabled = focus && (_inputController.Handedness.HandEnabled(Hand));
                _applicationFocus = focus;
            };

            Register();
        }

        private void OnDominantHandChanged(Handedness.Hand hand)
        {
            // warning: Hand is the associated hand, and should be used, hand is the currently active hand -.-
            _rayInteractor.enabled = _inputController.Handedness.HandEnabled(Hand); 
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        private void Update()
        {
            UpdateTracking();
        }

        /// <summary>
        /// This method is automatically called for "Just Before Render" input updates for VR devices.
        /// </summary>
        /// <seealso cref="Application.onBeforeRender"/>
        private void OnBeforeRender()
        {
            UpdateTracking();
        }

        private void UpdateTracking()
        {
            if (!TrackingEnabled || !_applicationFocus) { return; }

            UpdateTrackingInput(_controllerState);
            ApplyControllerState(_controllerState);
        }

        private void ApplyControllerState(XRControllerState controllerState)
        {
            if (controllerState == null) { return; }

            if ((controllerState.inputTrackingState & InputTrackingState.Position) != 0)
            {
                transform.localPosition = controllerState.position;
            }

            if ((controllerState.inputTrackingState & InputTrackingState.Rotation) != 0)
            {
                transform.localRotation = controllerState.rotation;
            }
        }

        private void UpdateTrackingInput(XRControllerState controllerState)
        {
            controllerState.inputTrackingState = _inputData.TrackingState;
            controllerState.position = _inputData.Position;
            controllerState.rotation = _inputData.Rotation;
        }

        #region Action registration

        private void Register()
        {
            if (_app != null)
            {
                _app.OnApplicationFocusChanged += _onApplicationFocusChanged;
            }

            if (_inputController != null)
            {
                _inputController.Handedness.DominantHand.Register(_onDominantHandChanged);
            }
        }

        private void Unregister()
        {
            if (_app != null)
            {
                _app.OnApplicationFocusChanged -= _onApplicationFocusChanged;
            }

            if (_inputController != null)
            {
                _inputController.Handedness.DominantHand.Unregister(_onDominantHandChanged);
            }
        }

        private void OnEnable()
        {
            Application.onBeforeRender += OnBeforeRender;
            Register();
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= OnBeforeRender;
            Unregister();
        }

        private void OnDestroy()
        {
            Unregister();
        }

        #endregion
    }
}
