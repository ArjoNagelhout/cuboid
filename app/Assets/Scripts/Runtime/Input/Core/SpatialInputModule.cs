//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Linq;
using System.Collections.Generic;
using Cuboid.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;
using static Cuboid.Input.SpatialPointerEventData;
using static Cuboid.Input.SpatialPointerReticleData;

namespace Cuboid.Input
{
    /// <summary>
    /// The SpatialInputModule is responsible for keeping track of all ray interactors and
    /// firing events for the SpatialUI and UI.
    /// 
    /// There are three layers:
    /// ObjectType.UI
    /// These are the traditional UI elements in the Unity UI package.
    /// These implement only PointerEventData. Also ScrollView and Button implement these. 
    ///
    /// ObjectType.SpatialUI
    /// These are the 3D UI elements added in this codebase, that can be moved around freely in 3D,
    /// either in world space, along an axis or along a plane. 
    ///
    /// ObjectType.OutsideUI
    /// These are global events that are fired (located in SpatialInputModule.Events when dragging, clicking etc.
    /// outside any SpatialUI or UI objects. These can be subscribed to by tools such as the SelectTool or DrawCubeTool. 
    /// </summary>
    public partial class SpatialInputModule : BaseInputModule
    {
        private static SpatialInputModule _instance;
        public static SpatialInputModule Instance => _instance;

        private const float k_ClickSpeed = 0.3f;
        private const float k_MinimumDistanceFromController = 0.01f;

        [SerializeField] private float _pointerDistanceDeltaOnScroll = 1f;
        [SerializeField] private float _uiDragPixelThreshold = 20f;

        [SerializeField] private float _invalidRaycastDistance = 1f;

        /// <summary>
        /// List of registered ray interactors
        /// </summary>
        private readonly List<RayInteractor> _registeredRayInteractors = new List<RayInteractor>();

        private readonly Dictionary<int, SpatialPointerEventData> _trackedDeviceEventByPointerId = new Dictionary<int, SpatialPointerEventData>();

        // Default configuration and reticle
        [Header("Configuration")]
        public SpatialPointerConfiguration DefaultPointerConfiguration = new SpatialPointerConfiguration();

        [Header("Reticle")]
        public SpatialPointerReticleData DefaultPointerReticleData = new SpatialPointerReticleData();

        private Camera _uiCamera;
        /// <summary>
        /// The <see cref="Camera"/> that Unity uses to perform ray casts when determining the screen space location of a tracked device cursor.
        /// </summary>
        public Camera UICamera
        {
            get
            {
                if (_uiCamera == null || !_uiCamera.isActiveAndEnabled)
                {
                    _uiCamera = Camera.main;
                }
                return _uiCamera;
            }
            set => _uiCamera = value;
        }

        protected override void Awake()
        {
            base.Awake();
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }

            DistanceOutsideUI = _defaultDistanceOutsideUI;

            CurrentSpatialPointerMode = new("CurrentSpatialPointerMode", SpatialPointerMode.WorldSpace);
        }

        protected virtual void Update()
        {
            if (eventSystem.IsActive() && eventSystem.currentInputModule == this && eventSystem == EventSystem.current)
            {
                DoProcess();
            }
        }

        protected void DoProcess()
        {
            SendUpdateEventToSelectedObject();

            for (int i = 0; i < _registeredRayInteractors.Count; i++)
            {
                RayInteractor rayInteractor = _registeredRayInteractors[i];
                rayInteractor.UpdateModel();
                ProcessTrackedDevice(rayInteractor);
            }
        }

        /// <summary>
        /// Sends an update event to the currently selected object.
        /// </summary>
        /// <returns>Returns whether the update event was used by the selected object.</returns>
        protected bool SendUpdateEventToSelectedObject()
        {
            var selectedGameObject = eventSystem.currentSelectedGameObject;
            if (selectedGameObject == null)
                return false;

            

            var data = GetBaseEventData();
            //Debug.Log(data);
            //updateSelected?.Invoke(selectedGameObject, data);
            ExecuteEvents.Execute(selectedGameObject, data, ExecuteEvents.updateSelectedHandler);
            return data.used;
        }

        private void ProcessTrackedDevice(RayInteractor interactor)
        {
            if (!interactor.ChangedThisFrame) { return; }

            SpatialPointerEventData eventData = GetOrCreateCachedTrackedDeviceEvent(interactor.PointerId);
            eventData.Reset();
            interactor.CopyTo(eventData);

            eventData.button = PointerEventData.InputButton.Left;
            eventData.position = new Vector2(float.MinValue, float.MinValue);
            RaycastResult raycastResult = PerformRaycast(eventData);

            // Intercept raycast (e.g. for popups)
            bool wasInvalid = !raycastResult.isValid;
            bool wasIntercepted = false;
            if (interceptPointerCurrentRaycast != null)
            {
                wasIntercepted = interceptPointerCurrentRaycast.Invoke(ref raycastResult);
            }
            eventData.invalidRaycastWasIntercepted = wasIntercepted && wasInvalid;

            eventData.pointerCurrentRaycast = raycastResult;
            
            if (TryGetCamera(eventData, out Camera screenPointCamera))
            {
                GetPointerWorldPosition(eventData, out Vector3 worldPosition, out Vector3 worldTargetPosition);
                Vector2 screenPosition = screenPointCamera.WorldToScreenPoint(worldPosition);
                
                Vector2 thisFrameDelta = screenPosition - eventData.position;
                eventData.position = screenPosition;
                eventData.spatialPosition = worldPosition;
                eventData.spatialTargetPosition = worldTargetPosition;
                eventData.delta = thisFrameDelta;

                ProcessJoystickPressed(interactor.JoystickButtonDeltaState, eventData);

                if (!eventData.outsideUIPointerPress && !eventData.outsideUICaptured)
                {
                    ProcessPointerMovement(eventData);
                }
                ProcessPointerMovementOutsideUI(eventData);

                if (eventData.outsideUIPointerEnter || eventData.outsideUICaptured)
                {
                    ProcessPointerButtonOutsideUI(interactor.SelectDelta, eventData);
                    ProcessPointerButtonDragOutsideUI(eventData);
                }
                else
                {
                    ProcessPointerButton(interactor.SelectDelta, eventData);
                    ProcessPointerButtonDrag(eventData);
                    ProcessPointerScroll(eventData);
                }
                
                interactor.CopyFrom(eventData);
            }

            interactor.OnFrameFinished();
        }

