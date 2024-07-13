using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.UI
{
    public class PopupButton : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private IDisposable _createdPopupButton;

        public void CreatePopupButton<T>(IBinding<T> binding, Action<T> onSetValue = null) where T : Enum, new()
        {
            PopupButtonInternal<T> button = new(_button, binding);
            _createdPopupButton = button;
            button.OnSetValue = onSetValue;
        }

        private void OnDestroy()
        {
            if (_createdPopupButton != null)
            {
                _createdPopupButton.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public class PopupButtonInternal<T> : IDisposable, ICanSetBinding<T> where T : Enum, new()
        {
            private IBinding<T> _binding;
            private Action<T> _onValueChanged;
            private Button _button;

            public Action<T> OnSetValue;

            // constructor
            public PopupButtonInternal(Button button, IBinding<T> binding)
            {
                _onValueChanged = OnValueChanged;
                _button = button;

                _button.ActiveData = new Button.Data()
                {
                    OnPressed = () => OpenPopup()
                };

                SetBinding(binding);
            }

            public void SetBinding(IBinding<T> binding)
            {
                Unregister();
                _binding = binding;
                Register();
            }

            private void OnValueChanged(T value)
            {
                // update the text
                EnumUtils.GetEnumData(value, out string text, out Sprite icon);
                _button.ActiveData.Icon = icon;
                _button.ActiveData.Text = text;
                _button.DataChanged();
            }

            private void OpenPopup()
            {
                List<Button.Data> buttons = new List<Button.Data>();

                Type enumType = typeof(T);
                Array values = Enum.GetValues(enumType);
                foreach (T value in values)
                {
                    EnumUtils.GetEnumData(value, out string text, out Sprite icon);

                    bool active = _binding.Value.Equals(value);
                    buttons.Add(new Button.Data()
                    {
                        Text = text,
                        OnPressed = () =>
                        {
                            _binding.Value = value;
                            OnSetValue?.Invoke(_binding.Value);
                            PopupsController.Instance.CloseLastPopup();
                        },
                        Icon = (icon == null && active) ? Icons.Data.Check : icon,
                        Variant = active ? ButtonColors.Variant.Soft : ButtonColors.Variant.Plain
                    });
                }

                PrettyTypeNameAttribute prettyTypeName = enumType.GetCustomAttribute<PrettyTypeNameAttribute>();
                string title = prettyTypeName != null ? prettyTypeName.Name : enumType.Name;

                // open the popup, currently a simple context menu
                PopupsController.Instance.OpenContextMenu(new ContextMenu.ContextMenuData()
                {
                    Title = title,
                    Buttons = buttons
                });
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

            public void Dispose()
            {
                Unregister();
            }
        }
    }
}
