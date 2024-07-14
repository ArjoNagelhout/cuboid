//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.UI
{
    /// <summary>
    /// this is to ensure that the Toggle code stays clean and can only be interfaced
    /// with by SetBinding<bool> calls, same for the slider, InputField and future UI
    /// components.
    /// 
    /// Also when creating prefab variants for the Toggle, all serialized fields of
    /// Toggle(such as the background image and the handle image) don't need to be
    /// reassigned each time.
    ///
    /// Additionally, having this component allows us to add it in the Unity Editor,
    /// without having to add another script to do the binding, such as the
    /// SettingsViewController.
    /// This class can eventually be removed when views are composed mainly via
    /// ViewControllers, where the Binding can be set. <see cref="PropertiesViewController"/>
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class BindingToggle : MonoBehaviour
    {
        [System.Serializable]
        public enum Identifier
        {
            App_ShowAdvancedCursor,
            App_ShowWorldAxes,
            App_ShowWorldYGrid,
            App_Passthrough
        }

        private App App => App.Instance;

        [Header("Data Binding")]
        [SerializeField] private Identifier _identifier;

        private Toggle _toggle;

        private void Start()
        {
            _toggle = GetComponent<Toggle>();
            _toggle.SetBinding(_identifier switch
            {
                Identifier.App_ShowAdvancedCursor => App.ShowAdvancedCursor,
                Identifier.App_ShowWorldAxes => App.ShowWorldAxes,
                Identifier.App_ShowWorldYGrid => App.ShowWorldYGrid,
                Identifier.App_Passthrough => App.Passthrough,
                _ => null
            });
        }
    }
}
