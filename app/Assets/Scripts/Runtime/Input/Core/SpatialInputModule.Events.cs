// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cuboid.Input
{
    public partial class SpatialInputModule
    {
        public delegate SpatialPointerReticleData GetReticleDataOutsideUIDelegate();
        public delegate SpatialPointerConfiguration GetConfigurationOutsideUIDelegate();

        /// <summary>
        /// Delegate that modifies the raycast result, to be used by the PopupsController to
        /// intercept UI interactions when a popup is shown. 
        /// </summary>
        public delegate bool InterceptPointerCurrentRaycast(ref RaycastResult raycastResult);

        public InterceptPointerCurrentRaycast interceptPointerCurrentRaycast;

        public GetReticleDataOutsideUIDelegate outsideUIGetReticleData;
        public GetConfigurationOutsideUIDelegate outsideUIGetConfiguration;

        // contains outside UI events, events that are fired when no SpatialUI or UI was interacted with

#pragma warning disable CS0067 // this, because we might need the events later, but right now they're not all used

        /// <summary>
        /// This occurs when a UI pointer enters an element.
        /// </summary>
        public event Action<SpatialPointerEventData> outsideUIPointerEnter;

        /// <summary>
        /// This occurs when a UI pointer exits an element.
        /// </summary>
        public event Action<SpatialPointerEventData> outsideUIPointerExit;

        /// <summary>
        /// This occurs when a select button down occurs while a UI pointer is hovering an element.
        /// This event is executed using ExecuteEvents.ExecuteHierarchy when sent to the target element.
        /// </summary>
        public event Action<SpatialPointerEventData> outsideUIPointerDown;

        /// <summary>
        /// This occurs when a select button up occurs while a UI pointer is hovering an element.
        /// </summary>
        public event Action<SpatialPointerEventData> outsideUIPointerUp;

        /// <summary>
        /// This occurs when a select button click occurs while a UI pointer is hovering an element.
        /// </summary>
        public event Action<SpatialPointerEventData> outsideUIPointerClick;

        /// <summary>
        /// This occurs when a drag first occurs on an element.
        /// </summary>
        public event Action<SpatialPointerEventData> outsideUIBeginDrag;

        /// <summary>
        /// This occurs every frame while dragging an element.
        /// </summary>
        public event Action<SpatialPointerEventData> outsideUIDrag;

        /// <summary>
        /// This occurs on the last frame an element is dragged.
        /// </summary>
        public event Action<SpatialPointerEventData> outsideUIEndDrag;

        /// <summary>
        /// This occurs when a dragged element is dropped on a drop handler.
        /// </summary>
        public event Action<SpatialPointerEventData> outsideUIDrop;

        /// <summary>
        /// This occurs when an element is scrolled
        /// </summary>
        public event Action<SpatialPointerEventData> outsideUIScroll;

        /// <summary>
        /// This occurs when the user moves the cursor around (e.g. there is a delta)
        /// </summary>
        public event Action<SpatialPointerEventData> outsideUIMove;


        // events that are fired when SpatialUI or UI components are being interacted with


        /// <summary>
        /// This occurs when a UI pointer enters an element.
        /// </summary>
        public event Action<SpatialPointerEventData> pointerEnter;

        /// <summary>
        /// This occurs when a UI pointer exits an element.
        /// </summary>
        public event Action<SpatialPointerEventData> pointerExit;

        /// <summary>
        /// This occurs when a select button down occurs while a UI pointer is hovering an element.
        /// This event is executed using ExecuteEvents.ExecuteHierarchy when sent to the target element.
        /// </summary>
        public event Action<SpatialPointerEventData> pointerDown;

        /// <summary>
        /// This occurs when a select button up occurs while a UI pointer is hovering an element.
        /// </summary>
        public event Action<SpatialPointerEventData> pointerUp;

        /// <summary>
        /// This occurs when a select button click occurs while a UI pointer is hovering an element.
        /// </summary>
        public event Action<SpatialPointerEventData> pointerClick;

        /// <summary>
        /// This occurs when a drag first occurs on an element.
        /// </summary>
        public event Action<SpatialPointerEventData> beginDrag;

        /// <summary>
        /// This occurs every frame while dragging an element.
        /// </summary>
        public event Action<SpatialPointerEventData> drag;

        /// <summary>
        /// This occurs on the last frame an element is dragged.
        /// </summary>
        public event Action<SpatialPointerEventData> endDrag;

        /// <summary>
        /// This occurs when a dragged element is dropped on a drop handler.
        /// </summary>
        public event Action<SpatialPointerEventData> drop;

        /// <summary>
        /// This occurs when an element is scrolled
        /// </summary>
        public event Action<SpatialPointerEventData> scroll;

        /// <summary>
        /// This occurs when the user moves the cursor around (e.g. there is a delta)
        /// </summary>
        public event Action<SpatialPointerEventData> move;

#pragma warning restore CS0067
    }
}
