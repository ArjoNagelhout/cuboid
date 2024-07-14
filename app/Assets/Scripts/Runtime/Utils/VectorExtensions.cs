// Copyright (c) 2023 Arjo Nagelhout

using System;
using UnityEngine;
using static Unity.Mathematics.math;

namespace Cuboid
{
    public enum Axis
    {
        X = 0,
        Y,
        Z
    }

    public static class AxisExtensions
    {
        public static Vector3 ToVector3(this Axis axis)
        {
            Vector3 value = Vector3.zero;
            value[(int)axis] = 1.0f;
            return value;
        }

        public static (Axis, Axis) GetOtherAxes(this Axis axis) => axis switch
        {
            Axis.X => (Axis.Y, Axis.Z),
            Axis.Y => (Axis.X, Axis.Z),
            Axis.Z => (Axis.X, Axis.Y),
            _ => (Axis.X, Axis.Y)
        };
    }

    public static class VectorExtensions
    {
        public static Vector3 SetX(this Vector3 vector, float x) => new Vector3(x, vector.y, vector.z);
        public static Vector3 SetY(this Vector3 vector, float y) => new Vector3(vector.x, y, vector.z);
        public static Vector3 SetZ(this Vector3 vector, float z) => new Vector3(vector.x, vector.y, z);

        public static Vector2 SetX(this Vector2 vector, float x) => new Vector2(x, vector.y);
        public static Vector2 SetY(this Vector2 vector, float y) => new Vector2(vector.x, y);


        public static void SetXMutating(this ref Vector3 vector, float x) { vector.x = x; }
        public static void SetYMutating(this ref Vector3 vector, float y) { vector.y = y; }
        public static void SetZMutating(this ref Vector3 vector, float z) { vector.z = z; }

        public static void SetXMutating(this ref Vector2 vector, float x) { vector.x = x; }
        public static void SetYMutating(this ref Vector2 vector, float y) { vector.y = y; }

        public static bool RoughlyEquals(this Vector3 vector, Vector3 otherVector)
        {
            return Vector3.Distance(vector, otherVector) <= 0.001f;
        }

        public static Vector3 Clamp(this Vector3 vector, Vector3 minVector, Vector3 maxVector)
        {
            return new Vector3(
                Mathf.Clamp(vector.x, minVector.x, maxVector.x),
                Mathf.Clamp(vector.y, minVector.y, maxVector.y),
                Mathf.Clamp(vector.z, minVector.z, maxVector.z));
        }

        public static Vector3 Inverse(this Vector3 vector)
        {
            // this is to make sure that there are no division by zero, resulting
            // in infinity getting assigned to a Vector3 which makes Unity throw errors
            float x = Mathf.Approximately(vector.x, 0.0f) ? 0f : 1 / vector.x;
            float y = Mathf.Approximately(vector.y, 0.0f) ? 0f : 1 / vector.y;
            float z = Mathf.Approximately(vector.z, 0.0f) ? 0f : 1 / vector.z;
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Extend a given vector to all axes, but not scaled / proportionally
        /// Simply copy the value of the other components to the one that is 0.
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector3 ExtendToAllAxes(this Vector3 vector)
        {
            return vector switch
            {
                { x: 0, y: 0 } => new Vector3(vector.z, vector.z, vector.z),
                { x: 0, z: 0 } => new Vector3(vector.y, vector.y, vector.y),
                { y: 0, z: 0 } => new Vector3(vector.x, vector.x, vector.x),
                { x: 0 } => new Vector3(vector.y, vector.y, vector.z),
                { y: 0 } => new Vector3(vector.x, vector.x, vector.z),
                { z: 0 } => new Vector3(vector.x, vector.y, vector.x),
                _ => vector
            };
        }
    }
}