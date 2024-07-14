// Copyright (c) 2023 Arjo Nagelhout

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Reflection;
using UnityEngine.UIElements;
using static Cuboid.UI.AssetLocationsView;
using static UnityEngine.Rendering.DebugUI;

namespace Cuboid
{
    /// <summary>
    /// Allows you to get which location has been selected when instantiating the view. 
    /// </summary>
    public interface ILocationView
    {
        public void LoadState();

        public void SetData<T>(T value);
    }

    /// <summary>
    /// Asset Collections are custom built for the ShapeReality app
    /// using Unity. 
    /// </summary>
    [PrettyTypeName(Name = "Asset Collections")]
    public enum AssetCollectionsLocation
    {
        /// <summary>
        /// Asset Collections located in the Assets/ folder in the persistentDataPath
        /// </summary>
        [EnumData(Icon = "Widget", Name = "Local")]
        Local,

        /// <summary>
        /// TODO: Asset Collections located in the Resources folder that are shipped with the binary
        /// </summary>
        [EnumData(Icon = "Widget", Name = "Built In")]
        BuiltIn,

        // TODO: Potentially add some high quality asset collections that get distributed via a CDN
    }

    /// <summary>
    /// These are generic file storage containers from which you can retrieve
    /// any document, such as images or .obj, .fbx files.
    /// </summary>
    [PrettyTypeName(Name = "Files")]
    public enum FileLocation
    {
        /// <summary>
        /// TODO: Located in Files/ in the persistentDataPath
        /// </summary>
        [EnumData(Icon = "Folder", Name = "Local")]
        Local,

        /// <summary>
        /// TODO
        /// </summary>
        [EnumData(Icon = "Cloud", Name = "Google Drive")]
        GoogleDrive,

        /// <summary>
        /// TODO
        /// </summary>
        [EnumData(Icon = "Cloud", Name = "DropBox")]
        DropBox
    }

    /// <summary>
    /// These are services that provide access to 3d content via their
    /// </summary>
    [PrettyTypeName(Name = "Third Party Providers")]
    public enum ThirdPartyAssetProvider
    {
        /// <summary>
        /// TODO: 
        /// </summary>
        [EnumData(Icon = "Extension", Name = "SketchFab")]
        SketchFab
    }
}

namespace Cuboid.UI
{
    public class AssetLocationsView : MonoBehaviour, INavigationStackView
    {
        private AssetsViewController _controller;
        private AssetsViewController Controller => _controller == null ? _controller = AssetsViewController.Instance : _controller;

        [SerializeField] private GameObject _headerPrefab;
        [SerializeField] private Transform _contentTransform;

        [SerializeField] private Cuboid.Utils.SerializableDictionary<AssetCollectionsLocation, GameObject> _assetCollectionsLocationsPrefabs;
        [SerializeField] private Cuboid.Utils.SerializableDictionary<FileLocation, GameObject> _fileLocationsPrefabs;
        [SerializeField] private Cuboid.Utils.SerializableDictionary<ThirdPartyAssetProvider, GameObject> _thirdPartyLocationsPrefabs;

        private void Awake()
        {
            _storedState = new(string.Join('_', nameof(AssetLocationsView), nameof(_storedState)), new State());
        }

        private void Start()
        {
            InstantiateLocationGroup<AssetCollectionsLocation>();
            //InstantiateLocationGroup<FileLocation>();
            //InstantiateLocationGroup<ThirdPartyAssetProvider>();
        }

        [System.Serializable]
        private class State
        {
            public string OpenedLocationTypeName;
            public string OpenedLocationValue;

            public void SetData<T>(T value)
            {
                OpenedLocationTypeName = value.GetType().FullName;
                OpenedLocationValue = value.ToString();
            }

            public void ClearData()
            {
                OpenedLocationTypeName = null;
                OpenedLocationValue = null;
            }
        }

        private StoredBinding<State> _storedState;

        /// <summary>
        /// Should be called by <see cref="AssetsViewController"/> to load the previous state of the locations view. 
        /// </summary>
        public void LoadState()
        {
            State storedState = _storedState.Value;
            string typeName = storedState.OpenedLocationTypeName;
            if (typeName == null) { return; }
            Type t = Type.GetType(typeName);
            if (t == null) { return; }

            MethodInfo method = typeof(AssetLocationsView).GetMethod(nameof(LoadStateInternal), BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null) { return; }
            MethodInfo loadStateInternal = method.MakeGenericMethod(t);
            if (loadStateInternal == null) { return; }
            if (!Enum.TryParse(t, storedState.OpenedLocationValue, false, out object result)) { return; }
            loadStateInternal.Invoke(this, new object[] { result });
        }

        private void LoadStateInternalAOT()
        {
            LoadStateInternal<AssetCollectionsLocation>(default);
            LoadStateInternal<FileLocation>(default);
            LoadStateInternal<ThirdPartyAssetProvider>(default);
            throw new Exception("For IL2CPP AOT code generation. Don't call this method directly");
        }

        private void LoadStateInternal<T>(T value)
        {
            EnumUtils.GetEnumData(value, out string text, out Sprite icon);
            SelectLocation(value, text, false);
        }

        private GameObject GetPrefab<T>(T value)
        {
            GameObject prefab = null;
            switch (value)
            {
                case AssetCollectionsLocation assetCollectionsLocation:
                    _assetCollectionsLocationsPrefabs.TryGetValue(assetCollectionsLocation, out prefab);
                    break;
                case FileLocation fileLocation:
                    _fileLocationsPrefabs.TryGetValue(fileLocation, out prefab);
                    break;
                case ThirdPartyAssetProvider thirdPartyAssetProvider:
                    _thirdPartyLocationsPrefabs.TryGetValue(thirdPartyAssetProvider, out prefab);
                    break;
                default:
                    break;
            }

            return prefab;
        }

        private void SelectLocation<T>(T value, string title, bool animate = true)
        {
            _storedState.Value.SetData(value);
            _storedState.ValueChanged();

            GameObject prefab = GetPrefab(value);
            if (prefab == null) { return; }

            ILocationView view = null;
            view = Controller.NavigationStack.OpenView<ILocationView>(prefab, title, animate, () =>
            {
                view.LoadState();
            });
            view.SetData<T>(value);
        }

        private string GetName(Type t)
        {
            PrettyTypeNameAttribute prettyName = t.GetCustomAttribute<PrettyTypeNameAttribute>();
            return prettyName != null ? prettyName.Name : t.Name;
        }

        private void InstantiateLocationGroup<T>()
        {
            Type t = typeof(T);

            GameObject header = Instantiate(_headerPrefab, _contentTransform, false);
            TextMeshProUGUI headerText = header.GetComponent<TextMeshProUGUI>();
            headerText.text = GetName(t);

            // then instantiate all cases
            Array values = Enum.GetValues(t);
            foreach (T value in values)
            {
                EnumUtils.GetEnumData(value, out string text, out Sprite icon);
                GameObject associatedPrefab = GetPrefab(value);
                Button button = UIController.Instance.InstantiateButton(_contentTransform, new Button.Data()
                {
                    Text = text,
                    Icon = icon,
                    Disabled = associatedPrefab == null,    
                    OnPressed = () =>
                    {
                        SelectLocation<T>(value, text);
                    }
                });
            }
        }

        void INavigationStackView.OnWillEnable()
        {
            _storedState.Value.ClearData();
            _storedState.ValueChanged();
        }

        void INavigationStackView.OnDisable() { }
        void INavigationStackView.OnEnable() { }
        void INavigationStackView.OnClose() { }
    }
}
