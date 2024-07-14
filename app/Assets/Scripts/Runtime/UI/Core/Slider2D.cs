// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Cuboid.Utils;

namespace Cuboid.UI
{
    /// <summary>
    /// Slider with points at which it will snap. 
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class Slider2D : UIBehaviour,
        IPointerDownHandler,
        IPointerClickHandler,
        IPointerUpHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        ICanSetBinding<Vector2>
    {
        protected IBinding<Vector2> _binding;

        private Action<Vector2> _onValueChanged;

        [Header("Component References")]
        /// <summary>
        /// Image component of the handle the user presses. 
        /// </summary>
        public Image HandleImage;

        private RectTransform _rectTransform;
        private RectTransform _handleRectTransform;

        public Vector2 MinValue = Vector2.zero;

        public Vector2 MaxValue = Vector2.one;

        public float ScrollDeltaInPixels = 2f;

        /// <summary>
        /// Such as in a scroll view, otherwise will use the exact position 
        /// </summary>
        public bool RelativeDrag = false;
        public bool SetValueOnClick = false; // used by color picker
        public bool ShouldUpdateHandleColor = true;

        [System.NonSerialized] public Binding<bool> Hovered = new(false);
        [System.NonSerialized] public Binding<bool> Pressed = new(false);

        public Action<Vector2> OnSetValue;
        public Action<Vector2> OnConfirmValue;

        private bool _isEditingMultiple = false;
        public bool IsEditingMultiple
        {
            get => _isEditingMultiple;
            set
            {
                _isEditingMultiple = value;
                // set handle visibility
                if (HandleImage != null)
                {
                    HandleImage.enabled = !_isEditingMultiple;
                }
            }
        }

        protected override void Start()
        {
            _onValueChanged = OnValueChanged;

            _rectTransform = GetComponent<RectTransform>();
            _handleRectTransform = HandleImage.GetComponent<RectTransform>();

            Register();

            UpdateHandleColor();
        }

        private void OnValueChanged(Vector2 value)
        {
            // update the horizontal position of the slider

            Vector2 newHandlePosition = Utils.Math.Map(value, MinValue, MaxValue, Vector2.zero, _maxHandlePosition);
            _handleRectTransform.anchoredPosition = newHandlePosition;
        }

        /// <summary>
        /// Because the 
        /// </summary>
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (_binding != null)
            {
                OnValueChanged(_binding.Value);
            }
        }

        private void UpdateHandleColor()
        {
            if (!ShouldUpdateHandleColor) { return; }

            ColorsScriptableObject colors = UIController.Instance.Colors.Get(ButtonColors.Variant.Plain);
            Color handleColor = colors.Normal;
            if (Pressed.Value)
            {
                handleColor = colors.Pressed;
            }
            else if (Hovered.Value)
            {
                handleColor = colors.Hover;
            }

            if (HandleImage != null)
            {
                HandleImage.color = handleColor;
            }
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            Hovered.Value = true;
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            Hovered.Value = false;
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            Pressed.Value = true;
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            Pressed.Value = false;
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (!SetValueOnClick || RelativeDrag) { return; } // doesn't need to do anything, because it's relative anyway

            SetValueBasedOnPointerPosition(eventData);
            ConfirmValue();
        }

        private Vector2 _pointerStartLocalPosition;
        private Vector2 _handleStartPosition;
        private Vector2 _handleSize => _handleRectTransform == null ? Vector2.zero : _handleRectTransform.sizeDelta;
        private Vector2 _sliderSize => _rectTransform == null ? Vector2.zero : _rectTransform.rect.size;
        private Vector2 _maxHandlePosition => _sliderSize - _handleSize;

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            // set the start value / offset
            _pointerStartLocalPosition = _handleRectTransform.anchoredPosition;

            _pointerStartLocalPosition = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, eventData.pressEventCamera, out _pointerStartLocalPosition);
            _handleStartPosition = _handleRectTransform.anchoredPosition;
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            SetValueBasedOnPointerPosition(eventData);
        }

        private void SetValueBasedOnPointerPosition(PointerEventData eventData)
        {
            Vector2 localCursor;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, eventData.pressEventCamera, out localCursor))
                return;

            Vector2 position;
            if (RelativeDrag)
            {
                Vector2 pointerDelta = localCursor - _pointerStartLocalPosition;
                position = _handleStartPosition + pointerDelta;
            }
            else
            {
                position = localCursor - _handleSize / 2 + new Vector2(_sliderSize.x, 0); // HACK: I have no idea why this offset is required. 
            }

            Vector2 newHandlePosition = Utils.Math.Clamp(position, Vector2.zero, _maxHandlePosition);
            Vector2 newValue = Utils.Math.Map(newHandlePosition, Vector2.zero, _maxHandlePosition, MinValue, MaxValue);

            if (_binding != null)
            {
                _binding.Value = newValue;
                OnSetValue?.Invoke(newValue);
            }
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            ConfirmValue();
        }

        private void ConfirmValue()
        {
            if (_binding != null)
            {
                OnConfirmValue?.Invoke(_binding.Value);
            }
        }

        #region Action registration

        public void SetBinding(IBinding<Vector2> binding)
        {
            Unregister();
            _binding = binding;
            Register();
        }

        protected void Register()
        {
            if (_binding != null)
            {
                _binding.Register(_onValueChanged);
            }
        }

        protected void Unregister()
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
