// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static UnityEngine.Rendering.DebugUI;

namespace Cuboid.UI
{
    /// <summary>
    /// A UI element with a toggle
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class Toggle : UIBehaviour,
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ICanSetBinding<bool>
    {
        protected IBinding<bool> _binding;

        private Action<bool> _onDataChanged;

        private bool _hovered = false;
        public bool Hovered
        {
            get => _hovered;
            set
            {
                _hovered = value;
                UpdateHandleColor();
            }
        }

        private bool _pressed = false;
        public bool Pressed
        {
            get => _pressed;
            set
            {
                _pressed = value;
                UpdateHandleColor();
            }
        }

        private bool _isEditingMultiple = false;
        public bool IsEditingMultiple
        {
            get => _isEditingMultiple;
            set
            {
                _isEditingMultiple = value;
                // set handle visibility
                if (_handleImage != null)
                {
                    _handleImage.enabled = !_isEditingMultiple;
                }
                UpdateBackgroundColor();
            }
        }

        public Action<bool> OnSetValue;

        [Header("Component References")]
        /// <summary>
        /// Image component of the handle the user presses. 
        /// </summary>
        [SerializeField] private Image _handleImage;

        [SerializeField] private Image _backgroundImage;

        private RectTransform _rectTransform;
        private RectTransform _handleRectTransform;

        [SerializeField] private float _padding = 5f;

        protected override void Start()
        {
            _onDataChanged = OnValueChanged;

            if (_backgroundImage == null)
            {
                _backgroundImage = GetComponent<Image>();
            }

            if (_handleImage == null)
            {
                foreach (Transform child in transform)
                {
                    if (child.TryGetComponent<Image>(out var handleImage))
                    {
                        _handleImage = handleImage;
                        break;
                    }
                }
            }

            _rectTransform = GetComponent<RectTransform>();
            _handleRectTransform = _handleImage.GetComponent<RectTransform>();

            Register();

            UpdateHandleColor();
        }

        private void UpdateBackgroundColor()
        {
            ColorsScriptableObject colors = UIController.Instance.Colors.Get(ButtonColors.Variant.Solid);
            if (_backgroundImage != null)
            {
                bool active = _binding != null && _binding.Value == true && !IsEditingMultiple;
                _backgroundImage.color = active ? colors.Normal : Color.white;
            }
        }

        private void OnValueChanged(bool value)
        {
            UpdateBackgroundColor();

            // Update the handle position (naive implementation)
            float width = _rectTransform.sizeDelta.x;
            float handleWidth = _handleRectTransform.sizeDelta.x;

            float maxPosition = width - handleWidth - _padding;
            _handleRectTransform.anchoredPosition = new Vector2(value ? maxPosition : _padding, 0f);
        }

        private void UpdateHandleColor()
        {
            ColorsScriptableObject colors = UIController.Instance.Colors.Get(ButtonColors.Variant.Plain);
            Color handleColor = colors.Normal;
            if (Pressed)
            {
                handleColor = colors.Pressed;
            }
            else if (Hovered)
            {
                handleColor = colors.Hover;
            }

            if (_handleImage != null)
            {
                _handleImage.color = handleColor;
            }
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (_binding != null)
            {
                _binding.Value = !_binding.Value; // toggle the boolean value of the binding
                OnSetValue?.Invoke(_binding.Value);
            }
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            Pressed = true;
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            Hovered = true;
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            Hovered = false;
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            Pressed = false;
        }

        #region Action registration

        public void SetBinding(IBinding<bool> binding)
        {
            Unregister();
            _binding = binding;
            Register();
        }

        protected void Register()
        {
            if (_binding != null)
            {
                _binding.Register(_onDataChanged);
            }
        }

        protected void Unregister()
        {
            if (_binding != null)
            {
                _binding.Unregister(_onDataChanged);
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
