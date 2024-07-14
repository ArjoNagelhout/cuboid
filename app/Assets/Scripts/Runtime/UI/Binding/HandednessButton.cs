//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cuboid.Input;

namespace Cuboid.UI
{
    public class HandednessButton : BindingButton<Handedness.Hand>
    {
        protected override IBinding<Handedness.Hand> GetBinding() => InputController.Instance.Handedness.DominantHand;

        protected override void OnValueChanged(Handedness.Hand data)
        {
            _button.Active = data == _associatedData;
        }
    }
}