        #region Spatial Pointer Mode

        private void ProcessJoystickPressed(ButtonDeltaState joystickPressedDeltaState, SpatialPointerEventData eventData)
        {
            if (joystickPressedDeltaState == ButtonDeltaState.Pressed)
            {
                CurrentSpatialPointerMode.Value = SwitchSpatialPointerMode(eventData, CurrentSpatialPointerMode.Value);
            }
        }

        private SpatialPointerMode SwitchSpatialPointerMode(SpatialPointerEventData eventData, SpatialPointerMode mode)
        {
            return mode == SpatialPointerMode.WorldSpace ? SpatialPointerMode.Raycast : SpatialPointerMode.WorldSpace;
        }

        // the mode is global, and could even be made dependant on the current editing mode (1:1 AR, scale model VR)
        public StoredBinding<SpatialPointerMode> CurrentSpatialPointerMode;

        [SerializeField] private float _defaultDistanceOutsideUI = 1.0f;

        [NonSerialized] public float DistanceOutsideUI = 0.0f;

        #endregion

        #region Get SpatialPointer World Position

        /// <summary>
        /// We have the GetPointerWorldPosition method, this is the method that gets called at the start of each frame.
        /// This is because we want to change the eventData.spatialTargetPosition and eventData.spatialPosition.
        ///
        /// There are two post processing steps done to enable smooth movement from a distance:
        /// 1. Stabilization
        /// 2. Smooth towards (linear interpolation)
        ///
        /// There are many "end-users" of this method, such as the DrawShapeTool, SelectTool, RealityAssetUIElement, SpatialUI Gizmos
        ///
        /// The challenge is in the fact that each of these have slightly unique needs for calculating the position, but we still want
        /// unified behaviour across these tools.
        /// 
        /// The approach that was currently taken was to just make it work, but every time a slight requirement change appeared, the
        /// SpatialInputModule had to be rewritten to accommodate this new functionality.
        /// 
        /// Such as for the DrawShapeTool:
        /// - Before captured / dragging: raycast with the scene, and if no raycast was found, try to raycast with the horizontal world plane
        /// - While captured / dragging: raycast with a defined plane from the object that was under the pointer at pointer down.
        /// 
        /// SpatialUI Gizmos:
        /// - Before dragging: nothing should be done.
        /// - While dragging: move in world space, edit distance by moving joystick up / down
        /// 
        /// RealityAssetUIElement:
        /// - Before dragging: nothing should be done.
        /// - While dragging: move in world space, edit distance by moving joystick up / down, if raycast = true: raycast with the scene
        /// 
        /// SelectTool:
        /// - Before dragging: raycast with the scene
        /// - While dragging: move in world space, edit distance by moving joystick up / down, if raycast = true: raycast with the scene
        /// 
        /// And some imagined tools:
        /// 
        /// TextTool:
        /// - Before dragging: raycast with the scene, if no raycast was found, move in world space
        /// - While dragging: raycast with a defined plane that was facing the camera at pointer down. 
        /// 
        /// What we can see is that there is overlap in behaviour, but there are small deviations between the tools.
        /// This all stems from the fact that we want to support UI, SpatialUI *and* outsideUI events from the SpatialInputModule
        /// so that there is one unified place for sending input events.
        /// 
        /// We want a system that is able to calculate the eventData.spatialPointerPosition,
        /// depending on the currently active tool, whether it is UI, SpatialUI or outsideUI.
        /// 
        /// There are two modes that can be toggled between by pressing the Joystick button:
        /// - WorldSpace
        /// - RaycastWithScene 
        /// </summary>
        private void GetPointerWorldPosition(SpatialPointerEventData eventData, out Vector3 current, out Vector3 target)
        {
            bool draggingUI = false;
            bool draggingSpatialUI = false;
            bool outsideUI = false;

            if (eventData.outsideUIPointerEnter || eventData.outsideUICaptured)
            {
                // this means it's outside UI, so we need to employ getting the 3D
                // spatial cursor position
                outsideUI = true;
            }
            else if (eventData.pointerDrag != null)
            {
                // otherwise if dragging
                draggingUI = eventData.dragObjectType == ObjectType.UI;
                draggingSpatialUI = eventData.dragObjectType == ObjectType.SpatialUI;
            }

            if (draggingUI)
            {
                GetUIPointerPosition(eventData, out current);
                target = current;
                return;
            }

            // steps to determine the position:

            // post-processing steps:
            // these are to be implemented by default
            // Stabilize
            // SmoothTo

            // these are to be implemented by the tools themselves
            // SnapToGrid
            // SnapToObject
            // SnapToGuidelines

            current = eventData.spatialPosition;
            target = eventData.spatialTargetPosition;

            if (draggingSpatialUI)
            {
                UpdateDistance(eventData);
                GetSpatialPointerPositionWorldSpace(eventData, out current, out target);
                return;
            }

            if (outsideUI)
            {
                bool valid = GetSpatialPointerPositionOutsideUI(eventData, out current, out target);
                eventData.outsideUIValidPointerPosition = valid;
                if (valid) { return; }
            }

            // Otherwise get current pointer raycast
            if (eventData.pointerCurrentRaycast.isValid && !eventData.invalidRaycastWasIntercepted)
            {
                current = eventData.pointerCurrentRaycast.worldPosition;
                target = current;
                return;
            }

            // Otherwise return invalid raycast distance from the controller
            GetSpatialPointerInputPositionWorldSpace(eventData, out current, _invalidRaycastDistance);
            target = current;
        }

