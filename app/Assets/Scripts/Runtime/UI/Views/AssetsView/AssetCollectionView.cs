//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Cuboid.UI.ScrollViewPool;
using Cuboid.UnityPlugin;

namespace Cuboid.UI
{
    public sealed class AssetCollectionView : MonoBehaviour
    {
        [SerializeField] private GameObject _assetPrefab;
        [SerializeField] private ScrollViewPool _assetsPoolComponent;
        [SerializeField] private ScrollViewPool.GridLayout _layout;
        private ScrollViewPoolInternal<AssetData> _assetsPool;
        private ScrollViewPoolInternal<AssetData> AssetsPool => _assetsPool != null ? _assetsPool : _assetsPool = CreatePool();

        private ScrollViewPoolInternal<AssetData> CreatePool()
        {
            return _assetsPoolComponent.CreatePool<AssetData>(new ScrollViewPoolInternal<AssetData>.Data()
            {
                Layout = _layout,
                Prefab = _assetPrefab,
                Values = null,
                Identifier = null
            });
        }

        private SerializedRealityAssetCollection _data;
        public SerializedRealityAssetCollection Data
        {
            get => _data;
            set
            {
                _data = value;
                OnDataChanged(_data);
            }
        }

        private void OnDataChanged(SerializedRealityAssetCollection collection)
        {
            if (collection == null)
            {
                ClearPool();
                return;
            }

            // first get the collection data
            string identifier = collection.Identifier;
            if (!RealityAssetsController.Instance.Collections.Value.TryGetValue(identifier, out CollectionData collectionData))
            {
                ClearPool();
                return;
            }

            List<AssetData> assets = RealityAssetsController.GetAssetDataList(collectionData);
            AssetsPool.ActiveData.Values = assets;
            AssetsPool.ActiveData.Identifier = identifier;
            AssetsPool.DataChanged();
        }

        private void ClearPool()
        {
            AssetsPool.ActiveData.Values = null;
            AssetsPool.DataChanged();
        }
    }
}
