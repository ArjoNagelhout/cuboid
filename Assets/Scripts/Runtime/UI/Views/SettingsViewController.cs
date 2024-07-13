using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Cuboid.UI
{
    public class SettingsViewController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _cacheSizeNotice;

        private CacheController _cacheController;
        private Action<long> _onCacheSizeOnDiskChanged;

        private void Start()
        {
            _cacheController = CacheController.Instance;
            _onCacheSizeOnDiskChanged = OnCacheSizeOnDiskChanged;

            Register();
        }

        private void OnCacheSizeOnDiskChanged(long bytes)
        {
            bool hasValue = bytes != -1;

            string bytesString = hasValue ? Utils.Utils.BytesToString(bytes, 2) : "-";

            // update the cache size notice
            _cacheSizeNotice.SetText($"Cache size on disk: {bytesString}");
        }

        public void ClearCache()
        {
            _cacheController.ClearCache();
        }

        #region Action registration

        private void Register()
        {
            if (_cacheController != null)
            {
                _cacheController.CacheSizeOnDiskInBytes.Register(_onCacheSizeOnDiskChanged);
            }
        }

        private void Unregister()
        {
            if (_cacheController != null)
            {
                _cacheController.CacheSizeOnDiskInBytes.Unregister(_onCacheSizeOnDiskChanged);
            }
        }

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void OnDestroy()
        {
            Unregister();
        }

        #endregion
    }
}
