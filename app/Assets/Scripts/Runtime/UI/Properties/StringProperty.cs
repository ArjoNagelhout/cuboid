// Copyright (c) 2023 Arjo Nagelhout

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.UI
{
    public class StringProperty : MonoBehaviour, IProperty<string>
    {
        public Property<string> Property { get; private set; }

        [SerializeField] private InputField _inputField;

        private void Awake()
        {
            Property = new StringPropertyInternal(_inputField);
        }

        private void OnDestroy()
        {
            Property.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        public class StringPropertyInternal : Property<string>
        {
            private InputField _inputField;

            private IBinding<string> _internalBinding = new BindingWithoutNew<string>();

            public StringPropertyInternal(InputField inputField) : base()
            {
                _inputField = inputField;

                _internalBinding.OnValueChanged += (value) => { _inputField.text = value; };

                _inputField.OnSetValue = (value) => { _sourceBinding.Value = value; SetValue(); };
                _inputField.OnConfirmValue = (value) => { _sourceBinding.Value = value; ConfirmValue(); };
            }

            protected override void OnValueChanged(string value)
            {
                base.OnValueChanged(value);

                _internalBinding.Value = value;
            }

            protected override void CalculateIsEditingMultiple()
            {
                _inputField.IsEditingMultiple = IsEditingMultiple(_targetBindings);
            }
        }
    }
}