        private static void GetUIPointerPosition(SpatialPointerEventData eventData, out Vector3 current)
        {
            current = eventData.spatialPosition;

            Vector3 origin = eventData.rayPoints[0];
            Vector3 direction = (eventData.rayPoints[1] - origin).normalized;
            Ray ray = new Ray(origin, direction);

            Transform transform = eventData.pointerDrag.transform;
            Vector3 normal = transform.forward;
            Vector3 position = transform.position;

            Plane plane = new Plane(normal, position);

            if (plane.Raycast(ray, out float enter))
            {
                current = ray.GetPoint(enter);
            }
            else
            {
                // construct a raycast directly from the origin towards the plane
                Ray newRay = new Ray(origin, -normal);
                if (plane.Raycast(newRay, out float newEnter))
                {
                    current = ray.GetPoint(newEnter);
                }
            }
        }

        public static void GetSpatialPointerInputPositionWorldSpace(SpatialPointerEventData eventData, out Vector3 inputPosition, float distance)
        {
            // get the ray from the controller
            Vector3 origin = eventData.rayPoints[0];
            Vector3 direction = (eventData.rayPoints[1] - origin).normalized;
            Ray ray = new Ray(origin, direction);

            // should be set depending on the move along type
            inputPosition = ray.GetPoint(distance);
        }

        private bool GetSpatialPointerPositionOutsideUI(SpatialPointerEventData eventData, out Vector3 current, out Vector3 target)
        {
            bool valid = false;

            Vector3 inputPosition = Vector3.zero;
            float distance = 0f;

            current = eventData.spatialPosition;
            target = eventData.spatialTargetPosition;

            if (eventData.configuration.customGetSpatialPointerInputPosition != null)
            {
                valid = eventData.configuration.customGetSpatialPointerInputPosition.Invoke(eventData, out inputPosition, out distance);
            }
            else
            {
                // Use a sensible default for when no custom get spatial pointer position is supplied

                // use the select tool as the default: raycast with scene and when dragging etc. do it in world space

                if ((eventData.outsideUIDragging && eventData.outsideUIPointerDrag) // only do world space movement if it is actually dragging something outsideUI
                    || (eventData.outsideUIPointerPress && eventData.outsideUIPressRaycastResult.isValid)) // or if an object has been pressed
                {
                    // move in world space
                    distance = eventData.distance;
                    UpdateDistance(eventData);
                    GetSpatialPointerInputPositionWorldSpace(eventData, out inputPosition, distance);
                    valid = true;
                }
                else
                {
                    valid = eventData.pointerCurrentRaycast.isValid;
                    if (valid)
                    {
                        inputPosition = eventData.pointerCurrentRaycast.worldPosition;
                        distance = eventData.pointerCurrentRaycast.distance;
                        eventData.distance = distance;
                    }
                }
            }

            // early return
            if (!valid) { return valid; }

            target = inputPosition;

            //target = inputPosition;
            Vector3 newPosition = StabilizePointerPosition(eventData, inputPosition, distance);

            if (eventData.outsideUIDragging || (eventData.outsideUICaptured && !eventData.outsideUIPointerPress))
            {
                current = Smoothing.SmoothTo(current, newPosition, eventData.configuration.SmoothToLerpTime, Time.deltaTime);
            }
            else if (!eventData.outsideUIPointerPress)
            {
                current = target;
            }

            return valid;
        }

        public void UpdateDistanceOutsideUI(SpatialPointerEventData eventData) => DistanceOutsideUI = UpdateDistance(eventData, DistanceOutsideUI);
        public void UpdateDistance(SpatialPointerEventData eventData) => eventData.distance = UpdateDistance(eventData, eventData.distance);

        /// <summary>
        /// Updates the distance based on the y scroll value of the associated ray interactor.
        /// </summary>
        private float UpdateDistance(SpatialPointerEventData eventData, float distance)
        {
            float scrollDelta = eventData.scrollDelta.y;
            if (Mathf.Approximately(scrollDelta, 0)) { return distance; }

            distance += scrollDelta * _pointerDistanceDeltaOnScroll * Time.deltaTime;
            distance = Mathf.Max(k_MinimumDistanceFromController, distance); // clamp lower bound
            return distance;
        }

        private static bool GetSpatialPointerPositionWorldSpace(SpatialPointerEventData eventData, out Vector3 current, out Vector3 target)
        {
            float distance = eventData.distance;
            GetSpatialPointerInputPositionWorldSpace(eventData, out Vector3 inputPosition, distance);
            Vector3 newPosition = StabilizePointerPosition(eventData, inputPosition, distance);

            current = eventData.spatialPosition;
            if (eventData.dragging)
            {
                current = Smoothing.SmoothTo(eventData.spatialPosition, newPosition, eventData.configuration.SmoothToLerpTime, Time.deltaTime);
            }

            target = inputPosition;

            return true;
        }

        private static float GetStabilizationRadius(SpatialPointerEventData eventData, float distance)
        {
            float stabilizationRadius = eventData.configuration.StabilizationRadius;
            float referenceDistance = eventData.configuration.MinimumStabilizationDistance;

            float distanceCorrectedRadius = stabilizationRadius;
            if (distance > referenceDistance)
            {
                float distanceFromReferenceDistance = distance - referenceDistance;
                distanceCorrectedRadius = stabilizationRadius + distanceFromReferenceDistance * stabilizationRadius;
            }
            return distanceCorrectedRadius;
        }

