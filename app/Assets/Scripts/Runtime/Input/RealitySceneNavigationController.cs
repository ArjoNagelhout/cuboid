// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cuboid.Input;

namespace Cuboid
{
    /// <summary>
    /// Handles scene navigation either by teleportation, transforming
    /// the scene or continuous locomotion.
    /// 
    /// Exposing all these settings individually to the user would be confusing,
    /// so we need to combine them into distinct modes. These modes are also depending
    /// on whether the scene is set to 1:1 AR mode etc.
    /// 
    /// The following modes are available:
    /// - 1:1 AR / VR -> positions are exactly in the position they should be, no scene transformation, no teleportation etc.
    /// - World scale, (walk through scene via teleportation or continuous locomotion)
    /// - Miniature, (translate / transform scene with controllers)
    /// </summary>
    public class RealitySceneNavigationController : MonoBehaviour
    {
        

        /// <summary>
        /// Whether to enable continuous locomotion with joystick
        /// (forward, backwards, left, right)
        /// </summary>
        public bool ContinuousLocomotion;

        /// <summary>
        /// Whether to enable teleportation via joystick up
        /// </summary>
        public bool Teleportation;

        /// <summary>
        /// Whether to enable snap turns via joystick left / right
        /// </summary>
        public bool SnapTurns;

        /// <summary>
        /// Whether to enable translating the scene with one hand
        /// </summary>
        public bool TranslateSceneWithOneController;

        /// <summary>
        /// Whether to enable transforming (translating, rotating, scaling)
        /// the scene with two hands. 
        /// </summary>
        public bool TransformSceneWithTwoControllers;

        private InputController _inputController;

        private Action<bool> _onLeftTransformSceneButtonChanged;
        private Action<bool> _onRightTransformSceneButtonChanged;

        private int __count = 0;
        private int _count
        {
            get => __count;
            set
            {
                int previousValue = __count;
                __count = value;

                // clamp value to be more than 0
                if (__count < 0) { __count = 0; }

                if (__count == 1 && previousValue == 0)
                {
                    // start translating
                    // store the current position
                    // if it was already scaling / rotating, it should continue doing that 
                }

                // this means we should start to scale / rotate
                if (__count >= 2 && TransformSceneWithTwoControllers)
                {
                    // start scaling + rotating
                }
            }
        }

        private void Start()
        {
            _inputController = InputController.Instance;

            _onLeftTransformSceneButtonChanged = OnLeftTransformSceneButtonChanged;
            _onRightTransformSceneButtonChanged = OnRightTransformSceneButtonChanged;

            Register();
        }

        private void OnLeftTransformSceneButtonChanged(bool value)
        {
            _count += value ? 1 : -1;
            //Debug.Log($"on left {_count}");
        }

        private void OnRightTransformSceneButtonChanged(bool value)
        {
            _count += value ? 1 : -1;
            //Debug.Log($"on right {_count}");
        }

        #region Action registration

        private void Register()
        {
            if (_inputController != null)
            {
                _inputController.LeftInput.GripButton.Register(_onLeftTransformSceneButtonChanged);
                _inputController.RightInput.GripButton.Register(_onRightTransformSceneButtonChanged);
            }
        }

        private void Unregister()
        {
            if (_inputController != null)
            {
                _inputController.LeftInput.GripButton.Unregister(_onLeftTransformSceneButtonChanged);
                _inputController.RightInput.GripButton.Unregister(_onRightTransformSceneButtonChanged);
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
