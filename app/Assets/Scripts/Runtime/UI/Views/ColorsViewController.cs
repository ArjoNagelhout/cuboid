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
    /// The ColorsViewController should show the color of the currently selected object.
    /// If it supports color editing. Otherwise it will simply set the color for the
    /// current tool (e.g. DrawShapeTool).
    ///
    /// Whether any of the objects can get their color set is determined by whether
    /// they implement the IHasColor interface. 
    /// </summary>
    public class ColorsViewController : MonoBehaviour
    {
        /// <summary>
        /// The color picker that the binding should be set of. 
        /// </summary>
        [SerializeField] private ColorPicker _colorPicker;

        private ColorsController _colorsController;

        private void Start()
        {
            _colorsController = ColorsController.Instance;

            _colorPicker.SetBinding(_colorsController.ActiveColor);
            _colorPicker.OnSetValue = _colorsController.OnSetValue;
            _colorPicker.OnConfirmValue = _colorsController.OnConfirmValue;
        }
    }
}
