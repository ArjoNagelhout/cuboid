// Copyright (c) 2023 Arjo Nagelhout

using System;
using UnityEngine;
using Cuboid.Models;

namespace Cuboid.Utils
{
    public static class TransformExtensions
    {
        /// <summary>
        /// Returns the world space forward ray for a specific transform
        /// </summary>
        /// <param name="transform"></param>
        /// <returns></returns>
        public static Ray ToRay(this Transform transform)
        {
            return new Ray(transform.position, transform.forward);
        }

        /// <summary>
        /// Sets the transform position, rotation and scale to the values of the given transform data
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="transformData"></param>
        public static void SetFromTransformData(this Transform transform, TransformData transformData)
        {
            transform.localPosition = transformData.Position;
            transform.localRotation = transformData.Rotation;
            transform.localScale = transformData.Scale;
        }

        public static Transform FindRecursive(this Transform self, string exactName) => self.FindRecursive(child => child.name == exactName);

        public static Transform FindRecursive(this Transform self, Func<Transform, bool> selector)
        {
            foreach (Transform child in self)
            {
                if (selector(child))
                {
                    return child;
                }

                var finding = child.FindRecursive(selector);

                if (finding != null)
                {
                    return finding;
                }
            }

            return null;
        }
    }
}