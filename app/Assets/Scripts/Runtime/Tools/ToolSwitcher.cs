//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cuboid.Input;
using DG.Tweening;
using System.Linq;
using Cuboid.UI;
using Cuboid.Utils;

namespace Cuboid
{
    /// <summary>
    /// Allows the user to switch the currently active tool using the joystick on the non-dominant hand
    /// controller.
    ///
    /// It will be either a radial menu or a simple up / down / left / right implementation
    /// </summary>
    public class ToolSwitcher : MonoBehaviour
    {
        // listen to non-dominant hand joystick input
        private InputController _inputController;
        private Action<Vector2> _onNonDominantHandScroll2DChanged;
        private Action<Handedness.Hand> _onDominantHandChanged;

        private Transform _leftHandJoystickTransform;
        private Transform _rightHandJoystickTransform;

        private ToolController _toolController;
        private Action<ToolController.Tool> _onActiveToolChanged;

        [SerializeField] private float _distanceFromCenter;
        [SerializeField] private float _deadzone;
        [SerializeField] private bool _clockwise;
        [SerializeField] private float _targetScale;

        [SerializeField] private ToolController.Tool[] _tools;

        [SerializeField] private Transform _toolsTransform;

        [SerializeField] private GameObject _toolPrefab;
        private GameObject _instantiatedTools;

        private Action<XRControllerVisual> _onLeftXRControllerVisualLoaded;
        private Action<XRControllerVisual> _onRightXRControllerVisualLoaded;

        private void Awake()
        {
            
        }

        private void Start()
        {
            _inputController = InputController.Instance;
            _onLeftXRControllerVisualLoaded = (visual) => { OnXRControllerVisualLoaded(visual, Handedness.Hand.LeftHand); };
            _onRightXRControllerVisualLoaded = (visual) => { OnXRControllerVisualLoaded(visual, Handedness.Hand.RightHand); };

            _onNonDominantHandScroll2DChanged = OnNonDominantHandScroll2DChanged;
            _onDominantHandChanged = OnDominantHandChanged;

            _toolController = ToolController.Instance;
            _onActiveToolChanged = OnActiveToolChanged;

            InstantiateToolSwitcherOptions(_tools);

            OnIsShowingToolSwitcherChanged(IsShowingToolSwitcher);

            Register();
        }

        private void OnXRControllerVisualLoaded(XRControllerVisual visual, Handedness.Hand hand)
        {
            if (visual == null) { return; }
            if (hand == Handedness.Hand.LeftHand)
            {
                _leftHandJoystickTransform = visual.JoyStickTransform;
            }
            else if (hand == Handedness.Hand.RightHand)
            {
                _rightHandJoystickTransform = visual.JoyStickTransform;
            }
            Attach();
        }

        private bool _isShowingToolSwitcher = false;
        public bool IsShowingToolSwitcher
        {
            get => _isShowingToolSwitcher;
            set
            {
                if (_isShowingToolSwitcher == value) { return; } // don't do anything if the value is already set
                _isShowingToolSwitcher = value;

                OnIsShowingToolSwitcherChanged(_isShowingToolSwitcher);
            }
        }

        private void OnIsShowingToolSwitcherChanged(bool show)
        {
            // show or hide the tool switcher using animation
            _toolsTransform.DOKill();
            if (show)
            {
                
                _toolsTransform.DOScale(_targetScale, 0.4f).SetEase(Ease.OutBack, 1.2f);
            }
            else
            {
                _toolsTransform.DOScale(0, 0.3f).SetEase(Ease.OutQuart);
            }
        }

