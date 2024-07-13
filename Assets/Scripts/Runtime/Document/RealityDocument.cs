// 
// RealityDocument.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cuboid.Models;
using Newtonsoft.Json;

namespace Cuboid
{
    [Serializable]
    public class RealityDocument
    {
        /// <summary>
        /// Scenes, accessed by guid
        /// </summary>
        public List<RealitySceneData> ScenesData;

        [JsonIgnore] // should be a custom serializer along the following lines: [JsonConverter(typeof(Vector3Converter))]
        public List<Vector3> CalibrationPoints;

        public enum ViewingMode
        {
            Fixed,
            WorldScale,
            Miniature
        }

        public Binding<ViewingMode> ActiveViewingMode;

        public static RealityDocument CreateEmptyRealityDocument()
        {
            return new RealityDocument()
            {
                ScenesData = new List<RealitySceneData>()
                {
                    new RealitySceneData()
                    {
                        Name = "Scene 1",
                        RealityObjects = new Dictionary<Guid, RealityObjectData>()
                        {
                        }
                    }
                },
                CalibrationPoints = new List<Vector3>()
                {
                    Vector3.zero,
                    Vector3.right,
                    Vector3.forward
                },
                ActiveViewingMode = new Binding<ViewingMode>(ViewingMode.Fixed)
            };
        }
    }

    [Serializable]
    public class RealitySceneData
    {
        /// <summary>
        /// Name of the scene
        /// </summary>
        public string Name;

        public Dictionary<Guid, RealityObjectData> RealityObjects;
    }
}

