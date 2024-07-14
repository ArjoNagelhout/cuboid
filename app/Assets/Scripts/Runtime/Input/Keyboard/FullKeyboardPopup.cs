// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cuboid.UI;
using UnityEngine.TextCore.Text;
using Unity.Burst.CompilerServices;

namespace Cuboid.Input
{
    public enum KeyboardLayout
    {
        Qwerty = 0,
        //Azerty,
        //Dvorak
    }

    /// <summary>
    /// A full keyboard that allows one to type on a qwerty keyboard
    ///
    /// This approach has been taken because it makes the core interaction system more robust
    /// (allowing two ray interactors to be enabled at the same time, and
    /// the default system keyboard doesn't have any logic for displaying, takes a long time to load
    ///
    /// and triggers an on ApplicationFocusLost event... Why Meta?? Can't even do this right...
    ///
    /// The popup is intended to be floating in front of the user, which means the normal
    /// UI will be temporarily invisible.
    ///
    /// This can also be used by other popups if needed (e.g. Login dialog etc.)
    ///
    /// No support for locale yet. 
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class FullKeyboardPopup : MonoBehaviour
    {
        [Header("Values")]
        [SerializeField] private float _horizontalPadding = 5f; // padding around all keys
        [SerializeField] private float _verticalPadding = 5f; // padding around all keys
        [SerializeField] private float _keyWidth = 80f;
        [SerializeField] private float _keyHeight = 70f;
        [SerializeField] private float _horizontalSpacing = 10f; // space between keys
        [SerializeField] private float _verticalSpacing = 6f; // space between keys
        [SerializeField] private float _row2Offset = 32f;
        [SerializeField] private float _enterWidth = 160f;

        [Header("Components")]
        [SerializeField] private InputField _inputField;

        /// <summary>
        /// Keyboard width computed property as determined by the
        /// sum of all keys in the second row.
        ///
        /// This could be made more generic by calculating which row contains the most keys and
        /// using that as the basis. 
        /// </summary>
        private float _keyboardWidth
        {
            get
            {
                KeyboardLayoutData data = _data;
                int row2KeysCount = data.Letters[1].Length;

                return _row2Offset
                    + _horizontalPadding * 2 // padding
                    + row2KeysCount * (_keyWidth + _horizontalSpacing) // keys
                    + _enterWidth; // enter
            }
        }
        private float _keyboardHeight
        {
            get
            {
                return _verticalPadding * 2 // padding
                    + 4 * (_keyHeight + _verticalSpacing); // key rows
            }
        }

        /// <summary>
        /// Whether to show letters or numbers & punctuation via the .?123 / ABC key
        /// </summary>
        public enum KeyboardMode
        {
            Letters,
            NumbersPunctuation
        }

        private KeyboardMode _keyboardMode = KeyboardMode.Letters;
        public KeyboardMode ActiveKeyboardMode
        {
            get => _keyboardMode;
            set
            {
                _keyboardMode = value;
                OnKeyboardModeChanged(_keyboardMode);
            }
        }

        private bool _shift = false;
        public bool Shift
        {
            get => _shift;
            set
            {
                _shift = value;
                OnShiftChanged(_shift);
            }
        }

        /// <summary>
        /// Each entry has an array of strings, these contain the rows
        /// (the keyboard has three rows)
        /// </summary>
        public struct KeyboardLayoutData
        {
            public string[] Letters;
            public string[] LettersShift;
            public string[] NumbersPunctuation;
            public string[] NumbersPunctuationShift;
        }
        //                                                                      10, 9, 9
        private static string[] _defaultNumbersPunctuation = new string[3] { "1234567890", "-/:;()$&@", ".,?!'\"" };
        private static string[] _defaultNumbersPunctuationShift = new string[3] { "[]{}#%^*+=", "_\\|~<>€", ".,?!'\"" };

        private KeyboardLayoutData _data => GetKeyboardLayoutData(_app.KeyboardLayout.Value);
        private KeyboardLayoutData GetKeyboardLayoutData(KeyboardLayout keyboardLayout) => keyboardLayout switch
        {
            KeyboardLayout.Qwerty => _qwertyLayout,
            //KeyboardLayout.Dvorak => _dvorakLayout,
            //KeyboardLayout.Azerty => _azertyLayout,
            _ => _qwertyLayout
        };

        private static KeyboardLayoutData _qwertyLayout = new KeyboardLayoutData()
        {
            Letters = new string[3] { "qwertyuiop", "asdfghjkl", "zxcvbnm,." },
            LettersShift = new string[3] { "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM!?" },
            NumbersPunctuation = _defaultNumbersPunctuation,
            NumbersPunctuationShift = _defaultNumbersPunctuationShift
        };

        // dvorak layout has one more column
        private static KeyboardLayoutData _dvorakLayout = new KeyboardLayoutData()
        {
            Letters = new string[3] { "',.pyfgcrl/", "aoeuidhtns", ";qjkxbmwvz" },
            LettersShift = new string[3] { "\"<>PYFGCRL?", "AOEUIDHTNS", ":QJKXBMWVZ" },
            NumbersPunctuation = _defaultNumbersPunctuation,
            NumbersPunctuationShift = _defaultNumbersPunctuationShift
        };

        private static KeyboardLayoutData _azertyLayout = new KeyboardLayoutData()
        {
            Letters = new string[3] { "azertyuiop", "qsdfghjklm", "wxcvbn,;:" },
            LettersShift = new string[3] { "AZERTYUIOP", "QSDFGHJKLM", "WXCVBN?./" },
            NumbersPunctuation = _defaultNumbersPunctuation,
            NumbersPunctuationShift = _defaultNumbersPunctuationShift
        };

        private Action<KeyboardLayout> _onKeyboardLayoutChanged;

        private List<Button>[] _keys = new List<Button>[3];

        private void OnKeyboardLayoutChanged(KeyboardLayout keyboardLayout)
        {
            // get data
            KeyboardLayoutData data = _data;

            // instantiate all keys
            for (int i = 0; i < 3; i++) // (always three rows)
            {
                // instantiate row
                // use the letters string length to determine keyboard columns

                string keysString = data.Letters[i];
                int newCount = keysString.Length;

                if (_keys[i] == null)
                {
                    _keys[i] = new List<Button>();
                }

                List<Button> keys = _keys[i];

                int currentCount = keys.Count;
                if (currentCount == newCount)
                {
                    // if the same amount of keys is required, don't do anything
                    continue;
                }
                else if (currentCount > newCount)
                {
                    // destroy the extra keys
                    for (int j = newCount; j < currentCount; j++)
                    {
                        Destroy(keys[j].gameObject);
                    }
                    _keys[i].RemoveRange(newCount, currentCount - newCount);
                }
                else
                {
                    // instantiate the extra keys
                    for (int j = currentCount; j < newCount; j++)
                    {
                        float offset = i switch
                        {
                            1 => _row2Offset,
                            2 => _keyWidth + _horizontalSpacing, // to make room for the shift key
                            _ => 0f
                        };

                        float x = offset + GetX(j);

                        // instantiate key
                        Button key = InstantiateKey(x, _keyWidth, i);
                        _keys[i].Add(key);
                    }
                }
            }

            UpdateKeyCharacters();
            UpdateSpecialKeysTransform();
            UpdateRectTransform();
        }

        private void UpdateRectTransform()
        {
            _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _rectTransform.sizeDelta = new Vector2(_keyboardWidth, _keyboardHeight);
        }

        private Button InstantiateKey(
            float x, float width, int row, // height is always the same so we can leave that out
            string text = null, Sprite icon = null,
            ButtonColors.Variant variant = ButtonColors.Variant.Plain)
        {
            Button key = UIController.Instance.InstantiateButton(transform, new Button.Data()
            {
                Text = text,
                Icon = icon,
                Variant = variant
            });
            key.gameObject.name = "Key";

            RectTransform rectTransform = key.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1); // align with top left, no stretch
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);

