// 
// TransformData.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 

using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Cuboid.Utils;

namespace Cuboid.Models
{
    public static class TransformDataExtensions
    {
        public static TransformData SetPositionMutating(this ref TransformData transformData, Vector3 position) { transformData.Position = position; return transformData;  }
        public static TransformData SetRotationMutating(this ref TransformData transformData, Quaternion rotation) { transformData.Rotation = rotation; return transformData;}
        public static TransformData SetRotationMutating(this ref TransformData transformData, Vector3 eulerRotation) { transformData.Rotation = Quaternion.Euler(eulerRotation); return transformData; }
        public static TransformData SetScaleMutating(this ref TransformData transformData, Vector3 scale) { transformData.Scale = scale; return transformData; }

        public static TransformData SetPositionXMutating(this ref TransformData transformData, float value) { transformData.Position.SetXMutating(value); return transformData;}
        public static TransformData SetPositionYMutating(this ref TransformData transformData, float value) { transformData.Position.SetYMutating(value); return transformData;}
        public static TransformData SetPositionZMutating(this ref TransformData transformData, float value) { transformData.Position.SetZMutating(value); return transformData;}

        public static TransformData SetRotationXMutating(this ref TransformData transformData, float value) { transformData.Rotation.SetXMutating(value); return transformData;}
        public static TransformData SetRotationYMutating(this ref TransformData transformData, float value) { transformData.Rotation.SetYMutating(value); return transformData;}
        public static TransformData SetRotationZMutating(this ref TransformData transformData, float value) { transformData.Rotation.SetZMutating(value); return transformData;}

        public static TransformData SetScaleXMutating(this ref TransformData transformData, float value) { transformData.Scale.SetXMutating(value); return transformData;}
        public static TransformData SetScaleYMutating(this ref TransformData transformData, float value) { transformData.Scale.SetYMutating(value); return transformData;}
        public static TransformData SetScaleZMutating(this ref TransformData transformData, float value) { transformData.Scale.SetZMutating(value); return transformData;}
    }

    /// <summary>
    /// Representation of a transform in data
    /// So that it can be serialized and read from a file
    /// </summary>
    public struct TransformData
    {
        /// <summary>
        /// Local Position
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Local Rotation
        /// </summary>
        public Quaternion Rotation;

        /// <summary>
        /// Local Scale
        /// </summary> 
        public Vector3 Scale;

        /// <summary>
        /// Constructs the TRS (Translation, Rotation, Scale) matrix.
        /// Does not contain skew or projection information. 
        /// </summary>
        [JsonIgnore]
        public Matrix4x4 Matrix => Matrix4x4.TRS(Position, Rotation, Scale);

        [JsonIgnore]
        public Matrix4x4 InverseMatrix => Matrix4x4.Inverse(LocalToWorldMatrix);

        /// <summary>
        /// Convenience property for Matrix
        /// </summary>
        [JsonIgnore]
        public Matrix4x4 LocalToWorldMatrix => Matrix;

        /// <summary>
        /// Convenience property for InverseMatrix
        /// </summary>
        [JsonIgnore]
        public Matrix4x4 WorldToLocalMatrix => InverseMatrix;

        public TransformData(Matrix4x4 matrix)
        {
            Position = matrix.GetPosition();
            Rotation = matrix.GetRotation();
            Scale = matrix.GetScale();
        }

        /// <summary>
        /// Constructs transform data from the local values of the transform
        /// </summary>
        /// <param name="transform"></param>
        public TransformData(Transform transform)
        {
            Position = transform.localPosition;
            Rotation = transform.localRotation;
            Scale = transform.localScale;
        }

        public TransformData(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public TransformData SetPosition(Vector3 position) => new TransformData(position, Rotation, Scale);

        public TransformData SetPositionX(float value) => new TransformData(Position.SetX(value), Rotation, Scale);
        public TransformData SetPositionY(float value) => new TransformData(Position.SetY(value), Rotation, Scale);
        public TransformData SetPositionZ(float value) => new TransformData(Position.SetZ(value), Rotation, Scale);

        public TransformData SetRotation(Quaternion rotation) => new TransformData(Position, rotation, Scale);

        public TransformData SetRotationX(float value) => new TransformData(Position, Quaternion.Euler(Rotation.eulerAngles.SetX(value)), Scale);
        public TransformData SetRotationY(float value) => new TransformData(Position, Quaternion.Euler(Rotation.eulerAngles.SetY(value)), Scale);
        public TransformData SetRotationZ(float value) => new TransformData(Position, Quaternion.Euler(Rotation.eulerAngles.SetZ(value)), Scale);

        public TransformData SetScale(Vector3 scale) => new TransformData(Position, Rotation, scale);

        public TransformData SetScaleX(float value) => new TransformData(Position, Rotation, Scale.SetX(value));
        public TransformData SetScaleY(float value) => new TransformData(Position, Rotation, Scale.SetY(value));
        public TransformData SetScaleZ(float value) => new TransformData(Position, Rotation, Scale.SetZ(value));

        public static TransformData WorldPositionAndRotation(Transform transform)
        {
            return new TransformData(transform.position, transform.rotation, Vector3.one);
        }
    }
}
