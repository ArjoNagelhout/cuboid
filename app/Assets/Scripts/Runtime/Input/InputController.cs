// 
// InputController.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Cuboid.Input
{
    /// <summary>
    /// The InputController is responsible for getting all input from the
    /// XRControllerInput action maps and allows other classes to listen to
    /// changes in input.
    /// 
    /// This is done via code instead of individual action bindings because
    /// this gives errors when the bindings are lost / changed. So there's less
    /// room for someone to accidentally forget binding.
    /// 
    /// As a tradeoff, this approach is more verbose. 
    /// </summary>
    public class InputController : MonoBehaviour,
        XRControllerInput.ILeftHandActions,
        XRControllerInput.ILeftHandTrackingActions,
        XRControllerInput.ILeftHandVisualsActions,
        XRControllerInput.IRightHandActions,
        XRControllerInput.IRightHandTrackingActions,
        XRControllerInput.IRightHandVisualsActions
    {
        private static InputController _instance;
        public static InputController Instance { get { return _instance; } }

        private XRControllerInput _xrControllerInput;

        [NonSerialized] public Binding<bool> NonDominantHandPrimaryButton = new(false);
        [NonSerialized] public Binding<bool> NonDominantHandSecondaryButton = new(false);
        [NonSerialized] public Binding<Vector2> NonDominantHandScroll2D = new(Vector2.zero);

        [NonSerialized] public Handedness Handedness;

        public XRController LeftHandXRController;
        public XRController RightHandXRController;

        public XRController GetXRController(Handedness.Hand hand) => hand == Handedness.Hand.LeftHand ?
            LeftHandXRController : RightHandXRController;

        private class ButtonData
        {
            public Binding<bool> Binding;
            public int Count = 0;
        }

        public class ControllerInputData
        {
            public Binding<bool> PrimaryButton = new();
            public Binding<bool> GripButton = new();
            public Binding<Vector2> Scroll2D = new();
            public Action JoystickButtonPressed;

            // tracking
            public Vector3 Position = Vector3.zero;
            public Quaternion Rotation = Quaternion.identity;
            public InputTrackingState TrackingState = InputTrackingState.All;

            // visuals
            public Action<float> OnPrimaryButtonValueChanged;
            public Action<float> OnSecondaryButtonValueChanged;
            public Action<float> OnTriggerValueChanged;
            public Action<float> OnGripValueChanged;
        }

        public ControllerInputData LeftInput => ControllersInputData[Handedness.Hand.LeftHand];
        public ControllerInputData RightInput => ControllersInputData[Handedness.Hand.RightHand];
        public Dictionary<Handedness.Hand, ControllerInputData> ControllersInputData = new Dictionary<Handedness.Hand, ControllerInputData>()
        {
            {
                Handedness.Hand.LeftHand,
                new ControllerInputData()
            },
            {
                Handedness.Hand.RightHand,
                new ControllerInputData()
            }
        };

        private Dictionary<Binding<bool>, ButtonData> _buttonData = new Dictionary<Binding<bool>, ButtonData>();

        private void Awake()
        {
            // Singleton implementation
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }

            Handedness = new();
        }

        private void Start()
        {
            // need to be registered because there are multiple buttons that can activate / deactivate the
            // value and we only want to fire the events once. 
            RegisterButton(LeftInput.PrimaryButton);
            RegisterButton(RightInput.PrimaryButton);

            RegisterActions();
        }

        private void RegisterButton(Binding<bool> binding)
        {
            _buttonData.Add(binding, new ButtonData()
            {
                Binding = binding,
                Count = 0
            });
        }

        #region Left Hand actions

        void XRControllerInput.ILeftHandActions.OnPrimaryButton(InputAction.CallbackContext context)
        {
            if (!Handedness.IsLeftHandEnabled)
            {
                ProcessButtonAction(context, NonDominantHandPrimaryButton);
            }
            ProcessButtonAction(context, LeftInput.PrimaryButton);
        }

        void XRControllerInput.ILeftHandActions.OnSecondaryButton(InputAction.CallbackContext context)
        {
            if (!Handedness.IsLeftHandEnabled)
            {
                ProcessButtonAction(context, NonDominantHandSecondaryButton);
            }
            ProcessButtonAction(context, LeftInput.PrimaryButton);
        }

        void XRControllerInput.ILeftHandActions.OnGripButton(InputAction.CallbackContext context)
        {
            ProcessButtonAction(context, LeftInput.PrimaryButton);
        }

        void XRControllerInput.ILeftHandActions.OnTriggerButton(InputAction.CallbackContext context)
        {
            ProcessButtonAction(context, LeftInput.PrimaryButton);
        }

        void XRControllerInput.ILeftHandActions.OnScroll2D(InputAction.CallbackContext context)
        {
            if (!Handedness.IsLeftHandEnabled)
            {
                NonDominantHandScroll2D.Value = context.ReadValue<Vector2>();
            }

            LeftInput.Scroll2D.Value = context.ReadValue<Vector2>();
        }

        void XRControllerInput.ILeftHandActions.OnJoystickButtonPressed(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                LeftInput.JoystickButtonPressed?.Invoke();
            }
        }

        #endregion

        #region Right hand actions

        void XRControllerInput.IRightHandActions.OnPrimaryButton(InputAction.CallbackContext context)
        {
            if (!Handedness.IsRightHandEnabled)
            {
                ProcessButtonAction(context, NonDominantHandPrimaryButton);
            }
            ProcessButtonAction(context, RightInput.PrimaryButton);
        }

        void XRControllerInput.IRightHandActions.OnSecondaryButton(InputAction.CallbackContext context)
        {
            if (!Handedness.IsRightHandEnabled)
            {
                ProcessButtonAction(context, NonDominantHandSecondaryButton);
            }
            ProcessButtonAction(context, RightInput.PrimaryButton);
        }

        void XRControllerInput.IRightHandActions.OnGripButton(InputAction.CallbackContext context)
        {
            ProcessButtonAction(context, RightInput.PrimaryButton);
        }

        void XRControllerInput.IRightHandActions.OnTriggerButton(InputAction.CallbackContext context)
        {
            ProcessButtonAction(context, RightInput.PrimaryButton);
        }

        void XRControllerInput.IRightHandActions.OnScroll2D(InputAction.CallbackContext context)
        {
            if (!Handedness.IsRightHandEnabled)
            {
                NonDominantHandScroll2D.Value = context.ReadValue<Vector2>();
            }

            RightInput.Scroll2D.Value = context.ReadValue<Vector2>();
        }

        void XRControllerInput.IRightHandActions.OnJoystickButtonPressed(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                RightInput.JoystickButtonPressed?.Invoke();
            }
        }

        #endregion

        /// <summary>
        /// Calls the down or up actions based on whether the button was
        /// pressed or released
        /// </summary>
        private void ProcessButtonAction(InputAction.CallbackContext context, Binding<bool> binding)
        {
            if (_buttonData.TryGetValue(binding, out ButtonData buttonData))
            {
                ref int count = ref _buttonData[binding].Count;
                switch (context.phase)
                {
                    case InputActionPhase.Started:
                        if (count++ == 0)
                        {
                            // means that this is the first time it tried to press
                            binding.Value = true;
                        }
                        break;
                    case InputActionPhase.Canceled:
                        if (--count == 0)
                        {
                            // means that this is the last button that was released
                            binding.Value = false;
                        }
                        break;
                }
            }
            else
            {
                switch (context.phase)
                {
                    case InputActionPhase.Started:
                        binding.Value = true;
                        break;
                    case InputActionPhase.Canceled:
                        binding.Value = false;
                        break;
                }
            }
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
            if (_xrControllerInput == null)
            {
                _xrControllerInput = new XRControllerInput();
            }

            _xrControllerInput.LeftHand.SetCallbacks(this);
            _xrControllerInput.LeftHandTracking.SetCallbacks(this);
            _xrControllerInput.LeftHandVisuals.SetCallbacks(this);

            _xrControllerInput.RightHand.SetCallbacks(this);
            _xrControllerInput.RightHandTracking.SetCallbacks(this);
            _xrControllerInput.RightHandVisuals.SetCallbacks(this);

            // always call enable! otherwise it won't get any actions
            _xrControllerInput.LeftHand.Enable();
            _xrControllerInput.LeftHandTracking.Enable();
            _xrControllerInput.LeftHandVisuals.Enable();

            _xrControllerInput.RightHand.Enable();
            _xrControllerInput.RightHandTracking.Enable();
            _xrControllerInput.RightHandVisuals.Enable();
        }

        private void UnregisterActions()
        {
            if (_xrControllerInput != null)
            {
                _xrControllerInput.LeftHand.Disable();
                _xrControllerInput.LeftHandTracking.Disable();
                _xrControllerInput.LeftHandVisuals.Disable();

                _xrControllerInput.RightHand.Disable();
                _xrControllerInput.RightHandTracking.Disable();
                _xrControllerInput.RightHandVisuals.Disable();
            }
        }

        // left hand action binding
        // tracking
        void XRControllerInput.ILeftHandTrackingActions.OnPosition(InputAction.CallbackContext context) => LeftInput.Position = context.ReadValue<Vector3>();
        void XRControllerInput.ILeftHandTrackingActions.OnRotation(InputAction.CallbackContext context) => LeftInput.Rotation = context.ReadValue<Quaternion>();
        void XRControllerInput.ILeftHandTrackingActions.OnTrackingState(InputAction.CallbackContext context) => LeftInput.TrackingState = (InputTrackingState)context.ReadValue<int>();

        // visuals
        void XRControllerInput.ILeftHandVisualsActions.OnPrimaryButtonValue(InputAction.CallbackContext context) => LeftInput.OnPrimaryButtonValueChanged?.Invoke(context.ReadValue<float>());
        void XRControllerInput.ILeftHandVisualsActions.OnSecondaryButtonValue(InputAction.CallbackContext context) => LeftInput.OnSecondaryButtonValueChanged?.Invoke(context.ReadValue<float>());
        void XRControllerInput.ILeftHandVisualsActions.OnGripValue(InputAction.CallbackContext context) => LeftInput.OnGripValueChanged?.Invoke(context.ReadValue<float>());
        void XRControllerInput.ILeftHandVisualsActions.OnTriggerValue(InputAction.CallbackContext context) => LeftInput.OnTriggerValueChanged?.Invoke(context.ReadValue<float>());

        // right hand action binding
        // tracking
        void XRControllerInput.IRightHandTrackingActions.OnPosition(InputAction.CallbackContext context) => RightInput.Position = context.ReadValue<Vector3>();
        void XRControllerInput.IRightHandTrackingActions.OnRotation(InputAction.CallbackContext context) => RightInput.Rotation = context.ReadValue<Quaternion>();
        void XRControllerInput.IRightHandTrackingActions.OnTrackingState(InputAction.CallbackContext context) => RightInput.TrackingState = (InputTrackingState)context.ReadValue<int>();

        // visuals
        void XRControllerInput.IRightHandVisualsActions.OnPrimaryButtonValue(InputAction.CallbackContext context) => RightInput.OnPrimaryButtonValueChanged?.Invoke(context.ReadValue<float>());
        void XRControllerInput.IRightHandVisualsActions.OnSecondaryButtonValue(InputAction.CallbackContext context) => RightInput.OnSecondaryButtonValueChanged?.Invoke(context.ReadValue<float>());
        void XRControllerInput.IRightHandVisualsActions.OnGripValue(InputAction.CallbackContext context) => RightInput.OnGripValueChanged?.Invoke(context.ReadValue<float>());
        void XRControllerInput.IRightHandVisualsActions.OnTriggerValue(InputAction.CallbackContext context) => RightInput.OnTriggerValueChanged?.Invoke(context.ReadValue<float>());

        #endregion
    }
}
