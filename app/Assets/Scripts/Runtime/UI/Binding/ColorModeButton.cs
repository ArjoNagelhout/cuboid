//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.UI
{
    public class ColorModeButton : BindingButton<RealityColorMode>
    {
        protected override IBinding<RealityColorMode> GetBinding() => App.Instance.ColorMode;

        protected override void OnValueChanged(RealityColorMode data)
        {
            _button.Active = data == _associatedData;
        }
    }
}
