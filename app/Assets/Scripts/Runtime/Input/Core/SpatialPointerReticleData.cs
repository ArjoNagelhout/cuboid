// Copyright (c) 2023 Arjo Nagelhout

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.Input
{
    public interface ISpatialPointerCustomReticle
    {
        public SpatialPointerReticleData ReticleData { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [System.Serializable]
    public class SpatialPointerReticleData
    {
        /// <summary>
        /// The image that should be used for the reticle
        /// </summary>
        public Sprite ReticleImage;

        public Color Color = Color.white;

        /// <summary>
        /// The color the reticle should change to on press
        /// </summary>
        public Color PressedColor = Color.white;

        /// <summary>
        /// How the position should be offset from the target spatial pointer position
        /// </summary>
        public Vector3 PositionOffset = Vector3.zero;

        /// <summary>
        /// 
        /// </summary>
        public Quaternion Rotation = Quaternion.identity;

        public delegate Quaternion UpdateRotation();

        public UpdateRotation updateRotation;

        /// <summary>
        /// 
        /// </summary>
        public float ReferenceSizeAtOneMeterDistance = 0.05f;

        /// <summary>
        /// Whether to always face the camera,
        /// if false, will use <see cref="Rotation"/>
        /// </summary>
        public bool Billboard = true;

        public static SpatialPointerReticleData Default
        {
            get
            {
                SpatialInputModule spatialInputModule = SpatialInputModule.Instance;
                return spatialInputModule != null ? spatialInputModule.DefaultPointerReticleData :
                    new SpatialPointerReticleData();
            }
        }
    }
}
