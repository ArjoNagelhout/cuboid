// Copyright (c) 2023 Arjo Nagelhout

using System.Collections;
using System.Collections.Generic;
using Cuboid.Input;
using Cuboid.Models;
using UnityEngine;
using Cuboid.Utils;

namespace Cuboid
{
    public class ScaleTool : AxisHandleTool<ScaleHandle, AxisHandleData>
    {
        [SerializeField] private GameObject _scaleHandlePrefab;

        private ScaleHandle[] _scaleHandles = new ScaleHandle[3];

        protected override void InstantiateHandles()
        {
            _scaleHandles = InstantiateHandles(_scaleHandlePrefab);
        }

        protected override void UpdateHandlesRotation()
        {
            RotateAxisHandles(_scaleHandles);
        }

        protected override void OnHandleDrag(SpatialPointerEventData eventData, AxisHandleData data)
        {
            TransformData initialTransformData = _selectionController.InitialSelectionTransformData;
            TransformData newTransformData = initialTransformData;

            Vector3 pressedPosition = eventData.spatialPressPosition;
            Vector3 position = eventData.spatialPosition;

            Axis axis = data.axis;
            int index = (int)axis;

            Vector3 localPosition = initialTransformData.WorldToLocalMatrix.MultiplyPoint3x4(position);
            Vector3 localPressedPosition = initialTransformData.WorldToLocalMatrix.MultiplyPoint3x4(pressedPosition);

            Vector3 scale = initialTransformData.Scale;

            float originalPosition = localPressedPosition[index];
            float originalScale = scale[index];

            float newPosition = localPosition[index];
            float newScale = Math.Map(newPosition, 0, originalPosition, 0, originalScale);

            scale[index] = newScale;

            newTransformData.SetScaleMutating(scale);

            _selectionController.SetCurrentSelectionTransformData(newTransformData);
            _selectionController.UpdateRealityObjectInstanceTransforms();
        }
    }
}
