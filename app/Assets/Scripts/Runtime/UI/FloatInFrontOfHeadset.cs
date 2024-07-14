// Copyright (c) 2023 Arjo Nagelhout

using System.Collections;
using System.Collections.Generic;
using Cuboid.Utils;
using UnityEngine;

namespace Cuboid
{
    /// <summary>
    /// Component that will move the transform it is attached to in front of the headset
    /// </summary>
    public class FloatInFrontOfHeadset : MonoBehaviour
    {
        [SerializeField] private float _minimumStabilizationDistance = 0.05f;
        [SerializeField] private float _stabilizationRadius = 1f;
        [SerializeField] private float _distanceFromCamera = 1.0f;
        [SerializeField] private float _smoothToLerpTime = 0.1f;

        private Transform _cameraTransform;

        private void Start()
        {
            _cameraTransform = Camera.main.transform;
            transform.position = CalculateTargetPosition();
        }

        private void Update()
        {
            Vector3 target = CalculateTargetPosition();

            float distance = Vector3.Distance(_cameraTransform.position, target);

            float stabilizationRadius = GetStabilizationRadius(_stabilizationRadius, _minimumStabilizationDistance, distance);
            target = Stabilize(transform.position, target, stabilizationRadius);

            // smooth from current position to the target position
            transform.position = Smoothing.SmoothTo(transform.position, target, _smoothToLerpTime, Time.deltaTime);

            // update rotation
            Vector3 delta = transform.position - _cameraTransform.position;
            transform.localRotation = Quaternion.LookRotation(delta, Vector3.up);
        }

        private void OnEnable()
        {
            // set the transform immediately to the target
            if (_cameraTransform != null)
            {
                transform.position = CalculateTargetPosition();
            }
        }

        private static float GetStabilizationRadius(float stabilizationRadius, float referenceDistance, float distance)
        {
            float distanceCorrectedRadius = stabilizationRadius;
            if (distance > referenceDistance)
            {
                float distanceFromReferenceDistance = distance - referenceDistance;
                distanceCorrectedRadius = stabilizationRadius + distanceFromReferenceDistance * stabilizationRadius;
            }
            return distanceCorrectedRadius;
        }

        private static Vector3 Stabilize(Vector3 currentPosition, Vector3 targetPosition, float radius)
        {
            Vector3 newPosition = currentPosition;

            // stabilize stroke (such as in https://docs.blender.org/manual/en/latest/sculpt_paint/brush/stroke.html)
            Vector3 deltaPosition = targetPosition - currentPosition;
            if (deltaPosition.sqrMagnitude >= radius * radius)
            {
                Vector3 lastPosition = deltaPosition.normalized * radius;
                Vector3 realDeltaPosition = deltaPosition - lastPosition;
                newPosition = currentPosition + realDeltaPosition;
            }

            return newPosition;
        }

        private Vector3 CalculateTargetPosition()
        {
            Vector3 forward = Vector3.forward;

            // then we want to rotate that by the camera transform
            forward = _cameraTransform.rotation * forward;

            // now move relative to the camera transform with distance
            Vector3 targetPosition = _cameraTransform.position + forward * _distanceFromCamera;

            return targetPosition;
        }
    }
}
