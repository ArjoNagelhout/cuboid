using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Cuboid.UI;
using Cuboid.Utils;

namespace Cuboid
{
    /// <summary>
    /// Responsible for calculating the cache size on disk and propagating clearing the cache
    /// to classes that have a cache when the app is low on memory via the <see cref="OnClearCache"/> action.
    /// </summary>
    public class CacheController : MonoBehaviour
    {
        private static CacheController _instance;
        public static CacheController Instance => _instance;

        public Action OnClearCache;

        private void Awake()
        {
            // Singleton implemention
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }
        }

        private void Start()
        {
            RecalculateCacheSizeOnDisk();
        }

        [System.NonSerialized]
        public Binding<long> CacheSizeOnDiskInBytes = new Binding<long>(-1);

        public void RecalculateCacheSizeOnDisk()
        {
            Task<long> task = CalculateCacheSizeOnDiskAsync();
            task.ContinueWithOnMainThread((t) =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    CacheSizeOnDiskInBytes.Value = t.Result;
                }
                else
                {
                    CacheSizeOnDiskInBytes.Value = -1;
                }
            });
        }

        private async Task<long> CalculateCacheSizeOnDiskAsync()
        {
            string cachePath = Constants.CacheDirectoryPath;
            DirectoryInfo cacheDirectoryInfo = new DirectoryInfo(cachePath);

            long sizeInBytes = await Task.Run(() => cacheDirectoryInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length));
            return sizeInBytes;
        }

        public void ClearCache()
        {
            OnClearCache?.Invoke();

            string cachePath = Constants.CacheDirectoryPath;
            long sizeBeforeCleaning = CacheSizeOnDiskInBytes.Value;

            if (Directory.Exists(cachePath))
            {
                Directory.Delete(cachePath, recursive: true);
                Directory.CreateDirectory(cachePath);
                RecalculateCacheSizeOnDisk();

                NotificationsController.Instance.OpenNotification(new Notification.Data()
                {
                    Title = "Cleared cache on disk",
                    Description = "Cleared " + Utils.Utils.BytesToString(sizeBeforeCleaning),
                    Icon = Icons.Data.Info,
                    IconColor = Color.green,
                    DisplayDurationInSeconds = 2f
                });
            }
        }

        
    }
}
