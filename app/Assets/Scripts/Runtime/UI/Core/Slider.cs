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
using Cuboid.Utils;

namespace Cuboid.UI
{
    /// <summary>
    /// Slider
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class Slider : UIBehaviour,
        IPointerDownHandler,
        IPointerClickHandler,
        IPointerUpHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IScrollHandler,
        ICanSetBinding<float>
    {
        protected IBinding<float> _binding;

        private Action<float> _onValueChanged;

        public Action<float> OnSetValue;
        public Action<float> OnConfirmValue;

        [Header("Component References")]
        /// <summary>
        /// Image component of the handle the user presses. 
        /// </summary>
        [SerializeField] private Image _handleImage;
        [SerializeField] private Image _fillAreaImage;

        private RectTransform _rectTransform;
        private RectTransform _handleRectTransform;
        private RectTransform _fillAreaRectTransform;

        /// <summary>
        /// Such as in a scroll view, otherwise will use the exact position 
        /// </summary>
        public bool RelativeDrag = true;
        public bool SetValueOnClick = false; // used by color picker

        public float MinValue = 0f;

        public float MaxValue = 1f;

        public float ScrollDeltaInPixels = 2f;

        [SerializeField] private bool _displayFillArea = true;

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
                if (_fillAreaImage != null)
                {
                    _fillAreaImage.enabled = _displayFillArea && !_isEditingMultiple;
                }
                if (_handleImage != null)
                {
                    _handleImage.enabled = !_isEditingMultiple;
                }
            }
        }

        protected override void Start()
        {
            _onValueChanged = OnValueChanged;

            _rectTransform = GetComponent<RectTransform>();
            _handleRectTransform = _handleImage.GetComponent<RectTransform>();
            _fillAreaRectTransform = _fillAreaImage.GetComponent<RectTransform>();

            _fillAreaImage.enabled = _displayFillArea && !_isEditingMultiple;

            Register();

            UpdateHandleColor();
        }

        private void OnValueChanged(float value)
        {
            if (_fillAreaRectTransform == null || _handleRectTransform == null) { return; }

            // update the horizontal position of the slider

            float newHandlePosition = Utils.Math.Map(value, MinValue, MaxValue, 0, _maxHandlePosition);
            newHandlePosition = Mathf.Clamp(newHandlePosition, 0, _maxHandlePosition);
            _handleRectTransform.anchoredPosition = new Vector2(newHandlePosition, 0);

            // update the fill area
            if (_displayFillArea)
            {
                _fillAreaRectTransform.sizeDelta = new Vector2(newHandlePosition + _handleWidth / 2, 0);
            }
        }

        /// <summary>
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

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            Hovered = true;
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            Hovered = false;
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            Pressed = true;
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            Pressed = false;
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (!SetValueOnClick || RelativeDrag) { return; } // doesn't need to do anything, because it's relative anyway

            SetValueBasedOnPointerPosition(eventData);
            ConfirmValue();
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            // should enable the images otherwise will give a null reference error
            //_fillAreaImage.enabled = true;
            //_handleImage.enabled = true;

            // set the start value / offset
            _pointerStartLocalPosition = _handleRectTransform.anchoredPosition.x;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 startLocalPosition);
            _pointerStartLocalPosition = startLocalPosition.x;

            _handleStartPosition = _handleRectTransform.anchoredPosition.x;
        }

        private float _pointerStartLocalPosition;
        private float _handleStartPosition;

        private float _handleWidth => _handleRectTransform == null ? 0 : _handleRectTransform.sizeDelta.x; // check for null
        private float _sliderWidth => _rectTransform == null ? 0 : _rectTransform.rect.size.x; // check for null
        private float _maxHandlePosition => _sliderWidth - _handleWidth;

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            SetValueBasedOnPointerPosition(eventData);
        }

        private void SetValueBasedOnPointerPosition(PointerEventData eventData)
        {
            // update the horizontal position of the slider
            Vector2 localCursor;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, eventData.pressEventCamera, out localCursor))
                return;

            float position;
            if (RelativeDrag)
            {
                float pointerDelta = localCursor.x - _pointerStartLocalPosition;
                position = _handleStartPosition + pointerDelta;
            }
            else
            {
                position = localCursor.x - _handleWidth / 2 + _sliderWidth; // HACK: No idea why this is necessary, same as Slider2D
            }

            float newHandlePosition = Mathf.Clamp(position, 0, _maxHandlePosition);
            float newValue = Utils.Math.Map(newHandlePosition, 0, _maxHandlePosition, MinValue, MaxValue);
            if (_binding != null)
            {
                _binding.Value = newValue;
            }
            OnSetValue?.Invoke(newValue);
        }

        private void ConfirmValue()
        {
            OnConfirmValue?.Invoke(_binding.Value);
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            ConfirmValue();
        }

        void IScrollHandler.OnScroll(PointerEventData eventData)
        {
            // increase / decrease value on scroll
            //_binding.Data = _binding.Data +
            //float position = _handleRectTransform.anchoredPosition.x;
            //float deltaPosition = eventData.scrollDelta.x * ScrollDeltaInPixels;

            //float newValue = Utils.Math.Map(position + deltaPosition, 0, _maxHandlePosition, MinValue, MaxValue);
            //_binding.Value = newValue;
        }

        #region Action registration

        public void SetBinding(IBinding<float> binding)
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
