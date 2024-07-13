using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.UI
{
    public class IntProperty : MonoBehaviour, IProperty<int>
    {
        [SerializeField] private Slider _slider;
        [SerializeField] private ValueField _valueField;

        [SerializeField] private float _valueFieldWidth = 100f;
        [SerializeField] private float _padding = 10f;

        public Property<int> Property { get; private set; }

        private void Awake()
        {
            Property = new IntPropertyInternal(_slider, _valueField, _valueFieldWidth, _padding);
        }

        private void OnDestroy()
        {
            Property.Dispose();
        }

        public class IntPropertyInternal : Property<int>
        {
            private Slider _slider;
            private ValueField _valueField;
            private float _valueFieldWidth;
            private float _padding;

            private IBinding<float> _internalBinding = new Binding<float>();

            public IntPropertyInternal(Slider slider, ValueField valueField, float valueFieldWidth, float padding) : base()
            {
                _slider = slider;
                _valueField = valueField;
                _valueFieldWidth = valueFieldWidth;
                _padding = padding;

                _slider.SetBinding(_internalBinding);
                _slider.OnSetValue = (value) => { _sourceBinding.Value = Mathf.RoundToInt(value); SetValue(); };
                _slider.OnConfirmValue = (value) => { _sourceBinding.Value = Mathf.RoundToInt(value); ConfirmValue(); };

                _valueField.SetBinding(_internalBinding);
                _valueField.OnSetValue = (value) => { _sourceBinding.Value = Mathf.RoundToInt(value); ConfirmValue(); };
            }

            protected override void OnDataChanged(PropertiesController.RuntimeSerializedPropertyData data)
            {
                base.OnDataChanged(data);

                // set the slider values
                RuntimeSerializedPropertyInt property = (data.PropertyAttribute as RuntimeSerializedPropertyInt);
                if (property == null) { return; }

                FloatProperty.FloatPropertyInternal.UpdateSliderValueFieldVisibility(_slider, _valueField, property.Slider, property.InputField, _valueFieldWidth, _padding);

                _slider.MinValue = property.Min;
                _slider.MaxValue = property.Max;
            }

            protected override void OnValueChanged(int value)
            {
                base.OnValueChanged(value);

                _internalBinding.Value = value;
            }

            protected override void CalculateIsEditingMultiple()
            {
                bool isEditingMultiple = IsEditingMultiple(_targetBindings);
                _valueField.IsEditingMultiple = isEditingMultiple;
                _slider.IsEditingMultiple = isEditingMultiple;
            }

            protected override void SetBinding(IBinding<int> binding)
            {
                base.SetBinding(binding);
                if (binding != null)
                {
                    _internalBinding.Value = binding.Value;
                }
            }
        }
    }
}
