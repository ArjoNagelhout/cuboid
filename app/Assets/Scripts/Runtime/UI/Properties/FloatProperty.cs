//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.UI
{
    public class FloatProperty : MonoBehaviour, IProperty<float>
    {
        [SerializeField] private Slider _slider;
        [SerializeField] private ValueField _valueField;

        [SerializeField] private float _valueFieldWidth = 100f;
        [SerializeField] private float _padding = 10f;

        public Property<float> Property { get; private set; }

        private void Awake()
        {
            Property = new FloatPropertyInternal(_slider, _valueField, _valueFieldWidth, _padding);
        }

        private void OnDestroy()
        {
            Property.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        public class FloatPropertyInternal : Property<float>
        {
            private Slider _slider;
            private ValueField _valueField;
            private float _valueFieldWidth;
            private float _padding;

            private IBinding<float> _internalBinding = new Binding<float>();

            public FloatPropertyInternal(Slider slider, ValueField valueField, float valueFieldWidth, float padding) : base()
            {
                _slider = slider;
                _valueField = valueField;
                _valueFieldWidth = valueFieldWidth;
                _padding = padding;

                _slider.SetBinding(_internalBinding);
                _slider.OnSetValue = (value) => { _sourceBinding.Value = value; SetValue(); };
                _slider.OnConfirmValue = (value) => { _sourceBinding.Value = value; ConfirmValue(); };

                _valueField.SetBinding(_internalBinding);
                _valueField.OnSetValue = (value) => { _sourceBinding.Value = value; ConfirmValue(); };
            }

            /// <summary>
            /// Also to be used by Int slider
            /// </summary>
            public static void UpdateSliderValueFieldVisibility(Slider slider, ValueField valueField,
                bool showSlider, bool showValueField, float valueFieldWidth, float padding)
            {
                slider.gameObject.SetActive(showSlider);
                valueField.gameObject.SetActive(showValueField);

                RectTransform sliderRectTransform = slider.GetComponent<RectTransform>();
                RectTransform valueFieldRectTransform = valueField.GetComponent<RectTransform>();

                // set slider transform
                float rightOffset = showValueField ? -(valueFieldWidth + padding) : 0;
                sliderRectTransform.sizeDelta = sliderRectTransform.sizeDelta.SetX(rightOffset);
                sliderRectTransform.anchoredPosition = sliderRectTransform.anchoredPosition.SetX(rightOffset);

                // set value field transform
                bool fullWidthInputField = showValueField && !showSlider;
                valueFieldRectTransform.anchorMin = valueFieldRectTransform.anchorMin.SetX(fullWidthInputField ? 0 : 1);
                valueFieldRectTransform.sizeDelta = valueFieldRectTransform.sizeDelta.SetX(fullWidthInputField ? 0 : valueFieldWidth);
            }

            protected override void OnDataChanged(PropertiesController.RuntimeSerializedPropertyData data)
            {
                base.OnDataChanged(data);

                // set the slider values
                RuntimeSerializedPropertyFloat property = (data.PropertyAttribute as RuntimeSerializedPropertyFloat);
                if (property == null) { return; }

                UpdateSliderValueFieldVisibility(_slider, _valueField, property.Slider, property.InputField, _valueFieldWidth, _padding);

                _slider.MinValue = property.Min;
                _slider.MaxValue = property.Max;
            }

            protected override void OnValueChanged(float value)
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

            protected override void SetBinding(IBinding<float> binding)
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
