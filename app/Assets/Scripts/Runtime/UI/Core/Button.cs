//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace Cuboid.UI
{
    /// <summary>
    /// 
    /// </summary>
    public class Button : UIBehaviour,
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        /// <summary>
        /// 
        /// </summary>
        [Serializable]
        public class Data
        {
            /// <summary>
            /// 
            /// </summary>
            public string Text;

            /// <summary>
            /// 
            /// </summary>
            public Sprite Icon;

            /// <summary>
            /// 
            /// </summary>
            public Action OnPressed;

            public bool Disabled = false;

            /// <summary>
            /// 
            /// </summary>
            public ButtonColors.Variant Variant = ButtonColors.Variant.Plain;
        }

        [Header("Button Data")]
        [SerializeField]
        private Data _data;

        public Data ActiveData
        {
            get => _data;
            set
            {
                _data = value;
                if (_data == null) { return; }
                OnDataChanged(_data);
            }
        }

        [SerializeField] private UnityEvent _onPressedEvent;

        private bool _active = false;
        /// <summary>
        /// Active means whether this button is the currently active data,
        /// e.g. for menu panels: if Settings is selected, the corresponding button is set to
        /// "Active" and thus doesn't need to be interactable since it's already selected.
        /// </summary>
        public bool Active
        {
            get => _active;
            set
            {
                _active = value;
                UpdateButtonColors();
            }
        }

        private bool _disabled = false;
        public bool Disabled
        {
            get => _disabled;
            set
            {
                _disabled = value;
                UpdateButtonColors();
            }
        }

        private bool _hovered;
        public bool Hovered
        {
            get => _hovered;
            set
            {
                _hovered = value;
                UpdateButtonColors();
            }
        }

        private bool _pressed;
        public bool Pressed
        {
            get => _pressed;
            set
            {
                _pressed = value;
                UpdateButtonColors();
            }
        }

        [Header("Component References")]
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;

        [Header("Icon rect transform settings")]
        public float IconSize = 40f;
        [SerializeField] private float _iconPadding = 10f;

        private RectTransform _iconRectTransform;

        protected override void Start()
        {
            if (_text == null)
            {
                _text = GetComponentInChildren<TextMeshProUGUI>();
            }

            if (_background == null)
            {
                _background = GetComponent<Image>();
            }

            // Try to find the icon in the children
            // Can't use GetComponentInChildren because this includes the parent
            // as well
            if (_icon == null)
            {
                foreach (Transform child in transform)
                {
                    if (child.TryGetComponent<Image>(out var icon))
                    {
                        _icon = icon;
                        break;
                    }
                }
            }

            if (_icon != null)
            {
                // cache the rect transform
                _iconRectTransform = _icon.GetComponent<RectTransform>();
            }

            if (ActiveData != null)
            {
                OnDataChanged(ActiveData);
            }
        }

        protected virtual void OnPressed()
        {
            _onPressedEvent.Invoke();
            if (ActiveData != null)
            {
                ActiveData.OnPressed?.Invoke();
            }
        }

        public void DataChanged()
        {
            OnDataChanged(ActiveData);
        }

        private void OnDataChanged(Data buttonData)
        {
            bool hasText = false;
            // Set text
            if (_text != null)
            {
                hasText = buttonData.Text != null;
                if (hasText)
                {
                    _text.text = buttonData.Text;
                }
                _text.gameObject.SetActive(hasText);
            }

            if (_icon != null)
            {
                // Set icon
                bool hasIcon = buttonData.Icon != null;
                if (hasIcon)
                {
                    _icon.sprite = buttonData.Icon;
                }
                _icon.gameObject.SetActive(hasIcon);

                // set the rect transform to the center or to the side, depending on whether the button also has text.
                if (_iconRectTransform != null)
                {
                    if (hasText)
                    {
                        // Align icon left
                        _iconRectTransform.anchorMin = new Vector2(0f, 0f); // left aligned
                        _iconRectTransform.anchorMax = new Vector2(0f, 1f); // stretch vertically
                        _iconRectTransform.sizeDelta = new Vector2(IconSize, -_iconPadding * 2); // add vertical padding
                        _iconRectTransform.anchoredPosition = new Vector2(_iconPadding, 0f); // left padding
                        _iconRectTransform.pivot = new Vector2(0f, 0.5f);
                    }
                    else
                    {
                        // Center icon
                        _iconRectTransform.anchorMin = new Vector2(0.5f, 0.5f); // centered
                        _iconRectTransform.anchorMax = new Vector2(0.5f, 0.5f); // centered
                        _iconRectTransform.sizeDelta = Vector2.one * IconSize; // icon size
                        _iconRectTransform.anchoredPosition = Vector2.zero; // no offset from center
                        _iconRectTransform.pivot = new Vector2(0.5f, 0.5f);
                    }
                }
            }

            Disabled = buttonData.Disabled;
            
            UpdateButtonColors();
        }

        private void UpdateButtonColors()
        {
            ButtonColors.Variant variant = ActiveData == null ? ButtonColors.Variant.Plain : ActiveData.Variant;
            ColorsScriptableObject buttonColors = UIController.Instance.Colors.Get(variant);

            Color backgroundColor;
            Color foregroundColor;
            if (Disabled)
            {
                backgroundColor = buttonColors.Disabled;
                foregroundColor = buttonColors.DisabledText;
            }
            else
            {
                foregroundColor = Active ? buttonColors.ActiveText : buttonColors.Text;
                if (Pressed)
                {
                    backgroundColor = Active ? buttonColors.ActivePressed : buttonColors.Pressed;
                }
                else if (Hovered)
                {
                    backgroundColor = Active ? buttonColors.ActiveHover : buttonColors.Hover;
                }
                else
                {
                    backgroundColor = Active ? buttonColors.Active : buttonColors.Normal;
                }
            }

            if (_icon != null)
            {
                _icon.color = foregroundColor;
            }

            if (_text != null)
            {
                _text.color = foregroundColor;
            }

            if (_background != null)
            {
                _background.color = backgroundColor;
            }
        }

        #region Pointer Events

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            // set hover to true
            Hovered = true;
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (Active || Disabled) { return; }

            OnPressed();
            //Hovered = false;
            //Pressed = false;
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            // set hover to false
            Hovered = false;
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            // set pressed to true
            Pressed = true;
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            // set pressed to false
            Pressed = false;
        }

        #endregion
    }
}

