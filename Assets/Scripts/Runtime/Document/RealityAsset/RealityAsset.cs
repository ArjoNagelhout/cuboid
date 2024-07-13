// 
// RealityAsset.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace Cuboid
{
    [PrettyTypeNameAttribute(Name = "Asset")]
    public class RealityAssetObjectData : RealityObjectData
    {
        public AssetData AssetData;

        public override IEnumerator InstantiateAsync(Action<RealityObject> completed)
        {
            return RealityAssetsController.Instance.InstantiateAsset(AssetData, completed);
        }
    }

    public class RealityAsset : RealityObject
    {

    }
}

