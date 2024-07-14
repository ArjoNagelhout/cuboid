// Copyright (c) 2023 Arjo Nagelhout

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.UI
{
    [RequireComponent(typeof(Toggle))]
    public class BooleanProperty : MonoBehaviour, IProperty<bool>
    {
        private Toggle _toggle;
        public Property<bool> Property { get; private set; }

        private void Awake()
        {
            _toggle = GetComponent<Toggle>();
            Property = new BooleanPropertyInternal(_toggle);
        }

        private void OnDestroy()
        {
            Property.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        public class BooleanPropertyInternal : Property<bool>
        {
            private Toggle _toggle;
            private IBinding<bool> _toggleBinding = new Binding<bool>();

            public BooleanPropertyInternal(Toggle toggle) : base()
            {
                _toggle = toggle;
                _toggle.SetBinding(_toggleBinding);
                _toggle.OnSetValue = (value) => { _sourceBinding.Value = value; ConfirmValue(); };
            }

            protected override void OnValueChanged(bool value)
            {
                base.OnValueChanged(value);

                _toggleBinding.Value = value;
            }

            protected override void CalculateIsEditingMultiple()
            {
                _toggle.IsEditingMultiple = IsEditingMultiple(_targetBindings);
            }

            protected override void SetBinding(IBinding<bool> binding)
            {
                base.SetBinding(binding);
                if (binding != null)
                {
                    _toggleBinding.Value = binding.Value;
                }
            }
        }
    }
}
