using System.Collections;
using System.Collections.Generic;
using Cuboid.Input;
using UnityEngine;

namespace Cuboid.UI
{
    public class HandlePositionButton : BindingButton<HandlePosition>
    {
        protected override IBinding<HandlePosition> GetBinding() => App.Instance.CurrentHandlePosition;

        protected override void OnValueChanged(HandlePosition data)
        {
            _button.Active = data == _associatedData;
        }
    }
}