            rectTransform.sizeDelta = new Vector2(width, _keyHeight); // key size

            float y = GetY(row);
            rectTransform.anchoredPosition = new Vector2(x, y);

            return key;
        }

        // row 1
        private RectTransform _keyBackspaceRectTransform;

        // row 2
        private RectTransform _keyEnterRectTransform;

        // row 3
        private Button _keyLeftShift;
        private Button _keyRightShift;

        private RectTransform _keyLeftShiftRectTransform;
        private RectTransform _keyRightShiftRectTransform;

        // row 4
        private Button _keyLeftNumbersPunctuation;
        private Button _keyKeyboardLayout;
        private Button _keyRightNumbersPunctuation;

        private RectTransform _keyLeftNumbersPunctuationRectTransform;
        private RectTransform _keyKeyboardLayoutRectTransform;
        private RectTransform _keySpaceRectTransform;
        private RectTransform _keyRightNumbersPunctuationRectTransform;
        private RectTransform _keyCloseKeyboardRectTransform;

        /// <summary>
        /// Method for instantiation of Shift keys, backspace, space, .?123 keys etc.
        /// </summary>
        private void InstantiateSpecialKeys()
        {
            // row 1
            InstantiateSpecialKey(ref _keyBackspaceRectTransform, () => PressBackspace(),
                "Key_Backspace", icon: Icons.Data.Backspace);
            
            // row 2
            InstantiateSpecialKey(ref _keyEnterRectTransform , () => PressEnter(),
                "Key_Enter", "Enter", variant: ButtonColors.Variant.Solid);

            // row 3 
            _keyLeftShift = InstantiateSpecialKey(ref _keyLeftShiftRectTransform, () => PressShift(),
                "Key_LeftShift", icon: Icons.Data.KeyboardShiftKey);

            _keyRightShift = InstantiateSpecialKey(ref _keyRightShiftRectTransform, () => PressShift(),
                "Key_RightShift", icon: Icons.Data.KeyboardShiftKey);

            // row 4
            _keyLeftNumbersPunctuation = InstantiateSpecialKey(ref _keyLeftNumbersPunctuationRectTransform, () => PressNumbersPunctuation(),
                "Key_LeftNumbersPunctuation", k_NumbersPunctuationKeyString);

            _keyKeyboardLayout = InstantiateSpecialKey(ref _keyKeyboardLayoutRectTransform, () => PressKeyboardLayout(),
                "Key_KeyboardLayout", icon: Icons.Data.Language);

            InstantiateSpecialKey(ref _keySpaceRectTransform, () => PressSpace(),
                "Key_Space", variant: ButtonColors.Variant.Plain);

            _keyRightNumbersPunctuation = InstantiateSpecialKey(ref _keyRightNumbersPunctuationRectTransform, () => PressNumbersPunctuation(),
                "Key_RightNumbersPunctuation", k_NumbersPunctuationKeyString);

            InstantiateSpecialKey(ref _keyCloseKeyboardRectTransform, () => CloseKeyboard(),
                "Key_CloseKeyboard", icon: Icons.Data.KeyboardHide);
        }

        private Button InstantiateSpecialKey(ref RectTransform rectTransform, Action onPressed, string name, string text = null, Sprite icon = null, ButtonColors.Variant variant = ButtonColors.Variant.Soft)
        {
            Button key = InstantiateKey(0, _keyWidth, 0, text, icon, variant);
            key.gameObject.name = name;
            key.ActiveData.OnPressed = onPressed;
            rectTransform = key.GetComponent<RectTransform>();
            return key;
        }

        private void PressNumbersPunctuation()
        {
            // switch keyboard mode
            ActiveKeyboardMode = ActiveKeyboardMode == KeyboardMode.Letters ? KeyboardMode.NumbersPunctuation : KeyboardMode.Letters;
        }

        private void PressEnter()
        {
            VirtualKeyboard.Instance.EnterCharacter('\n');
        }

        private void PressShift()
        {
            Shift = !Shift;
        }

        private void PressBackspace()
        {
            VirtualKeyboard.Instance.PressBackspace();
        }

        private void PressKeyboardLayout()
        {
            // Open switch keyboard layout popup
            List<Button.Data> buttons = new List<Button.Data>();

            Array values = Enum.GetValues(typeof(KeyboardLayout));
            foreach (int value in values)
            {
                buttons.Add(new Button.Data()
                {
                    Text = Enum.GetName(typeof(KeyboardLayout), value),
                    OnPressed = () => {
                        _app.KeyboardLayout.Value = (KeyboardLayout)value;
                        PopupsController.Instance.CloseLastPopup();
                    },
                    Variant = _app.KeyboardLayout.Value == (KeyboardLayout)value ? ButtonColors.Variant.Soft : ButtonColors.Variant.Plain
                });
            }

            PopupsController.Instance.OpenContextMenu(new UI.ContextMenu.ContextMenuData()
            {
                Title = "Keyboard layout",
                Buttons = buttons
            });

            // todo: potentially make it so that you can quickly switch when pressing,
            // and allow exact selection using holding and dragging. 
            /*
            int count = Enum.GetValues(typeof(KeyboardLayout)).Length;
            int current = (int)_app.KeyboardLayout.Value;
            if (current < count-1)
            {
                current++;
            }
            else
            {
                current = 0;
            }
            _app.KeyboardLayout.Value = (KeyboardLayout)current;*/
        }

        public void CloseKeyboard()
        {
            PopupsController.Instance.ClosePopup(gameObject);
            OnConfirmValue?.Invoke(_value);
        }

        /// <summary>
        /// Called when the keyboard layout has changed
        /// (recalculates the width and horizontal position of the keys)
        /// </summary>
        private void UpdateSpecialKeysTransform()
        {
            KeyboardLayoutData data = _data;
            int row1KeysCount = data.Letters[0].Length;
            float backspaceX = GetX(row1KeysCount);
            float keyboardWidth = _keyboardWidth;
            float keyboardWidthCorrected = keyboardWidth - _horizontalPadding;

            _keyBackspaceRectTransform.Set(x: backspaceX, width: keyboardWidthCorrected - backspaceX);
            _keyEnterRectTransform.Set(x: keyboardWidthCorrected - _enterWidth, y: GetY(1), width: _enterWidth);
            _keyLeftShiftRectTransform.Set(x: _horizontalPadding, y: GetY(2), width: _keyWidth);

            int row3KeysCount = data.Letters[2].Length;
            float rightShiftX = GetX(row3KeysCount+1); // +1 because left shift takes one as well

            _keyRightShiftRectTransform.Set(x: rightShiftX, y: GetY(2), width: keyboardWidthCorrected - rightShiftX);

            // width of two keys
            float doubleKeyWidth = 2 * _keyWidth + _horizontalSpacing;
            _keyLeftNumbersPunctuationRectTransform.Set(x: GetX(0), y: GetY(3), width: doubleKeyWidth);

            _keyKeyboardLayoutRectTransform.Set(x: GetX(2), y: GetY(3), width: _keyWidth);
            
            _keyCloseKeyboardRectTransform.Set(x: keyboardWidth - GetX(0) - _keyWidth, y: GetY(3), width: _keyWidth);
            float keyRightNumbersPunctuationX = keyboardWidth - GetX(2) - _keyWidth;
            _keyRightNumbersPunctuationRectTransform.Set(x: keyRightNumbersPunctuationX, y: GetY(3), width: doubleKeyWidth);

            float keySpaceX = GetX(3); // hehe
            _keySpaceRectTransform.Set(x: keySpaceX, y: GetY(3), width: keyRightNumbersPunctuationX - keySpaceX - _horizontalSpacing);
        }

        private float GetX(int column) => _horizontalPadding + column * (_keyWidth + _horizontalSpacing);
        private float GetY(int row) => -_verticalPadding - row * (_keyHeight + _verticalSpacing);

        private void PressSpace()
        {
            if (ActiveKeyboardMode == KeyboardMode.NumbersPunctuation && _pressedKeyInCurrentKeyboardMode)
            {
                // this means we want to switch back to the letters keyboard mode
                ActiveKeyboardMode = KeyboardMode.Letters;
            }

            // switch back to the previous mode if already pressed
            VirtualKeyboard.Instance.EnterCharacter(' ');
        }

        private void PressKey(char character)
        {
            _pressedKeyInCurrentKeyboardMode = true;

            if (Shift && ActiveKeyboardMode == KeyboardMode.Letters)
            {
                Shift = false;
            }

            VirtualKeyboard.Instance.EnterCharacter(character);
        }

        private void UpdateShiftKeyAppearance()
        {
            bool showShift = ActiveKeyboardMode == KeyboardMode.Letters;

            Sprite icon = showShift ? Icons.Data.KeyboardShiftKey : null;
            string text = showShift ? null :
                (Shift ? k_ShiftKeyNumbers : k_ShiftKeyPunctuation);

            _keyLeftShift.Pressed = showShift && Shift;
            _keyRightShift.Pressed = showShift && Shift;

            _keyLeftShift.ActiveData.Icon = icon;
            _keyRightShift.ActiveData.Icon = icon;

            _keyLeftShift.ActiveData.Text = text;
            _keyRightShift.ActiveData.Text = text;

            _keyLeftShift.DataChanged();
            _keyRightShift.DataChanged();
        }

        private const string k_ShiftKeyNumbers = "123";
        private const string k_ShiftKeyPunctuation = "#+=";
        private const string k_NumbersPunctuationKeyString = ".?123";
        private const string k_LettersKeyString = "ABC";

        private void OnShiftChanged(bool shift)
        {
            UpdateKeyCharacters();
            UpdateShiftKeyAppearance();
        }

        private bool _pressedKeyInCurrentKeyboardMode = true;

        private void OnKeyboardModeChanged(KeyboardMode keyboardMode)
        {
            _pressedKeyInCurrentKeyboardMode = false;
            Shift = false;

            // update the string
            string keyString = ActiveKeyboardMode == KeyboardMode.Letters ? k_NumbersPunctuationKeyString : k_LettersKeyString;
            _keyLeftNumbersPunctuation.ActiveData.Text = keyString;
            _keyRightNumbersPunctuation.ActiveData.Text = keyString;
            _keyLeftNumbersPunctuation.DataChanged();
            _keyRightNumbersPunctuation.DataChanged();

            UpdateKeyCharacters();
            UpdateShiftKeyAppearance();
        }

        private void UpdateKeyCharacters()
        {
            KeyboardLayoutData data = _data;

            // first get the right characters
            string[] keyStrings = null;
            if (ActiveKeyboardMode == KeyboardMode.Letters)
            {
                keyStrings = Shift ? data.LettersShift : data.Letters;
            }
            else if (ActiveKeyboardMode == KeyboardMode.NumbersPunctuation)
            {
                keyStrings = Shift ? data.NumbersPunctuationShift : data.NumbersPunctuation;
            }

            if (keyStrings == null) { return; }

            for (int i = 0; i < 3; i++) // rows
            {
                List<Button> keys = _keys[i];

                for (int j = 0; j < keys.Count; j++) // keys
                {
                    Button key = keys[j];

                    if (j < keyStrings[i].Length)
                    {
                        char character = keyStrings[i][j];
                        key.ActiveData.Text = character.ToString();
                        key.ActiveData.OnPressed = () => PressKey(character);
                    }
                    else
                    {
                        key.ActiveData.Text = null;
                        key.ActiveData.OnPressed = null;
                    }
                    key.DataChanged();
                }
            }
        }

        private App _app;

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();

            // Set actions

            _onSourceInputFieldConfirmValue = (value) =>
            {
                // _sourceInputField.text is to make sure you don't use the value that is passed by the keyboard popup
                // but the one that could have been changed, e.g. by the ValueField that performs an Evaluate function
                // on set value.
                // This back-propagates those calculated changes to this popup again. 
                SetValueWithoutNotify(_sourceInputField.text);
            };

            OnSetValue += (value) =>
            {
                if (_sourceInputField == null) { return; }

                _sourceInputField.text = value;
                _sourceInputField.OnSetValue?.Invoke(value);
            };

            OnConfirmValue += (value) =>
            {
                if (_sourceInputField == null) { return; }

                _sourceInputField.text = value;
                _sourceInputField.OnConfirmValue?.Invoke(value);
            };
        }

        private void Start()
        {
            _app = App.Instance;
            _onKeyboardLayoutChanged = OnKeyboardLayoutChanged;

            if (_inputField != null)
            {
                _inputField.Select();
                _inputField.onValueChanged.AddListener((value) =>
                {
                    _value = value;
                    OnSetValue?.Invoke(value);
                });

                _inputField.OnConfirmValue += (value) =>
                {
                    _value = value;
                    CloseKeyboard();// calls on confirm value
                };
            }

            InstantiateSpecialKeys();

            Register();
        }

        private string _value;

        public Action<string> OnSetValue;
        public Action<string> OnConfirmValue;

        /// <summary>
        /// Sets the value of the input field
        /// </summary>
        /// <param name="value"></param>
        public void SetValueWithoutNotify(string value)
        {
            _value = value;
            _inputField.SetTextWithoutNotify(value);
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

        private void Register()
        {
            if (_app != null)
            {
                _app.KeyboardLayout.Register(_onKeyboardLayoutChanged);
            }

            if (_sourceInputField != null)
            {
                SetValueWithoutNotify(_sourceInputField.text);

                _sourceInputField.OnConfirmValue += _onSourceInputFieldConfirmValue;
            }
        }

        private void Unregister()
        {
            if (_app != null)
            {
                _app.KeyboardLayout.Unregister(_onKeyboardLayoutChanged);
            }

            if (_sourceInputField != null)
            {
                _sourceInputField.OnConfirmValue -= _onSourceInputFieldConfirmValue;
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
