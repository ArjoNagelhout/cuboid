// Copyright (c) 2023 Arjo Nagelhout

using System.Collections;
using System.Collections.Generic;
using Cuboid.Models;
using UnityEngine;
using Cuboid.Utils;
using Cuboid.Input;
using UnityEngine.UIElements;

namespace Cuboid
{
    public class RotateTool : AxisHandleTool<RotateHandle, AxisHandleData>
    {
        [SerializeField] private GameObject _rotateHandlePrefab;

        private RotateHandle[] _rotateHandles = new RotateHandle[3];

        protected override void InstantiateHandles()
        {
            _rotateHandles = InstantiateHandles(_rotateHandlePrefab);
        }

        protected override void UpdateHandlesRotation()
        {
            RotatePlaneHandles(_rotateHandles);
        }

        protected override void OnHandleBeginDrag()
        {
            base.OnHandleBeginDrag();

            // show rotation visual
        }

        protected override void OnHandleEndDrag()
        {
            base.OnHandleEndDrag();

            // hide rotation visual
        }

        protected override void OnHandleDrag(SpatialPointerEventData eventData, AxisHandleData data)
        {
            TransformData initialTransformData = _selectionController.InitialSelectionTransformData;
            TransformData newTransformData = initialTransformData;

            // there are two approaches for rotation, one being the raycast with plane approach
            // and the other being using the world position, and simply projecting it to the rotation plane
            // 
            // the RotateHandle could be changed so that it sets its Configuration.customGetSpatialPointerInputPosition
            // delegate, but it will probably feel inconsistent with the rest of the interactions.

            // we can convert the position to the local coordinate space of the initialTransformData

            Axis axis = data.axis;
            (Axis, Axis) otherAxes = axis.GetOtherAxes();

            Plane plane = new Plane(axis.ToVector3(), Vector3.zero);

            // pressed position
            Vector3 pressedPosition = eventData.spatialPressPosition;
            Vector3 position = eventData.spatialPosition;

            initialTransformData.SetScaleMutating(Vector3.one);

            Vector3 localPressedPosition = initialTransformData.WorldToLocalMatrix.MultiplyPoint3x4(pressedPosition);
            Vector3 localPosition = initialTransformData.WorldToLocalMatrix.MultiplyPoint3x4(position);

            Vector3 localProjectedPressedPosition = plane.ClosestPointOnPlane(localPressedPosition);
            Vector3 localProjectedPosition = plane.ClosestPointOnPlane(localPosition);

            float deltaAngle = Vector3.SignedAngle(localProjectedPressedPosition, localProjectedPosition, axis.ToVector3());

            if (ModifiersController.Instance.ShiftModifier.Value)
            {
                deltaAngle = deltaAngle.Snap(360, 8);
            }
            
            Quaternion initialRotation = initialTransformData.Rotation;

            Vector3 rotatedAxis = initialRotation * axis.ToVector3();
            Quaternion deltaRotation = Quaternion.AngleAxis(deltaAngle, rotatedAxis);

            // we should multiply the rotations, because setting the rotation of a specific axis (x, y or z)
            // doesn't actually rotate it around that axis.
            Quaternion rotation = deltaRotation * initialRotation;

            newTransformData.SetRotationMutating(rotation);

            _selectionController.SetCurrentSelectionTransformData(newTransformData);
            _selectionController.UpdateRealityObjectInstanceTransforms();
        }
    }
}
