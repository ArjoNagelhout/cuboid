//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid
{
    public static class Layers
    {
        /// <summary>
        /// Usage:
        /// 
        /// <para>
        /// A layer is a bitmask. Which means that every bit is one layer in the
        /// int.
        /// </para>
        /// <para>
        /// Layers are defined in the Unity Editor with a given name.
        ///
        /// <para>
        /// To combine multiple layers into one layer mask,
        /// use the bitwise or operator on the layer's value:
        /// </para>
        /// 
        /// <code>
        /// int combinedLayerMask = layer.Value | layer2.Value;
        /// </code>
        /// </para>
        /// </summary>
        public struct Layer
        {
            /// <summary>
            /// Initialize layer with a given name (as in the Unity Editor)
            /// </summary>
            /// <param name="name"></param>
            public Layer(string name)
            {
                this.Name = name;
                this.layer = LayerMask.NameToLayer(Name);
            }

            public string Name;
            public LayerMask layer;

            public static implicit operator int(Layer layer)
            {
                return layer.layer.value;
            }
        }

        public static readonly Layer Default = new Layer("Default");
        public static readonly Layer UI = new Layer("UI");
        public static readonly Layer SpatialUI = new Layer("SpatialUI");
        public static readonly Layer Controllers = new Layer("Controllers");
        public static readonly Layer Selected = new Layer("Selected");
    }
}
