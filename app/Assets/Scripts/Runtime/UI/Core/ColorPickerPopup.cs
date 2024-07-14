//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.UI
{
    public class ColorPickerPopup : MonoBehaviour
    {
        /// <summary>
        /// Reference to the color picker itself
        /// </summary>
        public ColorPicker ColorPicker;

        public void Close()
        {
            PopupsController.Instance.ClosePopup(gameObject);
        }
    }
}
