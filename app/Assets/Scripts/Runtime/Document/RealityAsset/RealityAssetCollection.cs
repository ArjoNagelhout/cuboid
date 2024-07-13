// 
// RealityAssetCollection.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 

using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using UnityEngine;
using UnityEngine.U2D;
using System.Threading.Tasks;
using Cuboid.UnityPlugin;

namespace Cuboid
{
    [Serializable]
    public class AssetData
    {
        /// <summary>
        /// See <see cref="CollectionData.Identifier"/>
        /// </summary>
        public string CollectionIdentifier;

        public string AddressableName;

        public string Identifier => string.Join(UnityPlugin.Constants.k_IdentifierSeparator, CollectionIdentifier, AddressableName);
    }

    /// <summary>
    /// Used by <see cref="RealityAssetsController"/>
    /// </summary>
    public class CollectionData
    {
        /// <summary>
        /// Whether the collection has been loaded from StreamingAssets
        /// This dictates that the <see cref="BetterStreamingAssets"/> methods should
        /// be used for loading the thumbnail etc. instead of the default File.Open() methods. 
        /// </summary>
        public bool StreamingAssets;

        public AssetCollectionsLocation Location;

        /// <summary>
        /// Where is the collection (the .zip file) located. 
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// The addressables and data about the asset collection itself, such as the author. 
        /// </summary>
        public SerializedRealityAssetCollection Data { get; private set; }

        /// <summary>
        /// The Asset Bundle that can be used to actually load an object. 
        /// </summary>
        public AssetBundle AssetBundle = null;
        
        public SpriteAtlas SpriteAtlas = null;

        public Dictionary<string, GameObject> LoadedAssets = new();
        public Dictionary<string, Sprite> LoadedThumbnails = new();

        public CollectionData(SerializedRealityAssetCollection data, string filePath, AssetCollectionsLocation location, bool streamingAssets)
        {
            Data = data;
            FilePath = filePath;
            Location = location;
            StreamingAssets = streamingAssets;
        }
    }
}

///Hi <3 Mirna was here \(^.^)/