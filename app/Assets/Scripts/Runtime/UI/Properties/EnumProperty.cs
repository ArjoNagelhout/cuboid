using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Cuboid.UI
{
    [RequireComponent(typeof(PopupButton))]
    public class EnumProperty : MonoBehaviour, IGenericProperty
    {
        [SerializeField] private PopupButton _popupButton;

        IDisposable property = null;

        public Property<T> CreateProperty<T>()
        {
            EnumPropertyInternal<T> enumProperty = new EnumPropertyInternal<T>(_popupButton);
            property = enumProperty;
            return enumProperty;
        }

        private void OnDestroy()
        {
            property.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        public class EnumPropertyInternal<T> : Property<T>
        {
            private PopupButton _popupButton;

            private IBinding<T> _popupButtonBinding = new BindingWithoutNew<T>();

            public EnumPropertyInternal(PopupButton popupButton) : base()
            {
                _popupButton = popupButton;

                Type enumType = typeof(T);
                MethodInfo method = typeof(PopupButton).GetMethod("CreatePopupButton");
                var genericMethod = method.MakeGenericMethod(enumType);
                Action<T> onSetValue = (value) => { _sourceBinding.Value = value; ConfirmValue(); };
                genericMethod.Invoke(_popupButton, new object[] { _popupButtonBinding, onSetValue });
            }

            protected override void OnValueChanged(T value)
            {
                base.OnValueChanged(value);

                _popupButtonBinding.Value = value;
            }

            protected override void CalculateIsEditingMultiple()
            {
                //_popupButtonNew.EditingMultiple = IsEditingMultiple(_targetBindings);
            }

            protected override void SetBinding(IBinding<T> binding)
            {
                base.SetBinding(binding);
                if (binding != null)
                {
                    _popupButtonBinding.Value = binding.Value;
                }
            }
        }
    }
}
