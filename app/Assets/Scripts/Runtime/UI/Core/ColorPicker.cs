// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cuboid.UI
{
    /// <summary>
    /// Generic view that is to be used both in the Colors panel and the ColorPickerPopup
    /// </summary>
    public class ColorPicker : MonoBehaviour,
        ICanSetBinding<RealityColor>
    {
        private const int k_RGBMaxValue = 255;
        private const int k_HueMaxValue = 360;
        private const int k_SVMaxValue = 100;

        private Binding<Vector2> _internalColorPickerBinding = new();

        private App _app;
        private Action<RealityColorMode> _onColorModeChanged;

        private bool _isEditingMultiple;
        public bool IsEditingMultiple
        {
            get => _isEditingMultiple;
            set
            {
                _isEditingMultiple = value;

                _red.IsEditingMultiple = _isEditingMultiple;
                _green.IsEditingMultiple = _isEditingMultiple;
                _blue.IsEditingMultiple = _isEditingMultiple;

                _hue.IsEditingMultiple = _isEditingMultiple;
                _saturation.IsEditingMultiple = _isEditingMultiple;
                _value.IsEditingMultiple = _isEditingMultiple;

                _colorPickerSlider2D.IsEditingMultiple = _isEditingMultiple;
            }
        }

        [Serializable]
        private class SliderWithValueField
        {
#if UNITY_EDITOR

            internal Material _temporaryMaterial;

            /// <summary>
            /// To be used in OnApplicationQuit method, only in Unity Editor
            /// </summary>
            internal void _RestoreMaterialProperties()
            {
                BackgroundMaterial.CopyPropertiesFromMaterial(_temporaryMaterial);
            }
#endif

            public Material BackgroundMaterial;

            public Slider Slider;
            public ValueField ValueField;

            [NonSerialized] public Binding<int> Binding = new();

            private Binding<float> _internalBinding = new();

            private bool _isEditingMultiple;
            public bool IsEditingMultiple
            {
                get => _isEditingMultiple;
                set
                {
                    _isEditingMultiple = value;
                    Slider.IsEditingMultiple = _isEditingMultiple;
                    ValueField.IsEditingMultiple = _isEditingMultiple;
                }
            }

            public void Initialize(int minValue, int maxValue)
            {
                Slider.SetBinding(_internalBinding);
                ValueField.SetBinding(_internalBinding);

                Slider.MinValue = minValue;
                Slider.MaxValue = maxValue;

#if UNITY_EDITOR

                // make sure to copy the background material, otherwise it will set the values and constantly
                // get tracked in version control.
                // from https://answers.unity.com/questions/1537335/why-changing-material-shader-at-runtime-also-affec.html

                _temporaryMaterial = new Material(BackgroundMaterial);
#endif

                Binding.OnValueChanged += (value) => { _internalBinding.Value = (float)value; };

                Slider.OnSetValue += (value) => SetValue(value);
                Slider.OnConfirmValue += (value) => ConfirmValue(value);
                ValueField.OnSetValue += (value) => { SetValue(value); };
                ValueField.OnConfirmValue += (value) => { ConfirmValue(value); };
            }

            private void SetValue(float value)
            {
                int newValue = Mathf.RoundToInt(value);
                _internalBinding.Value = newValue;
                Binding.Value = newValue;
                OnSetValue?.Invoke(newValue);
            }

            private void ConfirmValue(float value)
            {
                int newValue = Mathf.RoundToInt(value);
                _internalBinding.Value = newValue;
                OnConfirmValue?.Invoke(newValue);
            }

            public Action<int> OnSetValue;
            public Action<int> OnConfirmValue;
        }

        [SerializeField] private Material _hsvColorPickerMaterial;
        [SerializeField] private Material _rgbColorPickerMaterial;

        [SerializeField] private Image _colorPickerBackgroundImage;

        [SerializeField] private Image _currentColorImage;

#if UNITY_EDITOR
        private Material _hsvColorPickerTemporaryMaterial;
        private Material _rgbColorPickerTemporaryMaterial;
#endif

        private IBinding<RealityColor> _binding;
        private Action<RealityColor> _onValueChanged;
        public Action<RealityColor> OnSetValue;
        public Action<RealityColor> OnConfirmValue;

        [SerializeField] private Slider2D _colorPickerSlider2D;

        [SerializeField] private GameObject _hsvSliders;
        [SerializeField] private SliderWithValueField _hue;
        [SerializeField] private SliderWithValueField _saturation;
        [SerializeField] private SliderWithValueField _value;

        [SerializeField] private GameObject _rgbSliders;
        [SerializeField] private SliderWithValueField _red;
        [SerializeField] private SliderWithValueField _green;
        [SerializeField] private SliderWithValueField _blue;

        private void Awake()
        {
            _hue.Initialize(0, k_HueMaxValue);
            _saturation.Initialize(0, k_SVMaxValue);
            _value.Initialize(0, k_SVMaxValue);

            _red.Initialize(0, k_RGBMaxValue);
            _green.Initialize(0, k_RGBMaxValue);
            _blue.Initialize(0, k_RGBMaxValue);

            _colorPickerSlider2D.SetBinding(_internalColorPickerBinding);

#if UNITY_EDITOR
            _hsvColorPickerTemporaryMaterial = new Material(_hsvColorPickerMaterial);
            _rgbColorPickerTemporaryMaterial = new Material(_rgbColorPickerMaterial);
#endif
        }

        private void Start()
        {
            _hue.OnSetValue += (value) => { OnHSVChanged(); SetValue(); };
            _saturation.OnSetValue += (value) => { OnHSVChanged(); SetValue(); };
            _value.OnSetValue += (value) => { OnHSVChanged(); SetValue(); };

            _red.OnSetValue += (value) => { OnRGBChanged(); SetValue(); };
            _green.OnSetValue += (value) => { OnRGBChanged(); SetValue(); };
            _blue.OnSetValue += (value) => { OnRGBChanged(); SetValue(); };

            _colorPickerSlider2D.OnSetValue += (value) => { OnColorPickerChanged(); SetValue(); };

            _hue.OnConfirmValue += (value) => { OnHSVChanged(); ConfirmValue(); };
            _saturation.OnConfirmValue += (value) => { OnHSVChanged(); ConfirmValue(); };
            _value.OnConfirmValue += (value) => { OnHSVChanged(); ConfirmValue(); };

            _red.OnConfirmValue += (value) => { OnRGBChanged(); ConfirmValue(); };
            _green.OnConfirmValue += (value) => { OnRGBChanged(); ConfirmValue(); };
            _blue.OnConfirmValue += (value) => { OnRGBChanged(); ConfirmValue(); };

            _colorPickerSlider2D.OnConfirmValue += (value) => { OnColorPickerChanged(); ConfirmValue(); };

            _onValueChanged = OnValueChanged;

            _app = App.Instance;
            _onColorModeChanged = OnColorModeChanged;

            Register();
        }

        private void OnColorModeChanged(RealityColorMode mode)
        {
            _hsvSliders.SetActive(mode == RealityColorMode.HSV);
            _rgbSliders.SetActive(mode == RealityColorMode.RGB);
            _colorPickerBackgroundImage.material = mode == RealityColorMode.HSV ? _hsvColorPickerMaterial : _rgbColorPickerMaterial;
            UpdateColorPicker();
        }

        private void SetValue()
        {
            if (_binding != null)
            {
                OnSetValue?.Invoke(_binding.Value);
            }
        }

        private void ConfirmValue()
        {
            if (_binding != null)
            {
                OnConfirmValue?.Invoke(_binding.Value);
            }
        }

        private void OnColorPickerChanged()
        {
            switch (App.Instance.ColorMode.Value)
            {
                case RealityColorMode.RGB:
                    {
                        Vector2 hv = _internalColorPickerBinding.Value;
                        float hue = hv.x;
                        // because we try to "sample" the color from the given position,
                        // the saturation will always be 1
                        float saturation = 1f;
                        float value = hv.y;

                        _binding.Value = RealityColor.HSV(hue, saturation, value);
                    }
                    break;
                case RealityColorMode.HSV:
                    {
                        Vector2 sv = _internalColorPickerBinding.Value;
                        float hue = (float)_hue.Binding.Value / k_HueMaxValue;
                        float saturation = sv.x;
                        float value = sv.y;

                        _binding.Value = RealityColor.HSV(hue, saturation, value);
                    }
                    break;
            }
        }

        private void OnHSVChanged()
        {
            float hue = (float)_hue.Binding.Value / k_HueMaxValue;
            float saturation = (float)_saturation.Binding.Value / k_SVMaxValue;
            float value = (float)_value.Binding.Value / k_SVMaxValue;
            _binding.Value = RealityColor.HSV(hue, saturation, value);
        }

        private void OnRGBChanged()
        {
            byte r = (byte)_red.Binding.Value;
            byte g = (byte)_green.Binding.Value;
            byte b = (byte)_blue.Binding.Value;
            _binding.Value = RealityColor.RGB(r, g, b);
        }

        private void UpdateColorPicker()
        {
            RealityColor color = _binding.Value;
            _hsvColorPickerMaterial.SetFloat("_Hue", color.hue);

            switch (App.Instance.ColorMode.Value)
            {
                case RealityColorMode.HSV:
                    _internalColorPickerBinding.Value = new Vector2(color.saturation, color.value);
                    break;
                case RealityColorMode.RGB:
                    _internalColorPickerBinding.Value = new Vector2(color.hue, color.value);
                    break;
            }
        }

#if UNITY_EDITOR

        private void RestoreMaterials()
        {
            _hue._RestoreMaterialProperties();
            _saturation._RestoreMaterialProperties();
            _value._RestoreMaterialProperties();
            _red._RestoreMaterialProperties();
            _green._RestoreMaterialProperties();
            _blue._RestoreMaterialProperties();

            _hsvColorPickerMaterial.CopyPropertiesFromMaterial(_hsvColorPickerTemporaryMaterial);
            _rgbColorPickerMaterial.CopyPropertiesFromMaterial(_rgbColorPickerTemporaryMaterial);
        }

        //private void OnApplicationQuit()
        //{
        //    RestoreMaterials();
        //}
#endif

        private void OnValueChanged(RealityColor color)
        {
            // we only want to update the values if the color has been set by external factors:
            // - the _targetBindings get updated
            // - the color picker is first instantiated

            // otherwise, when setting the HSV values, it will circumvent all of the special
            // logic to make sure the hue for example stays intact even when the color is black.

            // we could add more data to the undo / redo command, maybe create a data structure
            // that holds whether the color was set with HSV or RGB

            // otherwise we are just in the dark and need to do some hacky things to make sure the
            // HSV and RGB

            int r = (int)(color.r);
            int g = (int)(color.g);
            int b = (int)(color.b);
            int h = (int)(color.hue * k_HueMaxValue);
            int s = (int)(color.saturation * k_SVMaxValue);
            int v = (int)(color.value * k_SVMaxValue);

            _red.Binding.Value = r;
            _green.Binding.Value = g;
            _blue.Binding.Value = b;
            _hue.Binding.Value = h;
            _saturation.Binding.Value = s;
            _value.Binding.Value = v;

            UpdateColorPicker();

            Color32 color32 = _binding.Value.ToColor32();

            _hue.BackgroundMaterial.SetColor("_MainColor", color32);
            _saturation.BackgroundMaterial.SetColor("_MainColor", color32);
            _value.BackgroundMaterial.SetColor("_MainColor", color32);

            _saturation.BackgroundMaterial.SetFloat("_Hue", color.hue);
            _value.BackgroundMaterial.SetFloat("_Hue", color.hue);
            _value.BackgroundMaterial.SetFloat("_Saturation", color.saturation);

            _red.BackgroundMaterial.SetColor("_MainColor", color32);
            _green.BackgroundMaterial.SetColor("_MainColor", color32);
            _blue.BackgroundMaterial.SetColor("_MainColor", color32);
        }

        #region Action registration

        public void SetBinding(IBinding<RealityColor> binding)
        {
            Unregister();
            _binding = binding;
            Register();
        }

        private void Register()
        {
            if (_binding != null)
            {
                _binding.Register(_onValueChanged);
            }

            if (_app != null)
            {
                _app.ColorMode.Register(_onColorModeChanged);
            }
        }

        private void Unregister()
        {
            if (_binding != null)
            {
                _binding.Unregister(_onValueChanged);
            }

            if (_app != null)
            {
                _app.ColorMode.Unregister(_onColorModeChanged);
            }

#if UNITY_EDITOR
            RestoreMaterials();
#endif
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
