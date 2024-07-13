// 
// Matrix4x4Extensions.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid
{
    /// <summary>
    /// From https://answers.unity.com/questions/402280/how-to-decompose-a-trs-matrix.html
    /// </summary>
    public static class Matrix4x4Extensions
    {
        public static Vector3 GetScale(this Matrix4x4 matrix)
        {
            return new Vector3(
                matrix.GetColumn(0).magnitude,
                matrix.GetColumn(1).magnitude,
                matrix.GetColumn(2).magnitude
            );
        }

        public static Quaternion GetRotation(this Matrix4x4 matrix)
        {
            return Quaternion.LookRotation(
                matrix.GetColumn(2),
                matrix.GetColumn(1)
            );
        }
    }
}

