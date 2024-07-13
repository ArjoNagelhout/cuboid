// 
// ButtonColorsScriptableObject.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Cuboid.UI
{
    /// <summary>
    /// 
    /// </summary>
    [CreateAssetMenu(fileName = "Colors", menuName = "ShapeReality/Colors")]
    public class ColorsScriptableObject : ScriptableObject
    {
        public Color Disabled;

        public Color DisabledText;

        [Header("Normal")]
        public Color Normal;

        public Color Hover;

        public Color Pressed;

        public Color Text;

        [Header("Active")]
        public Color Active;

        public Color ActiveHover;

        public Color ActivePressed;

        public Color ActiveText;
    }
}

