// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cuboid.UnityPlugin;

namespace Cuboid.UI
{
    /// <summary>
    /// ViewController for the Assets Panel
    /// </summary>
    public sealed class AssetsViewController : MonoBehaviour
    {
        // Singleton implementation
        private static AssetsViewController _instance;
        public static AssetsViewController Instance => _instance;

        // Controllers
        private RealityAssetsController _realityAssetsController;

        // Elements
        [SerializeField] private Button _refreshButton;

        public NavigationStack NavigationStack;
        public GameObject AssetLocationsViewPrefab;
        public GameObject AssetCollectionsViewPrefab;
        public GameObject AssetCollectionViewPrefab;

        private void Awake()
        {
            // Singleton implementation
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }
        }

        private void Start()
        {
            _realityAssetsController = RealityAssetsController.Instance;

            AssetLocationsView assetLocationsView = null;
            assetLocationsView = NavigationStack.OpenView<AssetLocationsView>(AssetLocationsViewPrefab, "Locations", false, () =>
            {
                assetLocationsView.LoadState();
            });
        }

        public void Refresh()
        {
            _realityAssetsController.ReloadAssetCollections();
        }
    }
}
