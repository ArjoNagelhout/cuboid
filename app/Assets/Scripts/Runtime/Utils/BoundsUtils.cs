//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using Cuboid.Models;
using UnityEngine;

namespace Cuboid
{
    public static class BoundsUtils
    {
        /// <summary>
        /// Transforms 'bounds' using the specified transform matrix.
        /// </summary>
        /// <remarks>
        /// <para>Transforming a 'Bounds' instance means that the function will construct a new 'Bounds' 
        /// instance which has its center translated using the translation information stored in
        /// the specified matrix and its size adjusted to account for rotation and scale. The size
        /// of the new 'Bounds' instance will be calculated in such a way that it will contain the
        /// old 'Bounds'.</para>
        /// </remarks>
        /// <param name="bounds">
        /// The 'Bounds' instance which must be transformed.
        /// </param>
        /// <param name="transformMatrix">
        /// The specified 'Bounds' instance will be transformed using this transform matrix. The function
        /// assumes that the matrix doesn't contain any projection or skew transformation.
        /// </param>
        /// <returns>
        /// The transformed 'Bounds' instance.
        /// </returns>
        public static Bounds Transform(this Bounds bounds, Matrix4x4 transformMatrix)
        {
            // We will need access to the right, up and look vector which are encoded inside the transform matrix
            Vector3 rightAxis = transformMatrix.GetColumn(0);
            Vector3 upAxis = transformMatrix.GetColumn(1);
            Vector3 lookAxis = transformMatrix.GetColumn(2);

            // We will 'imagine' that we want to rotate the bounds' extents vector using the rotation information
            // stored inside the specified transform matrix. We will need these when calculating the new size of
            // the transformed bounds.
            Vector3 rotatedExtentsRight = rightAxis * bounds.extents.x;
            Vector3 rotatedExtentsUp = upAxis * bounds.extents.y;
            Vector3 rotatedExtentsLook = lookAxis * bounds.extents.z;

            // Calculate the new bounds size along each axis. The size on each axis is calculated by summing up the 
            // corresponding vector component values of the rotated extents vectors. We multiply by 2 because we want
            // to get a size and currently we are working with extents which represent half the size.
            float newSizeX = (Mathf.Abs(rotatedExtentsRight.x) + Mathf.Abs(rotatedExtentsUp.x) + Mathf.Abs(rotatedExtentsLook.x)) * 2.0f;
            float newSizeY = (Mathf.Abs(rotatedExtentsRight.y) + Mathf.Abs(rotatedExtentsUp.y) + Mathf.Abs(rotatedExtentsLook.y)) * 2.0f;
            float newSizeZ = (Mathf.Abs(rotatedExtentsRight.z) + Mathf.Abs(rotatedExtentsUp.z) + Mathf.Abs(rotatedExtentsLook.z)) * 2.0f;

            // Construct the transformed 'Bounds' instance
            var transformedBounds = new Bounds();
            transformedBounds.center = transformMatrix.MultiplyPoint(bounds.center);
            transformedBounds.size = new Vector3(newSizeX, newSizeY, newSizeZ);

            // Return the instance to the caller
            return transformedBounds;
        }

        public static Bounds GetBoundsTransformed(IEnumerable<RealityObject> realityObjects, TransformData transformation)
        {
            // Calculate the bounds for all the objects
            Bounds totalBounds = new Bounds();
            bool initialized = false;
            foreach (RealityObject realityObject in realityObjects)
            {
                MeshRenderer[] meshRenderers = realityObject.gameObject.GetComponentsInChildren<MeshRenderer>();

                foreach (MeshRenderer meshRenderer in meshRenderers)
                {
                    Matrix4x4 localToWorldMatrix = meshRenderer.localToWorldMatrix;
                    Matrix4x4 transformedLocalToWorldMmatrix = transformation.InverseMatrix * localToWorldMatrix;
                    Bounds bounds = meshRenderer.localBounds.Transform(transformedLocalToWorldMmatrix);

                    if (initialized)
                    {
                        totalBounds.Encapsulate(bounds);
                    }
                    else
                    {
                        totalBounds = bounds;
                        initialized = true;
                    }
                }
            }

            return totalBounds;
        }

        public static Bounds GetBounds(GameObject gameObject)
        {
            MeshRenderer[] meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();

            bool initialized = false;
            Bounds totalBounds = new Bounds(Vector3.zero, Vector3.one);

            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                Bounds newBounds = meshRenderer.bounds;
                // transform the bounds by the transform it's attached to

                if (!initialized)
                {
                    totalBounds = newBounds;
                    initialized = true;
                }
                else
                {
                    totalBounds.Encapsulate(newBounds);
                }
            }

            return totalBounds;
        }
    }
}

