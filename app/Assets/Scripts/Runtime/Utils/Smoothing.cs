// 
// Smoothing.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 

using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace Cuboid.Utils
{
    /// <summary>
    /// Static methods for smooth interpolation for extra polish
    /// </summary>
    public static class Smoothing
    {
        public static float TimeMultiplier => Time.deltaTime * 90f; // 90f used as the basis framerate in VR

        /// <summary>
        /// Smooths from source to goal, provided lerptime and a deltaTime.
        /// </summary>
        /// <param name="source">Current value</param>
        /// <param name="goal">"goal" value which will be lerped to</param>
        /// <param name="lerpTime">Smoothing/lerp amount. Smoothing of 0 means no smoothing, and max value means no change at all.</param>
        /// <param name="deltaTime">Delta time. Usually would be set to Time.deltaTime</param>
        /// <returns>Smoothed value</returns>
        public static Vector3 SmoothTo(Vector3 source, Vector3 goal, float lerpTime, float deltaTime)
        {
            return Vector3.Lerp(source, goal, (lerpTime == 0f) ? 1f : 1f - Mathf.Pow(lerpTime, deltaTime));
        }
    }
}