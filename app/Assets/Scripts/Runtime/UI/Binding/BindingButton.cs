//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Cuboid.TransformCommand;

namespace Cuboid.UI
{
    [RequireComponent(typeof(Button))]
    public abstract class BindingButton<T> : MonoBehaviour
    {
        protected IBinding<T> _binding;

        private Action<T> _onValueChanged;

        /// <summary>
        /// The associated data with this button, so that the data of the
        /// controller can be set to this associated data. e.g.
        ///
        /// MenuController, associated data of this button = Settings
        /// Sets the data of the MenuController to Settings.
        /// </summary>
        [Header("Data Binding")]
        [SerializeField] protected T _associatedData;

        protected abstract IBinding<T> GetBinding();

        private void Awake()
        {
            _onValueChanged = OnValueChanged;
            _button = GetComponent<Button>();
        }

        protected Button _button;

        protected void Start()
        {
            _binding = GetBinding();
            _onValueChanged = OnValueChanged;

            _button.ActiveData.OnPressed += OnPressed;

            OnValueChanged(_binding.Value); // To make sure it fires the first time

            Register();
        }

        protected abstract void OnValueChanged(T value);

        protected virtual void OnPressed()
        {
            _binding.Value = _associatedData;
        }

        protected void Register()
        {
            if (_binding != null)
            {
                _binding.OnValueChanged += _onValueChanged;
            }
        }

        protected void Unregister()
        {
            if (_binding != null)
            {
                _binding.OnValueChanged -= _onValueChanged;
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
    }
}
