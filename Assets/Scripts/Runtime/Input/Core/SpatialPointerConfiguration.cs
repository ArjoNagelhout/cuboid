using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.Input
{
    public interface ISpatialPointerConfiguration
    {
        public SpatialPointerConfiguration Configuration { get; set; }
    }

    /// <summary>
    /// The mode with which the RayInteractor should move objects and UI
    /// elements around
    /// </summary>
    public enum SpatialPointerMode
    {
        /// <summary>
        /// Default behaviour for the gizmos etc.
        /// Can be moved further and closer to the cursor
        /// </summary>
        WorldSpace = 0,

        /// <summary>
        /// Raycast with scene.
        /// </summary>
        Raycast
    }

    /// <summary>
    /// This class contains configuration information that can be used to change the
    /// smoothing and stabilization behaviour of the spatial pointer
    /// </summary>
    [System.Serializable]
    public class SpatialPointerConfiguration
    {
        /// <summary>
        /// 
        /// </summary>
        public float StabilizationRadius = 0.05f;

        /// <summary>
        /// 
        /// </summary>
        public float MinimumStabilizationDistance = 0.05f;

        /// <summary>
        /// 
        /// </summary>
        public bool StartDraggingFromPressedPosition = true;

        /// <summary>
        /// 
        /// </summary>
        public float DragThreshold = 0.05f;

        /// <summary>
        /// 
        /// </summary>
        public float MinimumDragThresholdDistance = 0.05f;

        /// <summary>
        /// 
        /// </summary>
        public float SmoothToLerpTime = 0.0001f;

        public delegate bool CustomGetSpatialPointerPosition(SpatialPointerEventData eventData, out Vector3 result, out float distance);

        public CustomGetSpatialPointerPosition customGetSpatialPointerInputPosition;

        public delegate bool CustomIsOverDragThreshold(SpatialPointerEventData eventData);

        public CustomIsOverDragThreshold customIsOverDragThreshold;

        /// <summary>
        /// 
        /// </summary>
        public static SpatialPointerConfiguration Default
        {
            get
            {
                SpatialInputModule spatialInputModule = SpatialInputModule.Instance;
                return spatialInputModule != null ?
                    spatialInputModule.DefaultPointerConfiguration : new SpatialPointerConfiguration();
            }
        }
    }
}

