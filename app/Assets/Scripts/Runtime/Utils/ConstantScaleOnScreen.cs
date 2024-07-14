//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cuboid.Utils
{
    public static class ConstantScaleOnScreen
    {
        /// <summary>
        /// Get the amount of pixels one meter spans at a certain world position
        /// </summary>
        /// <param name="position"></param>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static float GetPixelsPerMeter(Vector3 position, Camera camera)
        {
            Vector3 deltaPosition = camera.transform.position - position;
            Vector3 cameraForward = camera.transform.forward;
            Vector3 projectedVector = Vector3.Project(deltaPosition, cameraForward);

            float distanceBetweenCameraAndObjectInMeters = projectedVector.magnitude;

            // This distance should be projected onto the camera forward vector

            float fovInRadians = camera.fieldOfView * Mathf.Deg2Rad;
            float screenHeightInPixels = camera.pixelHeight;

            float screenHeightInMeters = 2 * distanceBetweenCameraAndObjectInMeters * Mathf.Tan(fovInRadians / 2);

            return screenHeightInPixels / screenHeightInMeters;
        }

        /// <summary>
        /// Constant scale on screen based on the given position and required size in pixels of the
        /// object. 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="sizeInPixels"></param>
        /// <returns></returns>
        public static Vector3 GetConstantScaleScreen(Vector3 position, float sizeInPixels)
        {
            float sizeInMeters = sizeInPixels / GetPixelsPerMeter(position, Camera.main);
            return Vector3.one * sizeInMeters;
        }

        /// <summary>
        /// This is spatial (so it won't correct for the camera projection matrix, because
        /// in VR this is unwanted because the user can see the d
        /// </summary>
        /// <param name="position"></param>
        /// <param name="referenceScaleAtOneMeterDistance"></param>
        /// <returns></returns>
        public static Vector3 GetConstantScale(Vector3 position, float referenceScaleAtOneMeterDistance)
        {
            Vector3 cameraPosition = Camera.main.transform.position;
            return referenceScaleAtOneMeterDistance * Vector3.Distance(position, cameraPosition) * Vector3.one;
        }


        /// <summary>
        /// This is spatial (so it won't correct for the camera projection matrix, because
        /// in VR this is unwanted because the user can see the d
        /// </summary>
        /// <param name="position"></param>
        /// <param name="referenceScaleAtOneMeterDistance"></param>
        /// <returns></returns>
        public static Vector3 GetConstantScale(Vector3 position, Vector3 referenceScaleAtOneMeterDistance)
        {
            Vector3 cameraPosition = Camera.main.transform.position;
            return Vector3.Distance(position, cameraPosition) * referenceScaleAtOneMeterDistance;
        }
    }
}
