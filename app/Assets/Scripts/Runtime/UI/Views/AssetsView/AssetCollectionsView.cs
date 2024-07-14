// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using Cuboid.UnityPlugin;
using UnityEngine;
using static Cuboid.UI.ScrollViewPool;

namespace Cuboid.UI
{
    public sealed class AssetCollectionsView : MonoBehaviour, ILocationView, INavigationStackView
    {
        private static AssetCollectionsView _instance;
        public static AssetCollectionsView Instance => _instance;

        private AssetsViewController _assetsViewController;
        private AssetsViewController AssetsViewController => _assetsViewController == null ? _assetsViewController = AssetsViewController.Instance : _assetsViewController;

        private RealityAssetsController _realityAssetsController;
        private RealityAssetsController RealityAssetsController => _realityAssetsController == null ? _realityAssetsController = RealityAssetsController.Instance : _realityAssetsController;

        private Action<Dictionary<string, CollectionData>> _onCollectionsChanged;

        // Scroll view pool
        [SerializeField] private GameObject _collectionItemPrefab;
        [SerializeField] private ScrollViewPool _assetCollectionsPoolComponent;
        [SerializeField] private ScrollViewPool.ListLayout _assetCollectionsLayout;
        private ScrollViewPoolInternal<SerializedRealityAssetCollection> _assetCollectionsPool;

        private string _identifier;
        private StoredBinding<string> _loadedCollectionIdentifier;

        private AssetCollectionsLocation _location = AssetCollectionsLocation.Local;

        private void Awake()
        {
            // Singleton implementation
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }
        }

        private void Start()
        {
            _onCollectionsChanged = OnCollectionsChanged;

            // Asset Collections Pool
            _assetCollectionsPool = _assetCollectionsPoolComponent.CreatePool<SerializedRealityAssetCollection>(new ScrollViewPoolInternal<SerializedRealityAssetCollection>.Data()
            {
                Layout = _assetCollectionsLayout,
                Prefab = _collectionItemPrefab,
                Values = null
            });

            SetIdentifier();

            Register();
        }

        private void OnCollectionsChanged(Dictionary<string, CollectionData> collections)
        {
            if (_assetCollectionsPool == null) { return; }

            List<SerializedRealityAssetCollection> c = new();
            foreach (KeyValuePair<string, CollectionData> collection in collections)
            {
                if (collection.Value.Location == _location)
                {
                    c.Add(collection.Value.Data);
                }
            }

            _assetCollectionsPool.ActiveData.Values = c;
            _assetCollectionsPool.ActiveData.Identifier = _identifier;
            _assetCollectionsPool.DataChanged();
        }

        public void OpenCollection(SerializedRealityAssetCollection collection, bool animated = true)
        {
            AssetCollectionView view = AssetsViewController.NavigationStack.OpenView<AssetCollectionView>(AssetsViewController.AssetCollectionViewPrefab, collection.Name, animated);
            view.Data = collection;
            _loadedCollectionIdentifier.Value = collection.Identifier;
        }

        private void SetIdentifier()
        {
            _identifier = string.Join('-', nameof(AssetCollectionsView), nameof(_loadedCollectionIdentifier), _location.ToString());
            _loadedCollectionIdentifier = new(_identifier, null);
        }

        void ILocationView.LoadState()
        {
            string storedIdentifier = _loadedCollectionIdentifier.Value;
            if (storedIdentifier == null) { return; }

            // get the serialized collection from the stored identifier
            RealityAssetsController.Collections.Value.TryGetValue(storedIdentifier, out CollectionData data);
            if (data == null) { return; }
            SerializedRealityAssetCollection collection = data.Data;
            if (collection == null) { return; }

            OpenCollection(collection, false);
        }

        void ILocationView.SetData<T>(T value)
        {
            if (value is AssetCollectionsLocation)
            {
                _location = (AssetCollectionsLocation)Convert.ChangeType(value, typeof(AssetCollectionsLocation));
                SetIdentifier();
                OnCollectionsChanged(RealityAssetsController.Collections.Value);
            }
        }

        void INavigationStackView.OnDisable()
        {
        }

        void INavigationStackView.OnEnable()
        {
        }

        void INavigationStackView.OnClose()
        {
        }

        void INavigationStackView.OnWillEnable()
        {
            if (_loadedCollectionIdentifier != null)
            {
                _loadedCollectionIdentifier.Value = null;
            }
        }

        #region Action registration

        private void Register()
        {
            if (RealityAssetsController != null)
            {
                RealityAssetsController.Collections.Register(_onCollectionsChanged);
            }
        }

        private void Unregister()
        {
            if (_realityAssetsController != null)
            {
                RealityAssetsController.Collections.Unregister(_onCollectionsChanged);
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
