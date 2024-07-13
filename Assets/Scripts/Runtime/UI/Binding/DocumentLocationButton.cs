// 
// DocumentLocationButton.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cuboid.UI
{
    public class DocumentLocationButton : BindingButton<DocumentLocation>
    {
        protected override IBinding<DocumentLocation> GetBinding() => DocumentsViewController.Instance.DocumentLocation;

        protected override void OnValueChanged(DocumentLocation data)
        {
            _button.Active = data == _associatedData;
        }
    }
}

