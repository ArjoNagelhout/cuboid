// 
// ToolButton.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.UI
{
    public class ToolButton : BindingButton<ToolController.Tool>
    {
        private ToolController _toolController;

        protected new void Start()
        {
            _toolController = ToolController.Instance;

            if (_toolController.ToolsData.TryGetValue(_associatedData, out ToolController.ToolData toolData))
            {
                _button.ActiveData.Icon = toolData.Icon;
                _button.ActiveData.Text = toolData.Name;
                _button.DataChanged();
            }

            base.Start();
        }

        protected override IBinding<ToolController.Tool> GetBinding() => _toolController.ActiveTool;

        protected override void OnValueChanged(ToolController.Tool data)
        {
            _button.Active = data == _associatedData;
        }
    }
}
