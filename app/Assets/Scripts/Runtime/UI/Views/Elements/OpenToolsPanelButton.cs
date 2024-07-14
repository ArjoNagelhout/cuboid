//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.UI
{
    /// <summary>
    /// Displays the currently active tool in the dock and when pressed opens
    /// the Tools View that allows the user to select a certain tool and set
    /// its options.
    ///
    /// Doesn't subclass from button, but keeps a reference,
    /// so that it can keep the prefab settings from the base button prefab.
    /// </summary>
    public class OpenToolsPanelButton : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private ToolController _toolController;
        private UIController _uiController;

        private Action<ToolController.Tool> _onActiveToolChanged;
        private Action<UIController.Panel> _onActivePanelChanged;

        private void Start()
        {
            _toolController = ToolController.Instance;
            _uiController = UIController.Instance;

            _onActiveToolChanged = OnActiveToolChanged;
            _onActivePanelChanged = OnActivePanelChanged;

            _button.ActiveData = new Button.Data()
            {
                OnPressed = OnPressed,
                Variant = ButtonColors.Variant.Soft
            };

            Register();
        }

        private void OnActiveToolChanged(ToolController.Tool tool)
        {
            if (_toolController.ToolsData.TryGetValue(tool, out ToolController.ToolData data))
            {
                _button.ActiveData.Text = data.Name;
                _button.ActiveData.Icon = data.Icon;
            }
            else
            {
                _button.ActiveData.Text = "";
                _button.ActiveData.Icon = null;
            }
            _button.DataChanged();
        }

        private void OnActivePanelChanged(UIController.Panel panel)
        {
            _button.Active = panel == UIController.Panel.Tools;
        }

        private void OnPressed()
        {
            // open the tools panel
            _uiController.ActivePanel.Value = UIController.Panel.Tools;
        }

        #region Action registration

        private void Register()
        {
            if (_toolController != null)
            {
                _toolController.ActiveTool.Register(_onActiveToolChanged);
            }

            if (_uiController != null)
            {
                _uiController.ActivePanel.Register(_onActivePanelChanged);
            }
        }

        private void Unregister()
        {
            if (_toolController != null)
            {
                _toolController.ActiveTool.Unregister(_onActiveToolChanged);
            }

            if (_uiController != null)
            {
                _uiController.ActivePanel.Unregister(_onActivePanelChanged);
            }
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
