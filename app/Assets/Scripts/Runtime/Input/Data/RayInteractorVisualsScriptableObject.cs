//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.Input
{
    /// <summary>
    /// 
    /// </summary>
    [CreateAssetMenu(fileName = "RayInteractorVisuals", menuName = "ShapeReality/RayInteractorVisuals")]
    public class RayInteractorVisualsScriptableObject : ScriptableObject
    {
        [Header("Normal")]
        /// <summary>
        /// When the ray is not intersecting with anything in the scene
        /// </summary>
        public Gradient Normal;

        /// <summary>
        /// When the ray interactor hovers over an object that can be interacted with
        /// </summary>
        public Gradient Hovered;

        /// <summary>
        /// When the ray interactor hovers over an object that can be interacted with, and the user presses it
        /// </summary>
        public Gradient Pressed;

        [Header("Disabled")]
        /// <summary>
        /// When the user hovers over an object that is disabled (grayed out)
        /// </summary>
        public Gradient DisabledHovered;

        /// <summary>
        /// When the user hovers over an object that is disabled (grayed out) and tries to press it
        /// </summary>
        public Gradient DisabledPressed;

        [Header("Not Allowed")]
        /// <summary>
        /// When the user hovers over an object that is actively not allowed (red)
        /// </summary>
        public Gradient NotAllowedHovered;

        /// <summary>
        /// When the user hovers over an object that is actively not allowed (red) and tries to press it
        /// </summary>
        public Gradient NotAllowedPressed;
    }
}
