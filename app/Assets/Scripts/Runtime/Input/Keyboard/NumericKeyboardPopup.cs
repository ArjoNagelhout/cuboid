//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cuboid.UI;
using TMPro;

namespace Cuboid.Input
{
    /// <summary>
    /// Custom numeric keyboard that can be used to input numeric values
    /// </summary>
    public class NumericKeyboardPopup : MonoBehaviour
    {
        [SerializeField] private Transform _buttonsTransform;

        [SerializeField] private float _padding;
        [SerializeField] private float _overrideFontSize = 30f;

        [SerializeField] private InputField _inputField;

        private string _value;

        private const int _columns = 4;
        private const int _rows = 5;

        private int _i;

        private Button InstantiateButton(Action onPressed, string text = null, Sprite icon = null, ButtonColors.Variant variant = ButtonColors.Variant.Plain)
        {
            Button button = UIController.Instance.InstantiateButton(_buttonsTransform, new Button.Data()
            {
                Text = text,
                Icon = icon,
                OnPressed = onPressed,
                Variant = variant
            });

            TextMeshProUGUI textMesh = button.GetComponentInChildren<TextMeshProUGUI>();
            if (textMesh != null)
            {
                textMesh.fontSize = _overrideFontSize;
            }

            // set recttransform values
            RectTransform rectTransform = button.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(_padding, _padding);
            rectTransform.sizeDelta = new Vector2(-_padding * 2, -_padding * 2);

            float width = 1f / _columns;
            float height = 1f / _rows;

            int x = _i % _columns;
            int y = (_rows - 1) - _i / _columns;
            
            float xMin = x * width;
            float xMax = (x + 1) * width;
            float yMin = y * height;
            float yMax = (y + 1) * height;

            rectTransform.anchorMin = new Vector2(xMin, yMin);
            rectTransform.anchorMax = new Vector2(xMax, yMax);

            _i++;

            return button;
        }

        private bool _canConfirmValue = false;

        private void Awake()
        {
            // Set actions
            _onSourceInputFieldConfirmValue = (value) =>
            {
                if (_sourceInputField != null)
                {
                    // _sourceInputField.text is to make sure you don't use the value that is passed by the keyboard popup
                    // but the one that could have been changed, e.g. by the ValueField that performs an Evaluate function
                    // on set value.
                    // This back-propagates those calculated changes to this popup again. 
                    SetValueWithoutNotify(_sourceInputField.text);
                }
            };

            OnSetValue += (value) =>
            {
                _canConfirmValue = true;

                if (_sourceInputField == null) { return; }

                _sourceInputField.text = value;
                _sourceInputField.OnSetValue?.Invoke(value);
            };

            OnConfirmValue += (value) =>
            {
                _canConfirmValue = false;

                if (_sourceInputField == null) { return; }

                _sourceInputField.text = value;
                _sourceInputField.OnConfirmValue?.Invoke(value);
            };
        }

        private void Start()
        {
            Instantiate();

            if (_inputField != null)
            {
                _inputField.Select();
                _inputField.onValueChanged.AddListener((value) =>
                {
                    _value = value;
                    OnSetValue?.Invoke(value);
                });
            }
        }

        /// <summary>
        /// Sets the value of the input field
        /// </summary>
        /// <param name="value"></param>
        public void SetValueWithoutNotify(string value)
        {
            _value = value;
            _inputField.SetTextWithoutNotify(value);
        }

        /// <summary>
        /// Listen to value changes of the input field
        /// </summary>
        public Action<string> OnSetValue;
        public Action<string> OnConfirmValue;

        private void Instantiate()
        {
            void Char(char character)
            {
                InstantiateButton(() => EnterCharacter(character), character.ToString(), null);
            }

            // top left to bottom right

            // row 1
            InstantiateButton(() => AC(), "AC", variant: ButtonColors.Variant.Soft);
            InstantiateButton(() => EnterCharacter('('), "(", variant: ButtonColors.Variant.Soft);
            InstantiateButton(() => EnterCharacter(')'), ")", variant: ButtonColors.Variant.Soft);
            InstantiateButton(() => PressBackspace(), icon: Icons.Data.Backspace, variant: ButtonColors.Variant.Soft);

            // row 2
            Char('7');
            Char('8');
            Char('9');
            InstantiateButton(() => EnterCharacter('/'), "/", variant: ButtonColors.Variant.Soft);

            // row 3
            Char('4');
            Char('5');
            Char('6');
            InstantiateButton(() => EnterCharacter('*'), "x", variant: ButtonColors.Variant.Soft);

            // row 4
            Char('1');
            Char('2');
            Char('3');
            InstantiateButton(() => EnterCharacter('-'), "-", variant: ButtonColors.Variant.Soft);

            // row 5
            Char('0');
            Char('.');
            InstantiateButton(() => Equals(), "=", variant: ButtonColors.Variant.Solid);
            InstantiateButton(() => EnterCharacter('+'), "+", variant: ButtonColors.Variant.Soft);
        }

        private void AC()
        {
            _inputField.text = ""; // does create a callback
        }

        private void Equals()
        {
            // means it needs to be evaluated.

            OnConfirmValue?.Invoke(_value);
        }

        private void PressBackspace()
        {
            VirtualKeyboard.Instance.PressBackspace();
        }

        private void EnterCharacter(char character)
        {
            VirtualKeyboard.Instance.EnterCharacter(character);
        }

        public void OK()
        {
            // apply the value
            OnConfirmValue?.Invoke(_value);
            PopupsController.Instance.ClosePopup(gameObject);
        }

        public void Cancel()
        {
            // apply the value as well, otherwise would have to revert
            OnConfirmValue?.Invoke(_value);
            PopupsController.Instance.ClosePopup(gameObject);
        }

        #region Action registration

        private Action<string> _onSourceInputFieldConfirmValue;

        private InputField _sourceInputField;

        public void SetSourceInputField(InputField inputField)
        {
            Unregister();
            _sourceInputField = inputField;
            Register();
        }

        public void Register()
        {
            if (_sourceInputField != null)
            {
                SetValueWithoutNotify(_sourceInputField.text);

                // make sure that when the value gets confirmed (e.g. the value is calculated etc.,
                // it sets the value to the calculated value)
                // this works, because the value is constantly calculated on the fly by the ValueField
                // and set to the source input field
                _sourceInputField.OnConfirmValue += _onSourceInputFieldConfirmValue;
            }
        }

        private void Unregister()
        {
            if (_sourceInputField != null)
            {
                _sourceInputField.OnConfirmValue -= _onSourceInputFieldConfirmValue;
            }

            if (_canConfirmValue)
            {
                OnConfirmValue?.Invoke(_value);
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