        private static Vector3 StabilizePointerPosition(SpatialPointerEventData eventData, Vector3 inputPosition, float distance)
        {
            float distanceCorrectedRadius = GetStabilizationRadius(eventData, distance);
            Vector3 newPosition = eventData.spatialPosition;

            // stabilize stroke (such as in https://docs.blender.org/manual/en/latest/sculpt_paint/brush/stroke.html)
            Vector3 deltaPosition = inputPosition - eventData.spatialPosition;
            if (deltaPosition.sqrMagnitude >= distanceCorrectedRadius * distanceCorrectedRadius)
            {
                Vector3 lastCursorPosition = deltaPosition.normalized * distanceCorrectedRadius;
                Vector3 realDeltaPosition = deltaPosition - lastCursorPosition;
                newPosition = eventData.spatialPosition + realDeltaPosition;
            }

            return newPosition;
        }

        //TODO: Refactor to remove duplicate code with StabilizePointerPosition
        private static Vector3 StabilizePointerPositionRaycast(SpatialPointerEventData eventData, Vector3 inputPosition, float distance)
        {
            float distanceCorrectedRadius = GetStabilizationRadius(eventData, distance);
            Vector3 newPosition = eventData.spatialPosition;

            // first calculate the plane normal
            int index = eventData.rayHitIndex;
            Vector3 rayOrigin = eventData.rayPoints[index];
            Vector3 hitPosition = inputPosition;
            Vector3 planeNormal = hitPosition - rayOrigin;

            // construct a plane at the spatial position
            Plane plane = new Plane(planeNormal, eventData.spatialPosition);

            // project the input position onto the plane
            Vector3 projectedInputPosition = plane.ClosestPointOnPlane(inputPosition);

            // then, get the delta position
            Vector3 projectedDeltaPosition = projectedInputPosition - eventData.spatialPosition;
            Vector3 deltaPosition = inputPosition - eventData.spatialPosition;

            // do the same as StabilizePointerPosition
            if (projectedDeltaPosition.sqrMagnitude >= distanceCorrectedRadius * distanceCorrectedRadius)
            {
                // The deltaPosition should be reprojected back to the 
                Vector3 lastCursorPosition = deltaPosition.normalized * distanceCorrectedRadius;
                Vector3 realDeltaPosition = deltaPosition - lastCursorPosition;
                newPosition = eventData.spatialPosition + realDeltaPosition;
            }

            return newPosition;
        }

        #endregion

        #region Process Pointer Button

        private void OnPress(SpatialPointerEventData eventData)
        {
            eventData.eligibleForClick = true;
            eventData.delta = Vector2.zero;
            eventData.dragging = false;
            eventData.outsideUIDragging = false;
            eventData.pressPosition = eventData.position;
            eventData.pointerPressRaycast = eventData.pointerCurrentRaycast;
            eventData.spatialPressPosition = eventData.spatialPosition;
            eventData.spatialTargetPositionOnPress = eventData.spatialTargetPosition;
            eventData.distance = eventData.pointerPressRaycast.distance; // copy the data from the raycast so that we can change it later. 
        }

        private void ProcessPointerButton(ButtonDeltaState buttonChanges, SpatialPointerEventData eventData)
        {
            if (buttonChanges == ButtonDeltaState.NoChange) { return; }

            GameObject hoverTarget = eventData.pointerCurrentRaycast.gameObject;

            // Handle button press
            if (buttonChanges == ButtonDeltaState.Pressed)
            {
                OnPress(eventData);
                
                GameObject newPressed = ExecutePointerDown(hoverTarget, eventData);
                if (newPressed == null) { newPressed = GetPointerClickHandler(hoverTarget); } // otherwise search for click handler. 

                float time = Time.unscaledTime;
                if (newPressed == eventData.lastPress && ((time - eventData.clickTime) < k_ClickSpeed)) { ++eventData.clickCount; } else { eventData.clickCount = 1; }
                eventData.clickTime = time;
                eventData.pointerPress = newPressed;
                eventData.rawPointerPress = hoverTarget;

                // Save the drag handler for drag events during this mouse down.
                GameObject dragObject = ExecuteInitializePotentialDrag(hoverTarget, eventData, out ObjectType objectType);
                eventData.dragObjectType = objectType;
                eventData.pointerDrag = dragObject;

                //Debug.Log($"Got configuration from object {dragObject}");
                eventData.configuration = GetSpatialPointerConfiguration(eventData, dragObject);
            }

            // Handle button release
            if (buttonChanges == ButtonDeltaState.Released)
            {
                GameObject target = eventData.pointerPress;
                ExecutePointerUp(target, eventData);
                ExecutePointerExit(target, eventData);

                GameObject pointerUpHandler = GetPointerClickHandler(hoverTarget);
                GameObject pointerDrag = eventData.pointerDrag;
                if (target == pointerUpHandler && eventData.eligibleForClick) // if the press object is the same as the up object
                {
                    ExecutePointerClick(target, eventData);
                }
                else if (eventData.dragging && pointerDrag != null)
                {
                    ExecuteDrop(hoverTarget, eventData);
                }

                eventData.eligibleForClick = false;
                eventData.pointerPress = null;
                eventData.rawPointerPress = null;
                eventData.pointerEnter = null;

                eventData.outsideUIPointerEnter = false;

                if (eventData.dragging)
                {
                    if (pointerDrag != null)
                    {
                        ExecuteEndDrag(pointerDrag, eventData);
                    }
                }

                eventData.dragging = false;
                eventData.pointerDrag = null;
            }
        }

