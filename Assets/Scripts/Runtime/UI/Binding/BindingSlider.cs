using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cuboid.UI
{
    /// <summary>
    /// See explanation at <see cref="BindingToggle"/> for why
    /// this is not a subclass of <see cref="Slider"/> and why it exists. 
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class BindingSlider : MonoBehaviour
    {
        [System.Serializable]
        public enum Identifier
        {
            App_ImportScale
        }

        private App App => App.Instance;

        /// <summary>
        /// Optional value field that should get the data binding as well. 
        /// </summary>
        [SerializeField] private ValueField _valueField;

        [Header("Data Binding")]
        [SerializeField] private Identifier _identifier;

        private Slider _slider;

        private void Start()
        {
            _slider = GetComponent<Slider>();

            IBinding<float> binding = _identifier switch
            {
                Identifier.App_ImportScale => App.ImportScale,
                _ => null
            };
            _slider.SetBinding(binding);

            if (_valueField != null && binding != null)
            {
                _valueField.SetBinding(binding);
            }
        }
    }
}
