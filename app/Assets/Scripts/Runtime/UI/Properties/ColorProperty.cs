using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Cuboid.UI
{
    public class ColorProperty : MonoBehaviour, IProperty<RealityColor>
    {
        /// <summary>
        /// button with the current color as its image.
        /// When pressed, should open the popup
        /// </summary>
        [SerializeField] private UnityEngine.UI.Button _button;
        [SerializeField] private GameObject _isEditingMultipleGraphic;
        [SerializeField] private Color _isEditingMultipleColor;

        /// <summary>
        /// Similar to the ValueField, this is the popup that gets instantiated
        /// </summary>
        [SerializeField] private GameObject _colorPickerPopupPrefab;

        public Property<RealityColor> Property { get; private set; }

        private void Awake()
        {
            Property = new ColorPropertyInternal(_button, _isEditingMultipleGraphic, _isEditingMultipleColor, _colorPickerPopupPrefab);
        }

        private void OnDestroy()
        {
            Property.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        public class ColorPropertyInternal : Property<RealityColor>
        {
            private UnityEngine.UI.Button _button;
            private GameObject _isEditingMultipleGraphic;
            private Color _isEditingMultipleColor;

            private GameObject _colorPickerPopupPrefab;

            private IBinding<RealityColor> _internalBinding = new Binding<RealityColor>();

            private ColorPicker _instantiatedColorPicker;

            public ColorPropertyInternal(UnityEngine.UI.Button button, GameObject isEditingMultipleGraphic, Color isEditingMultipleColor, GameObject colorPickerPopupPrefab) : base()
            {
                _button = button;
                _isEditingMultipleGraphic = isEditingMultipleGraphic;
                _isEditingMultipleColor = isEditingMultipleColor;

                _colorPickerPopupPrefab = colorPickerPopupPrefab;

                _button.onClick.AddListener(OpenColorPopup);
            }

            /// <summary>
            /// used by button
            /// </summary>
            private void OpenColorPopup()
            {
                ColorPickerPopup colorPickerPopup = PopupsController.Instance.OpenPopup<ColorPickerPopup>(
                    _colorPickerPopupPrefab, true, new PopupsController.PopupParams() { DontInterceptUndoButtons = true });
                _instantiatedColorPicker = colorPickerPopup.ColorPicker;
                _instantiatedColorPicker.SetBinding(_internalBinding);
                _instantiatedColorPicker.OnSetValue += (value) => { _sourceBinding.Value = value; SetValue(); };
                _instantiatedColorPicker.OnConfirmValue += (value) => { _sourceBinding.Value = value; ConfirmValue(); };
                _instantiatedColorPicker.IsEditingMultiple = _isEditingMultiple;
            }

            protected override void OnValueChanged(RealityColor value)
            {
                base.OnValueChanged(value);

                _internalBinding.Value = value;

                //// set the button color
                UpdateButtonColor();
            }

            private bool _isEditingMultiple = false;

            protected override void CalculateIsEditingMultiple()
            {
                _isEditingMultiple = IsEditingMultiple(_targetBindings);
                UpdateButtonColor();
                _isEditingMultipleGraphic.SetActive(_isEditingMultiple);

                if (_instantiatedColorPicker != null)
                {
                    _instantiatedColorPicker.IsEditingMultiple = _isEditingMultiple;
                }
            }

            private void UpdateButtonColor()
            {
                _button.image.color = _isEditingMultiple ? _isEditingMultipleColor : _internalBinding.Value.ToColor32();
            }

            protected override void SetBinding(IBinding<RealityColor> binding)
            {
                base.SetBinding(binding);
                if (binding != null)
                {
                    _internalBinding.Value = binding.Value;
                }
            }

            public override void Dispose()
            {
                base.Dispose();

                if (_instantiatedColorPicker != null)
                {
                    PopupsController.Instance.CloseAllPopups();
                }
            }
        }
    }
}
