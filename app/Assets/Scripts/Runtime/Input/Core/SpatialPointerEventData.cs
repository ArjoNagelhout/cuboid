//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cuboid.Input
{

    /// <summary>
    /// A custom UI event for devices that exist within 3D Unity space, separate from the camera's position.
    /// </summary>
    public class SpatialPointerEventData : PointerEventData
    {
        /// <summary>
        /// Initializes and returns an instance of <see cref="TrackedDeviceEventData"/> with event system.
        /// </summary>
        /// <param name="eventSystem"> The event system associated with the UI.</param>
        public SpatialPointerEventData(EventSystem eventSystem)
            : base(eventSystem)
        {
        }

        public enum ObjectType
        {
            UI,
            SpatialUI,
            OutsideUI
        }

        public ObjectType dragObjectType { get; set; }

        public ObjectType enterObjectType { get; set; }

        public Vector3 spatialPosition { get; set; }

        public Vector3 spatialTargetPosition { get; set; }
        
        public Vector3 spatialPressPosition { get; set; }

        /// <summary>
        /// To be used for determining drag
        /// </summary>
        public Vector3 spatialTargetPositionOnPress { get; set; }

        public SpatialPointerConfiguration configuration { get; set; }

        public SpatialPointerReticleData reticleData { get; set; }

        public bool outsideUIPointerPress { get; set; }

        public bool outsideUIPointerDrag { get; set; }

        public bool outsideUIDragging { get; set; }

        public bool outsideUICaptured { get; set; }

        public RaycastResult outsideUIPressRaycastResult { get; set; }

        public bool invalidRaycastWasIntercepted { get; set; }

        public Ray GetRay()
        {
            Vector3 origin = rayPoints[0];
            Vector3 direction = (rayPoints[1] - origin).normalized;
            Ray ray = new Ray(origin, direction);
            return ray;
        }

        /// <summary>
        /// whether or not there is a registered outside UI
        /// </summary>
        public bool outsideUIPointerEnter { get; set; }

        public bool outsideUIValidPointerPosition { get; set; }

        public float distance { get; set; }

        /// <summary>
        /// A series of interconnected points Unity uses to track hovered and selected UI.
        /// </summary>
        public List<Vector3> rayPoints { get; set; }

        /// <summary>
        /// Set by the ray caster, this is the index of the endpoint within the <see cref="rayPoints"/> list that received the hit.
        /// </summary>
        public int rayHitIndex { get; set; }

        /// <summary>
        /// The physics layer mask to use when checking for hits, both in occlusion and UI objects.
        /// </summary>
        public LayerMask layerMask { get; set; }

        /// <summary>
        /// (Read Only) The Interactor that triggered this event, or <see langword="null"/> if no interactor was responsible.
        /// </summary>
        public RayInteractor Interactor
        {
            get
            {
                SpatialInputModule realityInputModule = currentInputModule as SpatialInputModule;

                if (realityInputModule != null)
                {
                    return realityInputModule.GetInteractor(pointerId);
                }

                return null;
            }
        }
    }
}