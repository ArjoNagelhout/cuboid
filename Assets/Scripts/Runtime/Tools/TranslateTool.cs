using System.Collections;
using System.Collections.Generic;
using Cuboid.Models;
using UnityEngine;
using static Cuboid.TransformCommand;

namespace Cuboid
{
    /// <summary>
    /// Tool that allows translation along an axis, or along a face.
    ///
    /// Orients the face gizmo towards the quadrant that currently faces the user.
    ///
    /// Supports global / local orientation
    /// </summary>
    public class TranslateTool : AxisHandleTool<TranslateHandle, TranslateHandleData>
    {
        [SerializeField] private GameObject _axisHandlePrefab;
        [SerializeField] private GameObject _planeHandlePrefab;

        private TranslateHandle[] _axisHandles = new TranslateHandle[3];
        private TranslateHandle[] _planeHandles = new TranslateHandle[3];

        protected override void InstantiateHandles()
        {
            _axisHandles = InstantiateHandles(_axisHandlePrefab, data => data.handleType = TranslateHandleData.HandleType.Axis);
            _planeHandles = InstantiateHandles(_planeHandlePrefab, data => data.handleType = TranslateHandleData.HandleType.Plane);
        }

        protected override void UpdateHandlesRotation()
        {
            RotateAxisHandles(_axisHandles);
            RotatePlaneHandles(_planeHandles);
        }

        protected override void OnHandlePositionUpdated(Vector3 pressedPosition, Vector3 position, TranslateHandleData data)
        {
            TransformData initialTransformData = _selectionController.InitialSelectionTransformData;
            TransformData newTransformData = initialTransformData;

            Vector3 axis = data.axis.ToVector3();
            // get local position of the new handle position
            Vector3 localPosition = initialTransformData.WorldToLocalMatrix.MultiplyPoint3x4(position);

            Vector3 localProjectedPosition = Vector3.zero;
            if (data.handleType == TranslateHandleData.HandleType.Axis)
            {
                // project onto the axis
                localProjectedPosition = Vector3.Project(localPosition, axis);
            }
            else if (data.handleType == TranslateHandleData.HandleType.Plane)
            {
                // project onto the plane
                Plane plane = new Plane(axis, Vector3.zero);
                localProjectedPosition = plane.ClosestPointOnPlane(localPosition);
            }
            // change into global position
            Vector3 projectedPosition = initialTransformData.LocalToWorldMatrix.MultiplyPoint3x4(localProjectedPosition);

            // update the selection transform data
            newTransformData.SetPositionMutating(projectedPosition);

            _selectionController.SetCurrentSelectionTransformData(newTransformData);
            _selectionController.UpdateRealityObjectInstanceTransforms();
        }
    }
}
