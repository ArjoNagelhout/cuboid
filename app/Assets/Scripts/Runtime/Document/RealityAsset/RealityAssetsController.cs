// 
// RealityAssetsController.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cuboid.UnityPlugin;
using System.IO.Compression;
using Newtonsoft.Json;
using System.Text;
using UnityEngine.U2D;
using System.Linq;

namespace Cuboid
{
    /// <summary>
    /// Responsible for loading and unloading Reality Asset Collections into memory and out of memory.
    /// </summary>
    public sealed class RealityAssetsController : MonoBehaviour
    {
        private static RealityAssetsController _instance;
        public static RealityAssetsController Instance => _instance;

        /// <summary>
        /// Dictionary with the identifier of the asset collection.
        /// </summary>
        [NonSerialized] public Binding<Dictionary<string, CollectionData>> Collections = new(new Dictionary<string, CollectionData>());

        /// <summary>
        /// Internal function for loading an asset bundle from a file path
        /// </summary>
        private IEnumerator<object> LoadAssetBundleInternal(string filePath, string bundleName, bool streamingAssets)
        {
            AssetBundleCreateRequest request = null;

            // Get the asset bundle from zip. 
            using (Stream fileStream = FileUtils.GetStream(filePath, streamingAssets))
            {
                using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Read, true))
                {
                    string entryName = Path.Combine(UnityPlugin.Constants.k_AssetCollectionAssetBundleName, bundleName);
                    ZipArchiveEntry collectionEntry = archive.GetEntry(entryName);
                    if (collectionEntry == null)
                    {
                        yield return new Exception($"Collection at {filePath} does not contain entry for {entryName}");
                        yield break;
                    }
                    using (Stream zipStream = collectionEntry.Open())
                    {
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            zipStream.CopyTo(memoryStream);
                            request = AssetBundle.LoadFromStreamAsync(memoryStream);
                            while (!request.isDone) { yield return null; }
                        }
                    }
                }
            }

            // check if the asset bundle exists
            AssetBundle assetBundle = request.assetBundle;
            if (assetBundle == null)
            {
                yield return new Exception($"Failed to load {nameof(AssetBundle)} from path {filePath} with bundle name {bundleName}");
                yield break;
            }

            yield return assetBundle;
        }

        /// <summary>
        /// Load <see cref="AssetBundle"/> asynchronously from disk using the given identifier
        /// </summary>
        private IEnumerator<object> LoadAssetBundle(string collectionIdentifier)
        {
            if (!Collections.Value.TryGetValue(collectionIdentifier, out CollectionData collection))
            {
                yield return new Exception($"Collection with identifier {collectionIdentifier} does not exist");
                yield break;
            }

            // if the AssetBundle is already loaded, return that
            if (collection.AssetBundle != null) { yield return collection.AssetBundle; yield break; }

            string assetBundleName = collection.Data.Identifier;

            LoadAssetBundleInternal(collection.FilePath, assetBundleName, collection.StreamingAssets)
                .Execute<AssetBundle>(string.Join('-', collection.FilePath, collection.StreamingAssets), out CoroutineTask<AssetBundle> task);
            while (!task.Done) { yield return null; } if (task.Failed) { yield break; }

            AssetBundle assetBundle = task.Result;

            if (assetBundle == null)
            {
                yield return new Exception($"Could not load asset bundle from collection with identifier {collectionIdentifier}");
                yield break;
            }

            collection.AssetBundle = assetBundle;

            yield return collection.AssetBundle;
        }

        /// <summary>
        /// Load Asset asynchronously from Collection, first loads the asset bundle asynchronously. 
        /// </summary>
        public IEnumerator<object> LoadAsset(AssetData data)
        {
            string identifier = data.CollectionIdentifier;

            LoadAssetBundle(identifier).Execute(identifier, out CoroutineTask<AssetBundle> task);
            while (!task.Done) { yield return null; } if (task.Failed) { yield break; }

            CollectionData collection = Collections.Value[identifier];
            AssetBundle assetBundle = collection.AssetBundle;

            // If the asset is already loaded, return
            if (collection.LoadedAssets.TryGetValue(data.AddressableName, out GameObject value))
            {
                if (value != null) { yield return value; yield break; }
            }

            AssetBundleRequest request = assetBundle.LoadAssetAsync<GameObject>(data.AddressableName);
            while (!request.isDone) { yield return null; }

            GameObject asset = request.asset as GameObject;
            if (asset == null)
            {
                yield return new Exception($"Failed to load {nameof(GameObject)} with name {data.AddressableName} from {collection.Data.Name}");
                yield break;
            }

#if UNITY_EDITOR

            // Fix the materials
            // https://stackoverflow.com/questions/70741369/unity-loadassetbundles-changes-material-into-pink-urp
            // Because, they show up pink due to not matching materials

            AssetBundleEditorUtil.FixShadersForEditor(asset);
#endif
            collection.LoadedAssets[data.AddressableName] = asset;
            yield return asset;
        }

        /// <summary>
        /// Loads SpriteAtlas asynchronously, first loads the asset bundle asynchronously
        /// </summary>
        private IEnumerator<object> LoadSpriteAtlas(string collectionIdentifier)
        {
            LoadAssetBundle(collectionIdentifier).Execute(collectionIdentifier, out CoroutineTask<AssetBundle> task);
            while (!task.Done) { yield return null; } if (task.Failed) { yield break; }

            CollectionData collection = Collections.Value[collectionIdentifier];

            // If SpriteAtlas is already loaded, return
            if (collection.SpriteAtlas != null) { yield return collection.SpriteAtlas; yield break; }

            AssetBundleRequest request = task.Result.LoadAssetAsync<SpriteAtlas>(UnityPlugin.Constants.k_AssetCollectionSpriteAtlasName);
            while (!request.isDone) { yield return null; }

            SpriteAtlas spriteAtlas = request.asset as SpriteAtlas;
            if (spriteAtlas == null)
            {
                yield return new Exception($"Failed to load {nameof(SpriteAtlas)} from collection {collection.Data.Name}");
                yield break;
            }

            collection.SpriteAtlas = spriteAtlas;
            yield return spriteAtlas;
        }

        /// <summary>
        /// Loads the thumbnail for an asset in a collection asynchronously,
        /// first loads asset bundle, then sprite atlas, then thumbnail
        /// </summary>
        public IEnumerator<object> LoadAssetThumbnail(AssetData assetData)
        {
            string identifier = assetData.CollectionIdentifier;
            LoadSpriteAtlas(identifier).Execute(identifier, out CoroutineTask<SpriteAtlas> task);
            while (!task.Done) { yield return null; } if (task.Failed) { yield break; }

            CollectionData collection = Collections.Value[identifier];

            // if thumbnail is already loaded, return
            if (collection.LoadedThumbnails.TryGetValue(assetData.AddressableName, out Sprite value))
            {
                if (value != null) { yield return value; yield break; }
            }

            Sprite thumbnail = task.Result.GetSprite(assetData.AddressableName);

            if (thumbnail == null)
            {
                yield return new Exception($"Could not get Thumbnail for object with name {assetData.AddressableName} from collection {collection.Data.Name}"); yield break;
            }

            collection.LoadedThumbnails[assetData.AddressableName] = thumbnail;
            yield return thumbnail;
        }

        /// <summary>
        /// Instantiates an Asset and creates a <see cref="RealityAsset"/> GameObject in the Scene. 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="completed"></param>
        /// <returns></returns>
        public IEnumerator InstantiateAsset(AssetData data, Action<RealityObject> completed)
        {
            string identifier = data.CollectionIdentifier;
            string assetLoadIdentifier = string.Join(UnityPlugin.Constants.k_IdentifierSeparator, data.CollectionIdentifier, data.AddressableName);

            LoadAsset(data).Execute(assetLoadIdentifier, out CoroutineTask<GameObject> task);
            while (!task.Done) { yield return null; } if (task.Failed) { yield break; }

            GameObject asset = task.Result;

            GameObject instance = Instantiate(asset);
            RealityAsset realityAsset = instance.AddComponent<RealityAsset>();
            completed?.Invoke(realityAsset);
        }

        private void Awake()
        {
            // Singleton implementation
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }

            IEnumerable<AssetBundle> bundles = AssetBundle.GetAllLoadedAssetBundles();
            foreach (AssetBundle bundle in bundles)
            {
                Debug.Log(bundle.name);
            }
            AssetBundle.UnloadAllAssetBundles(true);
            ReloadAssetCollections();
        }

        private void Start()
        {
        }

        private void OnApplicationQuit()
        {
        }

        public void ReloadAssetCollections()
        {
            Collections.Value.Clear();
            AddAssetCollectionsFromDirectory(Constants.LocalAssetsDirectoryPath, AssetCollectionsLocation.Local);
            AddAssetCollectionsFromStreamingAssetsDirectory(Constants.k_BuiltInRealityAssetsDirectoryName, AssetCollectionsLocation.BuiltIn);
            Collections.ValueChanged();
        }

        public static List<AssetData> GetAssetDataList(CollectionData collectionData)
        {
            List<AssetData> assetDataList = new List<AssetData>();
            string collectionIdentifier = collectionData.Data.Identifier;
            foreach (string addressableName in collectionData.Data.AddressableNames)
            {
                assetDataList.Add(new AssetData()
                {
                    AddressableName = addressableName,
                    CollectionIdentifier = collectionIdentifier
                });
            }
            return assetDataList;
        }

        private void AddAssetCollectionsFromStreamingAssetsDirectory(string directoryPath, AssetCollectionsLocation location)
        {
            string[] paths = BetterStreamingAssets.GetFiles(directoryPath, "*"+UnityPlugin.Constants.k_AssetCollectionFileExtension, SearchOption.AllDirectories);
            //Debug.Log(paths);
            //Debug.Log(paths.Length);

            foreach (string filePath in paths)
            {
                byte[] jsonBytes = null;

                using (Stream fileStream = BetterStreamingAssets.OpenRead(filePath))
                {
                    using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Read, true))
                    {
                        ZipArchiveEntry collectionEntry = archive.GetEntry(UnityPlugin.Constants.k_AssetCollectionEntryName);
                        if (collectionEntry == null) { OnInvalid(filePath); continue; }
                        using (Stream zipStream = collectionEntry.Open())
                        {
                            using (MemoryStream memoryStream = new MemoryStream())
                            {
                                zipStream.CopyTo(memoryStream);
                                jsonBytes = memoryStream.ToArray();
                            }
                        }
                    }
                }

                LoadCollection(jsonBytes, filePath, location, true);
            }
        }

        private void AddAssetCollectionsFromDirectory(string directoryPath, AssetCollectionsLocation location)
        {
            // loop over all .zip files, see if they contain a file called collection.json
            foreach (string filePath in Directory.EnumerateFiles(directoryPath, "*"+UnityPlugin.Constants.k_AssetCollectionFileExtension, SearchOption.AllDirectories))
            {
                byte[] jsonBytes = null;

                // try to read the collection.json from the zip
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Read, true))
                    {
                        ZipArchiveEntry collectionEntry = archive.GetEntry(UnityPlugin.Constants.k_AssetCollectionEntryName);
                        if (collectionEntry == null) { OnInvalid(filePath); continue; }
                        using (Stream zipStream = collectionEntry.Open())
                        {
                            using (MemoryStream memoryStream = new MemoryStream())
                            {
                                zipStream.CopyTo(memoryStream);
                                jsonBytes = memoryStream.ToArray();
                            }
                        }
                    }
                }

                LoadCollection(jsonBytes, filePath, location, false);
            }
        }

        private void LoadCollection(byte[] jsonBytes, string filePath, AssetCollectionsLocation location, bool streamingAssets)
        {
            if (jsonBytes == null)
            {
                OnInvalid(filePath); return;
            }

            string json = Encoding.UTF8.GetString(jsonBytes);

            UnityPlugin.SerializedRealityAssetCollection collection =
                JsonConvert.DeserializeObject<UnityPlugin.SerializedRealityAssetCollection>(
                    json, UnityPlugin.SerializationSettings.RealityAssetCollectionJsonSerializationSettings);

            if (collection == null) { return; }

            CollectionData data = new CollectionData(collection, filePath, location, streamingAssets);
            string identifier = data.Data.Identifier;

            // compare which one is released later, and use that one
            if (Collections.Value.ContainsKey(identifier))
            {
                if (collection.CreationDate > Collections.Value[identifier].Data.CreationDate)
                {
                    Collections.Value.Remove(identifier);
                }
                else
                {
                    return;
                }
            }

            Collections.Value.TryAdd(identifier, data);
        }

        private void OnInvalid(string filePath)
        {
            Debug.Log($"{nameof(RealityAssetCollection)} at path {filePath} does not contain a valid {nameof(UnityPlugin.Constants.k_AssetCollectionEntryName)} entry");
        }
    }
}