        private void OnNonDominantHandScroll2DChanged(Vector2 value)
        {
            IsShowingToolSwitcher = value.sqrMagnitude > _deadzone * _deadzone;

            if (IsShowingToolSwitcher)
            {
                // now determine which option is currently selected

                int count = _tools.Length;

                // get the current rotation
                float angle = Vector3.SignedAngle(Vector3.up, value, Vector3.back);

                // snap angle to increments of the amount
                angle = angle.Snap(360f, count);

                if (angle < 0) { angle += 360f; }

                int index = Mathf.RoundToInt((angle / 360f) * (float)count);

                ToolController.Tool target = (ToolController.Tool)index;
                if (_toolController.ActiveTool.Value != target)
                {
                    _toolController.ActiveTool.Value = target;
                }
            }
        }

        private void OnDominantHandChanged(Handedness.Hand hand)
        {
            // attach to the right controller,
            // should be attached at the joystick location. (compatible with different controller models)
            Attach(hand);
        }

        private void Attach() => Attach(_inputController.Handedness.DominantHand.Value);

        private void Attach(Handedness.Hand hand)
        {
            // attach to the non dominant hand
            Transform targetTransform = hand == Handedness.Hand.RightHand ? _leftHandJoystickTransform : _rightHandJoystickTransform;
            _toolsTransform.SetParent(targetTransform, false);
        }

        private void OnActiveToolChanged(ToolController.Tool tool)
        {

        }

        //private 

        /// <summary>
        /// instantiates all options and adds them to the _toolsTransform
        /// </summary>
        private GameObject[] InstantiateToolSwitcherOptions(ToolController.Tool[] tools)
        {
            // in how many pieces to "cut the pie"
            int count = tools.Length;

            GameObject[] instantiatedToolSwitcherUIElements = new GameObject[count];

            float anglePerOption = 360f / count;

            for (int i = 0; i < count; i++)
            {
                ToolController.Tool tool = tools[i];

                float angle = anglePerOption * i;

                GameObject instantiatedToolSwitcherUIElement = Instantiate(_toolPrefab, _toolsTransform, false);
                Transform toolTransform = instantiatedToolSwitcherUIElement.transform;

                // set the transform
                Quaternion rotation = Quaternion.AngleAxis((_clockwise ? 1 : -1) * angle, Vector3.back);
                Vector3 direction = Vector3.up * _distanceFromCenter;
                toolTransform.localPosition = rotation * direction;

                // set the data
                ToolSwitcherUIElement toolSwitcherUIElement = instantiatedToolSwitcherUIElement.GetComponent<ToolSwitcherUIElement>();
                toolSwitcherUIElement.Tool = tool;

                instantiatedToolSwitcherUIElements[i] = instantiatedToolSwitcherUIElement;
            }

            return instantiatedToolSwitcherUIElements;
        }

        #region Action registration

        private void Register()
        {
            if (_inputController != null)
            {
                _inputController.NonDominantHandScroll2D.Register(_onNonDominantHandScroll2DChanged);
                _inputController.Handedness.DominantHand.Register(_onDominantHandChanged);

                _inputController.LeftHandXRController.LoadedXRControllerVisual.Register(_onLeftXRControllerVisualLoaded);
                _inputController.RightHandXRController.LoadedXRControllerVisual.Register(_onRightXRControllerVisualLoaded);
            }

            if (_toolController != null)
            {
                _toolController.ActiveTool.Register(_onActiveToolChanged);
            }
        }

        private void Unregister()
        {
            if (_inputController != null)
            {
                _inputController.NonDominantHandScroll2D.Unregister(_onNonDominantHandScroll2DChanged);
                _inputController.Handedness.DominantHand.Unregister(_onDominantHandChanged);

                _inputController.LeftHandXRController.LoadedXRControllerVisual.Unregister(_onLeftXRControllerVisualLoaded);
                _inputController.RightHandXRController.LoadedXRControllerVisual.Unregister(_onRightXRControllerVisualLoaded);
            }

            if (_toolController != null)
            {
                _toolController.ActiveTool.Unregister(_onActiveToolChanged);
            }
        }

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void OnDestroy()
        {
            Unregister();
        }

        #endregion
    }
}
