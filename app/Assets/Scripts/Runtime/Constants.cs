// 
// Constants.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 

using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid
{
    public static class Constants
    {
        // Raycast sort order for the SpatialGraphicRaycaster and SpatialPhysicsRaycaster
        // higher priority means that it is 
        public const int k_UISortOrderPriority = 50;
        public const int k_SpatialUISortOrderPriority = 40;
        public const int k_RealityObjectsSortOrderPriority = 0;

        public const string k_TagUndoButton = "UndoButton";

        public const string k_RealityAssetCollectionPostFix = "_AssetCollection";
        public const string k_LocalRealityAssetsDirectoryName = "Assets";
        public const string k_RealityAssetCollectionOnDiskFileName = "AssetCollection.json";
        public const string k_RealityDocumentsDirectoryName = "Documents";
        public const string k_RealityDocumentsTrashDirectoryName = "Trash";

        public const string k_BuiltInRealityAssetsDirectoryName = "Assets";

        public const string k_SpriteAtlasAddressableName = "SpriteAtlas";

        public const string k_CacheDirectoryName = ".cache";

        public static string LocalAssetsDirectoryPath => Path.Combine(Application.persistentDataPath, k_LocalRealityAssetsDirectoryName);
        public static string DocumentsDirectoryPath => Path.Combine(Application.persistentDataPath, k_RealityDocumentsDirectoryName);
        public static string TrashDirectoryPath => Path.Combine(Application.persistentDataPath, k_RealityDocumentsTrashDirectoryName);
        public static string CacheDirectoryPath => Path.Combine(Application.persistentDataPath, k_CacheDirectoryName);
    }
}