//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine.UIElements;
using UnityEngine.Events;
using System.Data;

namespace Cuboid.UI
{
    /// <summary>
    /// A UI element that enables you to input values (float or int)
    /// when pressed, or drag horizontally to make it smaller or larger. 
    /// </summary>
    public class ValueField : UIBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        private const string k_EditingMultipleDifferentValuesString = "Multiple";

        private IBinding<float> _binding;

        private Action<float> _onValueChanged;

        [SerializeField] private Cuboid.UI.InputField _inputField;

        public Action<float> OnConfirmValue;
        public Action<float> OnSetValue;

        private bool _isEditingMultiple = false;
        public bool IsEditingMultiple
        {
            get => _isEditingMultiple;
            set
            {
                _isEditingMultiple = value;
                UpdateText();
            }
        }

        protected override void Start()
        {
            _onValueChanged = OnValueChanged;

            _inputField.OnSetValue += (value) =>
            {
                if (Evaluate(value, out float output))
                {
                    if (_binding != null)
                    {
                        _binding.Value = output;
                    }
                    OnSetValue?.Invoke(output);
                }
            };
            _inputField.OnConfirmValue += (value) =>
            {
                if (Evaluate(value, out float output))
                {
                    if (_binding != null)
                    {
                        _binding.Value = output;
                    }
                    OnConfirmValue?.Invoke(output);
                }
            };

            Register();
        }

        private void UpdateText()
        {
            _inputField.text = IsEditingMultiple ?
                k_EditingMultipleDifferentValuesString : _binding.Value.ToString("0.###");
        }

        private void OnValueChanged(float newValue)
        {
            UpdateText();
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            
        }
        
        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            
        }

        private bool Evaluate(string value, out float output)
        {
            output = 0f;
            try
            {
                double result = Convert.ToDouble(new DataTable().Compute(value, null));
                float newValue = (float)result;

                if (float.IsInfinity(newValue) || float.IsNaN(newValue))
                {
                    throw new InvalidExpressionException();
                }

                output = newValue;
                return true;
            }
            catch (Exception e)
            {
                Debug.Log(e);
                return false;
            }
        }

        #region Action registration

        public void SetBinding(IBinding<float> binding)
        {
            Unregister();
            _binding = binding;
            Register();
        }

        private void Register()
        {
            if (_binding != null)
            {
                _binding.Register(_onValueChanged);
            }
        }

        private void Unregister()
        {
            if (_binding != null)
            {
                _binding.Unregister(_onValueChanged);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Unregister();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Unregister();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Register();
        }

        #endregion
    }
}