        // refactord the behaviour into their own separate methods to make it more readible.
        // otherwise it will become a jumbled mess of if else statements that have way too many
        // tiny bugs to reason about. 
        private void ProcessPointerButtonOutsideUI(ButtonDeltaState buttonChanges, SpatialPointerEventData eventData)
        {
            if (buttonChanges == ButtonDeltaState.NoChange) { return; }

            if (buttonChanges == ButtonDeltaState.Pressed)
            {
                OnPress(eventData);
                eventData.outsideUIPressRaycastResult = eventData.pointerCurrentRaycast;

                if (outsideUIPointerDown != null)
                {
                    outsideUIPointerDown.Invoke(eventData);
                    eventData.outsideUIPointerPress = true;
                }
                else
                {
                    if (outsideUIPointerClick != null)
                    {
                        eventData.outsideUIPointerPress = true;
                    }
                }

                if (outsideUIDrag != null || outsideUIBeginDrag != null || outsideUIEndDrag != null)
                {
                    eventData.outsideUIPointerDrag = true;
                }

                eventData.configuration = GetSpatialPointerConfiguration(eventData, null);
            }
            
            if (buttonChanges == ButtonDeltaState.Released)
            {
                // Handle outside UI pointer up 
                outsideUIPointerUp?.Invoke(eventData);
                outsideUIPointerExit?.Invoke(eventData);

                // check if eligible for click
                if (eventData.eligibleForClick)
                {
                    outsideUIPointerClick?.Invoke(eventData);
                }

                eventData.outsideUIPointerPress = false;

                if (eventData.outsideUIDragging && eventData.outsideUIPointerDrag)
                {
                    outsideUIEndDrag?.Invoke(eventData);
                }

                eventData.outsideUIPointerDrag = false;
                eventData.outsideUIDragging = false;
                eventData.outsideUIPressRaycastResult.Clear();
                eventData.eligibleForClick = false;
            }
        }

        #endregion

        #region Process Pointer Movement

        private void ProcessPointerMovement(SpatialPointerEventData eventData)
        {
            GameObject currentPointerTarget = eventData.pointerCurrentRaycast.gameObject;

            // If the pointer moved, send move events to all UI elements the pointer is
            // currently over.
            bool wasMoved = eventData.IsPointerMoving();
            if (wasMoved)
            {
                for (var i = 0; i < eventData.hovered.Count; ++i)
                {
                    ExecutePointerMove(eventData.hovered[i], eventData);
                }
            }

            // We don't want to enter / exit new objects while dragging.
            // this might be changed in the future for drag and drop hover support?
            if (eventData.pointerPress != null || eventData.dragging) { return; }

            // If we have no target or pointerEnter has been deleted,
            // we just send exit events to anything we are tracking
            // and then exit.
            if (currentPointerTarget == null || eventData.pointerEnter == null)
            {
                foreach (GameObject hovered in eventData.hovered)
                {
                    ExecutePointerExit(hovered, eventData);
                }

                eventData.hovered.Clear();

                if (currentPointerTarget == null)
                {
                    eventData.pointerEnter = null;
                    eventData.enterObjectType = ObjectType.OutsideUI;
                    return;
                }
            }

            if (eventData.pointerEnter == currentPointerTarget)
            {
                return;
            }

            GameObject commonRoot = FindCommonRoot(eventData.pointerEnter, currentPointerTarget);

            // 1. Send exit events to all objects in the hierarchy from the old pointerEnter
            // to the common root (excluding the common root)
            if (eventData.pointerEnter != null)
            {
                Transform target = eventData.pointerEnter.transform;

                // Walk up the hierarchy tree (each parent), until it has reached the common root
                while (target != null)
                {
                    // If it has reached the common root, stop traversing the hierarchy
                    if (commonRoot != null && commonRoot.transform == target)
                    {
                        break;
                    }

                    GameObject targetGameObject = target.gameObject;

                    // exit this object
                    ExecutePointerExit(targetGameObject, eventData);

                    eventData.hovered.Remove(targetGameObject);
                    target = target.parent;
                }
            }

            // Set pointerEnter to the newly raycast object
            eventData.pointerEnter = currentPointerTarget;

            // 2. Send enter events to all objects in the hierarchy from the newly raycast object
            // to the common root (excluding the common root)

            bool setEnterObjectType = false;
            bool setReticleData = false;

            //eventData.enterObjectType = ObjectType.OutsideUI;
            
            if (currentPointerTarget != null)
            {
                Transform target = currentPointerTarget.transform;

                // Walk up hierarchy, until reaching the common root
                while (target != null)
                {
                    GameObject targetGameObject = target.gameObject;
                    // If it has reached the common root, stop traversing the hierarchy
                    if (targetGameObject == commonRoot)
                    {
                        break;
                    }

                    ExecutePointerEnter(targetGameObject, eventData, out ObjectType objectType);
                    if (!setEnterObjectType)
                    {
                        eventData.enterObjectType = objectType;
                        setEnterObjectType = true;
                    }
                    if (!setReticleData)
                    {
                        if (objectType != ObjectType.OutsideUI)
                        {
                            if (GetReticleData(eventData, targetGameObject, out SpatialPointerReticleData reticleData))
                            {
                                setReticleData = true;
                            }
                            eventData.reticleData = reticleData;
                        }
                    }

                    if (wasMoved)
                    {
                        ExecutePointerMove(targetGameObject, eventData);
                    }
                    eventData.hovered.Add(targetGameObject);
                    target = target.parent;
                }
            }
        }

        public bool GetConfigurationOutsideUINextUpdate = false;

        private void ProcessPointerMovementOutsideUI(SpatialPointerEventData eventData)
        {
            GameObject currentPointerTarget = eventData.pointerCurrentRaycast.gameObject;

            if (eventData.outsideUIPointerEnter && eventData.outsideUIValidPointerPosition)
            {
                outsideUIMove?.Invoke(eventData);
            }

            if (eventData.enterObjectType == ObjectType.OutsideUI)
            {
                bool getConfiguration = !eventData.outsideUIPointerEnter || GetConfigurationOutsideUINextUpdate;
                if (getConfiguration)
                {
                    if (!eventData.outsideUIPointerEnter)
                    {
                        eventData.outsideUIPointerEnter = true;
                        outsideUIPointerEnter?.Invoke(eventData);
                    }
                    
                    eventData.configuration = GetSpatialPointerConfiguration(eventData, null);
                    eventData.reticleData = GetReticleDataOutsideUI(eventData);
                }
            }
            else
            {
                if (eventData.outsideUIPointerEnter)
                {
                    eventData.outsideUIPointerEnter = false;
                    outsideUIPointerExit?.Invoke(eventData);
                }
            }
        }

        #endregion

        #region Process Pointer Scroll

