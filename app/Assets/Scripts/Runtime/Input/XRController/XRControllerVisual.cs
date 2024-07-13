using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.Input
{
    /// <summary>
    /// The ControllerVisual is meant to display to the user what the current
    /// state of the controller's input is. e.g. whether Button A is pressed,
    /// or the amount to which the Trigger is moved. 
    /// </summary>
    public class XRControllerVisual : MonoBehaviour
    {
        /// <summary>
        /// The hand this <see cref="XRControllerVisual"/> belongs to.
        /// This is to make sure the menu button and the home button are placed
        /// in the correct location. 
        /// </summary>
        public Handedness.Hand Hand;

        [SerializeField] private Animator _animator;

        // transforms for the elements so that any script can place UI or elements at the
        // button positions
        public Transform JoyStickTransform;

        private Action<float> _onPrimaryButtonValueChanged;
        private Action<float> _onSecondaryButtonValueChanged;
        private Action<float> _onGripValueChanged;
        private Action<float> _onTriggerValueChanged;
        private Action<Vector2> _onJoystickValueChanged;

        private InputController _inputController;

        private void Start()
        {
            _inputController = InputController.Instance;

            // Actions
            _onPrimaryButtonValueChanged = (value) => { _animator.SetFloat("Button 1", value); };
            _onSecondaryButtonValueChanged = (value) => { _animator.SetFloat("Button 2", value); };
            _onGripValueChanged = (value) => { _animator.SetFloat("Grip", value); };
            _onTriggerValueChanged = (value) => { _animator.SetFloat("Trigger", value); };
            _onJoystickValueChanged = (value) =>
            {
                _animator.SetFloat("Joy X", value.x);
                _animator.SetFloat("Joy Y", value.y);
            };

            RegisterActions();
        }

        #region Action registration

        private void RegisterActions()
        {
            if (_inputController != null)
            {
                InputController.ControllerInputData inputData = _inputController.ControllersInputData[Hand];

                inputData.OnPrimaryButtonValueChanged += _onPrimaryButtonValueChanged;
                inputData.OnSecondaryButtonValueChanged += _onSecondaryButtonValueChanged;
                inputData.OnGripValueChanged += _onGripValueChanged;
                inputData.OnTriggerValueChanged += _onTriggerValueChanged;
                inputData.Scroll2D.OnValueChanged += _onJoystickValueChanged;
            }
        }

        private void UnregisterActions()
        {
            if (_inputController != null)
            {
                InputController.ControllerInputData inputData = _inputController.ControllersInputData[Hand];

                inputData.OnPrimaryButtonValueChanged -= _onPrimaryButtonValueChanged;
                inputData.OnSecondaryButtonValueChanged -= _onSecondaryButtonValueChanged;
                inputData.OnGripValueChanged -= _onGripValueChanged;
                inputData.OnTriggerValueChanged -= _onTriggerValueChanged;
                inputData.Scroll2D.OnValueChanged -= _onJoystickValueChanged;
            }
        }

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

        #endregion
    }
}
