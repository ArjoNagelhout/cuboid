// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cuboid.UI
{
    public class MenuButton : BindingButton<UIController.Panel>
    {
        protected override IBinding<UIController.Panel> GetBinding() => UIController.Instance.ActivePanel;

        protected override void OnValueChanged(UIController.Panel data)
        {
            _button.Active = data == _associatedData;
        }
    }
}
