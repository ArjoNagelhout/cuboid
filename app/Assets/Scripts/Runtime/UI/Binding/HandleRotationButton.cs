//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.UI
{
    public class HandleRotationButton : BindingButton<HandleRotation>
    {
        protected override IBinding<HandleRotation> GetBinding() => App.Instance.CurrentHandleRotation;

        protected override void OnValueChanged(HandleRotation data)
        {
            _button.Active = data == _associatedData;
        }
    }
}