        private void ProcessPointerScroll(SpatialPointerEventData eventData)
        {
            if (eventData.dragging)
            {
                return;
            }

            Vector2 scrollDelta = eventData.scrollDelta;
            if (!Mathf.Approximately(scrollDelta.sqrMagnitude, 0f))
            {
                ExecuteScroll(eventData);
            }
        }

        #endregion

        #region Process Drag

        private void ProcessPointerButtonDrag(SpatialPointerEventData eventData)
        {
            if (!eventData.IsPointerMoving() || UnityEngine.Cursor.lockState == CursorLockMode.Locked) { return; }

            if (eventData.pointerDrag != null)
            {
                if (!eventData.dragging)
                {
                    bool startDragging = eventData.dragObjectType switch
                    {
                        ObjectType.SpatialUI => IsSpatialPointerOverDragThreshold(eventData),
                        _ => (eventData.pressPosition - eventData.position).sqrMagnitude >= (_uiDragPixelThreshold * _uiDragPixelThreshold)
                    };

                    if (startDragging)
                    {
                        GameObject target = eventData.pointerDrag;

                        if (eventData.configuration.StartDraggingFromPressedPosition)
                        {
                            // Resets the spatial pointer position back to where it was pressed so that the object "snaps"
                            // to under the exact pointer position where the object was pressed
                            eventData.spatialPosition = eventData.spatialPressPosition;
                        }

                        ExecuteBeginDrag(target, eventData);
                        eventData.dragging = true;
                        eventData.eligibleForClick = false;
                    }
                }

                if (eventData.dragging)
                {
                    // If we moved from our initial press object, process an up for that object.
                    GameObject target = eventData.pointerPress;
                    if (target != eventData.pointerDrag)
                    {
                        ExecutePointerUp(target, eventData);

                        eventData.eligibleForClick = false;
                        eventData.pointerPress = null;
                        eventData.rawPointerPress = null;
                    }

                    ExecuteDrag(eventData.pointerDrag, eventData);
                }
            }
        }

        private void ProcessPointerButtonDragOutsideUI(SpatialPointerEventData eventData)
        {
            if (eventData.outsideUIPointerDrag)
            {
                if (!eventData.outsideUIDragging)
                {
                    bool startDragging = false;
                    // default is to use world space, but if we have a custom drag threshold, we'll use that instead
                    if (eventData.configuration.customIsOverDragThreshold != null)
                    {
                        startDragging = eventData.configuration.customIsOverDragThreshold.Invoke(eventData);
                    }
                    else
                    {
                        // only start dragging when over object (default)
                        if (eventData.outsideUIPressRaycastResult.isValid)
                        {
                            startDragging = IsSpatialPointerOverDragThreshold(eventData);
                        }
                    }
                    
                    if (startDragging)
                    {
                        GameObject target = eventData.pointerDrag;

                        // Resets the spatial pointer position back to where it was pressed so that the object "snaps"
                        // to under the exact pointer position where the object was pressed
                        eventData.spatialPosition = eventData.spatialPressPosition;

                        outsideUIBeginDrag?.Invoke(eventData);

                        eventData.outsideUIDragging = true;
                        eventData.eligibleForClick = false;
                    }
                }

                if (eventData.outsideUIDragging)
                {
                    outsideUIDrag?.Invoke(eventData);
                }
            }
        }

        private static float GetDragThreshold(SpatialPointerEventData eventData, float distance)
        {
            float dragThreshold = eventData.configuration.DragThreshold;
            float referenceDistance = eventData.configuration.MinimumDragThresholdDistance;

            float correctedThreshold = dragThreshold;
            if (distance > referenceDistance)
            {
                float distanceFromReferenceDistance = distance - referenceDistance;
                correctedThreshold = dragThreshold + distanceFromReferenceDistance * dragThreshold;
            }
            //Debug.Log($"distance: {distance}, correctedThreshold: {correctedThreshold}");
            return correctedThreshold;
        }

        public static bool IsSpatialPointerOverDragThreshold(SpatialPointerEventData eventData)
        {
            float distance = eventData.pointerPressRaycast.isValid ?
                eventData.pointerPressRaycast.distance : GetDistanceToTargetPosition(eventData);

            float correctedThreshold = GetDragThreshold(eventData, distance);
            return (eventData.spatialTargetPositionOnPress - eventData.spatialTargetPosition).sqrMagnitude >= (correctedThreshold * correctedThreshold);
        }

        private static float GetDistanceToTargetPosition(SpatialPointerEventData eventData)
        {
            Vector3 origin = eventData.rayPoints[0];
            Vector3 targetPosition = eventData.spatialTargetPosition;

            return Vector3.Distance(origin, targetPosition);
        }

        public static bool IsSpatialPointerOverDragThresholdRaycast(SpatialPointerEventData eventData)
        {
            // first calculate the plane normal
            int index = eventData.rayHitIndex;
            Vector3 rayOrigin = eventData.rayPoints[index];
            Vector3 targetPosition = eventData.spatialTargetPosition;
            Vector3 planeNormal = targetPosition - rayOrigin;

            float distance = GetDistanceToTargetPosition(eventData);

            float correctedThreshold = GetDragThreshold(eventData, distance);

            // construct a plane at the spatial position
            Plane plane = new Plane(planeNormal, eventData.spatialTargetPositionOnPress);

            // project the input position onto the plane
            Vector3 projectedTargetPosition = plane.ClosestPointOnPlane(targetPosition);

            return (eventData.spatialTargetPositionOnPress - projectedTargetPosition).sqrMagnitude >= (correctedThreshold * correctedThreshold);
        }

        #endregion

        #region Execute Events

        private ObjectType GetObjectTypeBasedOnLayer(GameObject go)
        {
            if (go == null)
            {
                return ObjectType.OutsideUI;
            }
            if (go.layer == Layers.SpatialUI.layer)
            {
                return ObjectType.SpatialUI;
            }
            else if (go.layer == Layers.UI.layer)
            {
                return ObjectType.UI;
            }
            else
            {
                return ObjectType.OutsideUI;
            }
        }

