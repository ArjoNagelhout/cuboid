// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Compression;

namespace Cuboid
{
    public class ThumbnailProvider : MonoBehaviour
    {
        private static ThumbnailProvider _instance;
        public static ThumbnailProvider Instance => _instance;

        private CacheController _cacheController;

        private const string k_ThumbnailCache = "thumbnails";

        private string _thumbnailCachePath;

        private void Awake()
        {
            // Singleton implementation
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }

            _thumbnailCachePath = Path.Combine(Constants.CacheDirectoryPath, k_ThumbnailCache);
            if (!Directory.Exists(_thumbnailCachePath)) { Directory.CreateDirectory(_thumbnailCachePath); }

            Application.lowMemory += OnLowMemory;
        }

        private void Start()
        {
            _cacheController = CacheController.Instance;
            _cacheController.OnClearCache += ClearCache;
        }

        private Dictionary<string, Sprite> _thumbnailCache = new Dictionary<string, Sprite>();

        private string GetCachedThumbnailIdentifier(string path) => path.Replace(Path.DirectorySeparatorChar, '_');

        //private string GetCachePath(string name) => Path.Combine(_thumbnailCachePath, name) + ".png";

        private void OnLowMemory()
        {
            ClearCache();
        }

        public void ClearCache()
        {
            _thumbnailCache.Clear();
            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// Invalidate the cache so that the thumbnail will get reloaded from the source location
        /// </summary>
        /// <param name="path">Either the path to the Asset Collection or RealityDocument</param>
        public void InvalidateThumbnail(string path)
        {
            string name = GetCachedThumbnailIdentifier(path);

            // 1. remove from memory
            if (_thumbnailCache.ContainsKey(name))
            {
                _thumbnailCache.Remove(name);
            }

            //string cachePath = GetCachePath(name);

            // 2. remove from disk
            //if (File.Exists(cachePath))
            //{
            //    File.Delete(cachePath);

            //    _cacheController.RecalculateCacheSizeOnDisk();
            //}
        }

        ///// <summary>
        ///// Loads the thumbnail using the following 3 options:
        ///// 1. From memory
        ///// 2. From disk
        ///// 3. From source (stores in memory and disk)
        ///// </summary>
        public IEnumerator<object> LoadThumbnailAsync(string filePath, bool streamingAssets)
        {
            if ((streamingAssets && !BetterStreamingAssets.FileExists(filePath)) || (!streamingAssets && !File.Exists(filePath)))
            {
                yield return new Exception($"{nameof(LoadThumbnailAsync)} failed to load thumbnail, {filePath} does not exist");
                yield break;
            }

            string cachedThumbnailIdentifier = GetCachedThumbnailIdentifier(filePath);
            int size = UnityPlugin.Constants.k_ThumbnailSize;

            //Debug.Log(cachedThumbnailIdentifier);

            // 1. Load it from memory
            if (_thumbnailCache.TryGetValue(cachedThumbnailIdentifier, out Sprite value))
            {
                if (value != null)
                {
                    yield return value;
                    yield break;
                }
            }

            //// 2. Load it from cache
            //string cachePath = GetCachePath(cachedThumbnailIdentifier);
            //if (File.Exists(cachePath))
            //{
            //    // load from file
            //    Task<byte[]> task = File.ReadAllBytesAsync(cachePath);

            //    while (!task.IsCompleted) { yield return null; }
            //    byte[] bytes = task.Result;
            //    if (bytes == null)
            //    {
            //        yield return new Exception($"failed to load thumbnails from cache path {cachePath}");
            //        yield break;
            //    }

            //    Sprite sprite = ConvertToSpriteAndStoreInMemory(bytes, size, size);

            //    yield return sprite;
            //    yield break;
            //}

            //Debug.Log("load thumbnail");

            // 3. Generate it from source location
            using (Stream fileStream = FileUtils.GetStream(filePath, streamingAssets))
            {
                //Debug.Log(fileStream);
                using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Read, true))
                {
                    ZipArchiveEntry thumbnailEntry = archive.GetEntry(UnityPlugin.Constants.k_ThumbnailEntryName);
                    using (Stream zipStream = thumbnailEntry.Open())
                    {
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            Task task = zipStream.CopyToAsync(memoryStream);

                            while (!task.IsCompleted) { yield return null; }
                            byte[] bytes = memoryStream.ToArray();
                            if (bytes == null)
                            {
                                yield return new Exception($"Failed to load thumbnail from path {filePath}");
                                yield break;
                            }

                            //if (!Directory.Exists(_thumbnailCachePath)) { Directory.CreateDirectory(_thumbnailCachePath); }

                            //Task taskStoreOnDisk = File.WriteAllBytesAsync(cachePath, bytes);
                            //while (!task.IsCompleted) { yield return null; }
                            //_cacheController.RecalculateCacheSizeOnDisk();

                            Sprite sprite = ConvertToSpriteAndStoreInMemory(bytes, size, size);

                            yield return sprite;
                            yield break;
                        }
                    }
                }
            }
        }

        private Sprite ConvertToSpriteAndStoreInMemory(byte[] bytes, int width, int height)
        {
            // create sprite from bytes
            Sprite sprite = PNGToSprite(bytes, width, height);

            // store in memory
            _thumbnailCache.TryAdd(name, sprite);
            return sprite;
        }

        private Sprite PNGToSprite(byte[] bytes, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height);
            texture.LoadImage(bytes);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            return sprite;
        }
    }
}
