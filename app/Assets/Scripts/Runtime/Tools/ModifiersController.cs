// 
// ModifiersController.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cuboid.Input;

namespace Cuboid
{
    /// <summary>
    /// The ModifiersController is responsible for 
    /// </summary>
    public sealed class ModifiersController : MonoBehaviour
    {
        private static ModifiersController _instance;
        public static ModifiersController Instance => _instance;

        private InputController _inputController;

        private Action<bool> _nonDominantHandPrimaryButtonChanged;
        private Action<bool> _nonDominantHandSecondaryButtonChanged;

        [NonSerialized] public Binding<bool> ShiftModifier = new(false);
        [NonSerialized] public Binding<bool> OptionModifier = new(false);

        private void Awake()
        {
            // Singleton implementation
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }
        }

        private void Start()
        {
            _inputController = InputController.Instance;

            _nonDominantHandPrimaryButtonChanged = OnNonDominantHandPrimaryButtonChanged;
            _nonDominantHandSecondaryButtonChanged = OnNonDominantHandSecondaryButtonChanged;

            Register();
        }

        private void OnNonDominantHandPrimaryButtonChanged(bool pressed)
        {
            ShiftModifier.Value = pressed;
        }

        private void OnNonDominantHandSecondaryButtonChanged(bool pressed)
        {
            OptionModifier.Value = pressed;
        }

        #region Action registration

        private void Register()
        {
            if (_inputController != null)
            {
                _inputController.NonDominantHandPrimaryButton.Register(_nonDominantHandPrimaryButtonChanged);
                _inputController.NonDominantHandSecondaryButton.Register(_nonDominantHandSecondaryButtonChanged);
            }
        }

        private void Unregister()
        {
            if (_inputController != null)
            {
                _inputController.NonDominantHandPrimaryButton.Unregister(_nonDominantHandPrimaryButtonChanged);
                _inputController.NonDominantHandSecondaryButton.Unregister(_nonDominantHandSecondaryButtonChanged);
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