        private GameObject GetPointerClickHandler(GameObject go)
        {
            GameObject pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(go);
            if (pointerUpHandler != null) { return pointerUpHandler; }
            return ExecuteEvents.GetEventHandler<ISpatialPointerClickHandler>(go);
        }

        private void ExecutePointerEnter(GameObject go, SpatialPointerEventData eventData, out ObjectType objectType)
        {
            ExecuteEvents.ExecuteMultiple(go, eventData, ExecuteEvents.pointerEnterHandler, SpatialPointerEvents.spatialPointerEnterHandler, out int calledIndex);
            if (calledIndex == 0)
            {
                objectType = ObjectType.UI;
                return;
            }
            if (calledIndex == 1)
            {
                objectType = ObjectType.SpatialUI;
                return;
            }

            objectType = GetObjectTypeBasedOnLayer(go);
        }

        private void ExecutePointerExit(GameObject go, SpatialPointerEventData eventData)
        {
            ExecuteEvents.ExecuteMultiple(go, eventData, ExecuteEvents.pointerExitHandler, SpatialPointerEvents.spatialPointerExitHandler, out int _);
            pointerExit?.Invoke(eventData);
        }

        private void ExecutePointerMove(GameObject go, SpatialPointerEventData eventData)
        {
            ExecuteEvents.ExecuteMultiple(go, eventData, ExecuteEvents.pointerMoveHandler, SpatialPointerEvents.spatialPointerMoveHandler, out int _);
            move?.Invoke(eventData);
        }

        private GameObject ExecutePointerDown(GameObject go, SpatialPointerEventData eventData)
        {
            GameObject pointerDownGameObject = ExecuteEvents.ExecuteHierarchyMultiple(go, eventData, ExecuteEvents.pointerDownHandler, SpatialPointerEvents.spatialPointerDownHandler, out int calledIndex);
            pointerDown?.Invoke(eventData);
            return pointerDownGameObject;
        }

        private void ExecutePointerUp(GameObject go, SpatialPointerEventData eventData)
        {
            ExecuteEvents.ExecuteMultiple(go, eventData, ExecuteEvents.pointerUpHandler, SpatialPointerEvents.spatialPointerUpHandler, out int _);
            pointerUp?.Invoke(eventData);
        }

        private void ExecutePointerClick(GameObject go, SpatialPointerEventData eventData)
        {
            ExecuteEvents.ExecuteMultiple(go, eventData, ExecuteEvents.pointerClickHandler, SpatialPointerEvents.spatialPointerClickHandler, out int _);
            pointerClick?.Invoke(eventData);
        }

        private GameObject ExecuteInitializePotentialDrag(GameObject go, SpatialPointerEventData eventData, out ObjectType objectType)
        {
            GameObject dragObject = ExecuteEvents.GetEventHandlerMultiple<IDragHandler, ISpatialDragHandler>(go, out int calledIndex);
            if (calledIndex == 0)
            {
                // IDragHandler was called
                ExecuteEvents.Execute(dragObject, eventData, ExecuteEvents.initializePotentialDrag);
                objectType = ObjectType.UI;
                return dragObject;
            }
            else if (calledIndex == 1)
            {
                // ISpatialDragHandler was called
                ExecuteEvents.Execute(dragObject, eventData, SpatialPointerEvents.spatialInitializePotentialDrag);
                objectType = ObjectType.SpatialUI;
                return dragObject;
            }
            objectType = ObjectType.OutsideUI;
            return null;
        }

        private void ExecuteBeginDrag(GameObject go, SpatialPointerEventData eventData)
        {
            ExecuteEvents.ExecuteMultiple(go, eventData, ExecuteEvents.beginDragHandler, SpatialPointerEvents.spatialBeginDragHandler, out int _);
            beginDrag?.Invoke(eventData);
        }
         
        private void ExecuteDrag(GameObject go, SpatialPointerEventData eventData)
        {
            ExecuteEvents.ExecuteMultiple(go, eventData, ExecuteEvents.dragHandler, SpatialPointerEvents.spatialDragHandler, out int _);
            drag?.Invoke(eventData);
        }

        private void ExecuteEndDrag(GameObject go, SpatialPointerEventData eventData)
        {
            ExecuteEvents.ExecuteMultiple(go, eventData, ExecuteEvents.endDragHandler, SpatialPointerEvents.spatialEndDragHandler, out int _);
            endDrag?.Invoke(eventData);
        }

        private void ExecuteDrop(GameObject go, SpatialPointerEventData eventData)
        {
            ExecuteEvents.ExecuteHierarchyMultiple(go, eventData, ExecuteEvents.dropHandler, SpatialPointerEvents.spatialDropHandler, out int _);
            drop?.Invoke(eventData);
        }

        private void ExecuteScroll(SpatialPointerEventData eventData)
        {
            GameObject scrollHandler = ExecuteEvents.GetEventHandlerMultiple<IScrollHandler, ISpatialScrollHandler>(eventData.pointerEnter, out int calledIndex);
            ExecuteEvents.ExecuteHierarchyMultiple(scrollHandler, eventData, ExecuteEvents.scrollHandler, SpatialPointerEvents.spatialScrollHandler, out calledIndex);
            scroll?.Invoke(eventData);
        }

        #endregion

        #region Get Configuration / Reticle Data

        /// <summary>
        /// Gets reticle data from interface of GameObject, from outsideUIGetReticleData or the default. 
        /// </summary>
        private bool GetReticleData(SpatialPointerEventData eventData, GameObject go, out SpatialPointerReticleData reticleData)
        {
            if (go != null)
            {
                ISpatialPointerCustomReticle customReticleInterface = go.GetComponent<ISpatialPointerCustomReticle>();
                if (customReticleInterface != null)
                {
                    SpatialPointerReticleData data = customReticleInterface.ReticleData;
                    if (data != null)
                    {
                        reticleData = data;
                        return true;
                    }
                }
            }
            reticleData = SpatialPointerReticleData.Default;
            return false;
        }

