// Copyright (c) 2023 Arjo Nagelhout

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cuboid.Utils
{
    /// <summary>
    /// 
    /// </summary>
    public static class ObjectFitUtils
    {
        /// <summary>
        /// Instantiates an object and fits it inside the provided transform. 
        /// </summary>
        public static GameObject InstantiateObjectInsideTransform(GameObject prefab, Transform transform)
        {
            GameObject instantiatedPrefab = GameObject.Instantiate(prefab, null, false);
            instantiatedPrefab.transform.localPosition = Vector3.zero;

            ObjectFitUtils.FitObject(instantiatedPrefab, out Vector3 positionOffset, out Vector3 scale);

            instantiatedPrefab.transform.localPosition = positionOffset;
            instantiatedPrefab.transform.localScale = Vector3.Scale(instantiatedPrefab.transform.localScale, scale);

            instantiatedPrefab.transform.SetParent(transform, false);

            return instantiatedPrefab;
        }

        /// <summary>
        /// Fits the object and its child objects into a 1 by 1 by 1 cube (in local scale)
        /// Returns the corrected local position and scale
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns>(offset, scale)</returns>
        public static void FitObject(GameObject gameObject, out Vector3 positionOffset, out Vector3 scale)
        {
            Bounds totalBounds = BoundsUtils.GetBounds(gameObject);
            Vector3 extents = totalBounds.extents * 2;
            GetLargestComponent(extents, out int index, out float largestValue); // Use the largest component for the scale of the object.
            scale = Vector3.one / largestValue;
            positionOffset = Vector3.Scale(-totalBounds.center, scale);
        }

        /// <summary>
        /// Returns the largest value from a given array of float values
        /// </summary>
        /// <param name="values"></param>
        /// <returns>Index and value</returns>
        public static void GetLargestValue(float[] values, out int largestValueIndex, out float largestValue)
        {
            largestValue = 0f;
            largestValueIndex = 0;
            for (int i = 0; i < values.Length; i++)
            {
                float componentValue = values[i];
                if (componentValue > largestValue)
                {
                    largestValueIndex = i;
                    largestValue = componentValue;
                }
            }
        }

        /// <summary>
        /// Returns the largest component (index) from a vector
        /// </summary>
        /// <param name="vector"></param>
        /// <returns>Index and value</returns>
        public static void GetLargestComponent(Vector3 vector, out int largestComponentIndex, out float largestValue)
        {
            float[] components = { vector.x, vector.y, vector.z };
            GetLargestValue(components, out largestComponentIndex, out largestValue);
        }

        /// <summary>
        /// Returns the smallest value from a given array of float values
        /// </summary>
        /// <param name="values"></param>
        /// <returns>Index and value</returns>
        public static void GetSmallestValue(float[] values, out int smallestValueIndex, out float smallestValue)
        {
            smallestValue = Mathf.Infinity;
            smallestValueIndex = 0;
            for (int i = 0; i < values.Length; i++)
            {
                float componentValue = values[i];
                if (componentValue < smallestValue)
                {
                    smallestValueIndex = i;
                    smallestValue = componentValue;
                }
            }
        }

        /// <summary>
        /// Returns the smallest component (index) from a vector
        /// </summary>
        /// <param name="vector"></param>
        /// <returns>Index and value</returns>
        public static void GetSmallestComponent(Vector3 vector, out int smallestComponentIndex, out float smallestValue)
        {
            float[] components = { vector.x, vector.y, vector.z };
            GetSmallestValue(components, out smallestComponentIndex, out smallestValue);
        }
    }
}
