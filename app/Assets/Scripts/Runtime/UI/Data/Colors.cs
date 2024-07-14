// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cuboid.UI;

namespace Cuboid.UI
{
    /// <summary>
    /// Colors for the application. 
    /// </summary>
    [Serializable]
    public class ButtonColors
    {
        public ColorsScriptableObject Plain;
        public ColorsScriptableObject Soft;
        public ColorsScriptableObject Solid;
        public ColorsScriptableObject Clear;

        /// <summary>
        /// Variant that can be exposed to the Unity editor to pick an appearance
        /// </summary>
        [Serializable]
        public enum Variant
        {
            Plain,
            Soft,
            Solid,
            Clear
        }

        public ColorsScriptableObject Get(Variant variant) => variant switch 
        {
            Variant.Plain => Plain,
            Variant.Soft => Soft,
            Variant.Solid => Solid,
            Variant.Clear => Clear,
            _ => Plain
        };
    }
}