        private SpatialPointerReticleData GetReticleDataOutsideUI(SpatialPointerEventData eventData)
        {
            if (eventData.outsideUIPointerEnter && outsideUIGetReticleData != null)
            {
                SpatialPointerReticleData data = outsideUIGetReticleData();
                if (data != null)
                {
                    return data;
                }
            }
            return SpatialPointerReticleData.Default;
        }

        /// <summary>
        /// Gets configuration from interface of GameObject, from outsideUIGetConfiguration, or the default. 
        /// </summary>
        private SpatialPointerConfiguration GetSpatialPointerConfiguration(SpatialPointerEventData eventData, GameObject go)
        {
            if (go != null)
            {
                ISpatialPointerConfiguration configurationInterface = go.GetComponent<ISpatialPointerConfiguration>();
                if (configurationInterface != null)
                {
                    SpatialPointerConfiguration data = configurationInterface.Configuration;
                    if (data != null)
                    {
                        return data;
                    }
                }
            }
            if (eventData.outsideUIPointerEnter && outsideUIGetConfiguration != null)
            {
                SpatialPointerConfiguration data = outsideUIGetConfiguration();
                if (data != null)
                {
                    return data;
                }
            }
            return SpatialPointerConfiguration.Default;
        }

        #endregion

        #region Raycast With Plane

        public static bool RaycastWithPlane(SpatialPointerEventData eventData, Plane plane, out Vector3 result, out float distance)
        {
            List<Vector3> rayPoints = eventData.rayPoints;
            result = Vector3.zero;
            float cummulativeDistance = 0f;
            for (int i = 1; i < rayPoints.Count; i++)
            {
                Vector3 from = rayPoints[i - 1];
                Vector3 to = rayPoints[i];

                if (PerformRaycastWithPlane(from, to, plane, out result, ref cummulativeDistance))
                {
                    eventData.rayHitIndex = i;
                    distance = cummulativeDistance;
                    eventData.distance = distance;
                    return true;
                }
            }
            distance = cummulativeDistance;
            eventData.distance = distance;
            return false;
        }

        private static bool PerformRaycastWithPlane(Vector3 from, Vector3 to, Plane plane, out Vector3 result, ref float distance)
        {
            float rayDistance = Vector3.Distance(to, from);
            Ray ray = new Ray(from, (to - from).normalized * rayDistance);

            float hitDistance = rayDistance;

            result = Vector3.zero;

            if (plane.Raycast(ray, out float enter))
            {
                if (enter <= hitDistance)
                {
                    result = ray.GetPoint(enter);
                    distance += enter;
                    return true;
                }
            }
            distance += rayDistance;
            return false;
        }

        #endregion

        #region InputModule Base Implementation

        /// <summary>
        /// Calls the event system to perform all raycasts, will fire all SpatialPhysicsRaycasters and SpatialGraphicRaycasters. 
        /// </summary>
        private RaycastResult PerformRaycast(SpatialPointerEventData eventData)
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            eventSystem.RaycastAll(eventData, m_RaycastResultCache);
            RaycastResult raycastResult = FindFirstRaycast(m_RaycastResultCache);
            m_RaycastResultCache.Clear();
            return raycastResult;
        }

        private bool TryGetCamera(SpatialPointerEventData eventData, out Camera screenPointCamera)
        {
            screenPointCamera = UICamera;
            if (screenPointCamera != null)
            {
                return true;
            }

            var module = eventData.pointerCurrentRaycast.module;
            if (module != null)
            {
                screenPointCamera = module.eventCamera;
                return screenPointCamera != null;
            }

            return false;
        }

        public override void Process()
        {
            // updating is done in the Update, after the position and rotation of the interactor has got the chance to update
        }

        /// <summary>
        /// Gets an <see cref="RealityRayInteractor"/> from its corresponding Unity UI Pointer Id.
        /// This can be used to identify individual Interactors from the underlying UI Events.
        /// </summary>
        /// <param name="pointerId">A unique integer representing an object that can point at UI.</param>
        /// <returns>Returns the interactor associated with <paramref name="pointerId"/>.
        /// Returns <see langword="null"/> if no Interactor is associated (e.g. if it's a mouse event).</returns>
        public RayInteractor GetInteractor(int pointerId)
        {
            for (int i = 0; i < _registeredRayInteractors.Count; i++)
            {
                if (_registeredRayInteractors[i].PointerId == pointerId)
                {
                    return _registeredRayInteractors[i];
                }
            }

            return null;
        }

        private SpatialPointerEventData GetOrCreateCachedTrackedDeviceEvent(int pointerId)
        {
            if (!_trackedDeviceEventByPointerId.TryGetValue(pointerId, out var result))
            {
                result = new SpatialPointerEventData(eventSystem);
                _trackedDeviceEventByPointerId.Add(pointerId, result);
            }

            return result;
        }

        private int _lastPointerId = 0;

        /// <summary>
        /// Register an <see cref="RayInteractor"/> with the UI system.
        /// Calling this will enable it to start interacting with UI.
        /// </summary>
        public void RegisterInteractor(RayInteractor interactor)
        {
            if (!_registeredRayInteractors.Contains(interactor))
            {
                _registeredRayInteractors.Add(interactor);
                interactor.PointerId = ++_lastPointerId;
            }
        }

        /// <summary>
        /// Unregisters an <see cref="RayInteractor"/> with the UI system.
        /// This cancels all UI Interaction and makes the <see cref="RealityRayInteractor"/> no longer able to affect UI.
        /// </summary>
        public void UnregisterInteractor(RayInteractor interactor)
        {
            if (_registeredRayInteractors.Contains(interactor))
            {
                _registeredRayInteractors.Remove(interactor);
            }
        }

        #endregion
    }
}
