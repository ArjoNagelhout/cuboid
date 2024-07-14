//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid
{
    public class AxisHandleData : HandleData
    {
        public Axis axis;
    }

    public class AxisHandle<T> : Handle<T> where T : AxisHandleData
    {
        [SerializeField] private MeshRenderer _meshRenderer;

        /// <summary>
        /// To be set by tool
        /// </summary>
        [System.NonSerialized] public Material DefaultMaterial;

        /// <summary>
        /// To be set by tool
        /// </summary>
        [System.NonSerialized] public Material HoveredMaterial;

        /// <summary>
        /// To be set by tool
        /// </summary>
        [System.NonSerialized] public Material PressedMaterial;

        protected override void UpdateHoverPressedAppearance()
        {
            if (Pressed)
            {
                _meshRenderer.material = PressedMaterial;
            }
            else if (Hovered)
            {
                _meshRenderer.material = HoveredMaterial;
            }
            else
            {
                _meshRenderer.material = DefaultMaterial;
            }
        }
    }
}
