//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cuboid.UnityPlugin;

namespace Cuboid.UI
{
    public class AssetCollectionItem : MonoBehaviour, IData<SerializedRealityAssetCollection>
    {
        private AssetCollectionsView _controller;
        private AssetCollectionsView Controller => _controller == null ? _controller = AssetCollectionsView.Instance : _controller;

        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _metadata;
        [SerializeField] private Image _thumbnail;
        [SerializeField] private GameObject _loadingSpinner;

        private IEnumerator _loadThumbnailCoroutine;

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

        private bool _loadingThumbnail;
        public bool LoadingThumbnail
        {
            get => _loadingThumbnail;
            private set
            {
                _loadingThumbnail = value;
                _loadingSpinner.SetActive(_loadingThumbnail);
                _thumbnail.gameObject.SetActive(!_loadingThumbnail);
            }
        }

        private void OnDataChanged(SerializedRealityAssetCollection collection)
        {
            CancelCoroutines();
            LoadingThumbnail = true;

            if (collection == null) { return; }

            _title.text = _data.Name;
            _metadata.text = $"{_data.Author}, {_data.CreationDate.ToLongDateString()}";

            _loadThumbnailCoroutine = SetThumbnail();
            StartCoroutine(_loadThumbnailCoroutine);
        }

        private IEnumerator SetThumbnail()
        {
            string identifier = Data.Identifier;
            if (!RealityAssetsController.Instance.Collections.Value.TryGetValue(identifier, out CollectionData data))
            {
                yield break;
            }

            yield return ThumbnailProvider.Instance.LoadThumbnailAsync(data.FilePath, data.StreamingAssets)
                .Execute(string.Join('-', data.FilePath, data.StreamingAssets), out CoroutineTask<Sprite> task);
            while (!task.Done) { yield return null; } if (task.Failed) { yield break; }

            Sprite thumbnail = task.Result;

            // set the thumbnail
            _thumbnail.sprite = thumbnail;
            LoadingThumbnail = false;
        }

        public void OnPress()
        {
            Controller.OpenCollection(Data);
        }

        private void CancelCoroutines()
        {
            CoroutineUtils.StopAndClearCoroutine(this, ref _loadThumbnailCoroutine);
        }

        private void OnEnable()
        {
            OnDataChanged(Data);
        }

        private void OnDisable()
        {
            CancelCoroutines();
        }

        private void OnDestroy()
        {
            CancelCoroutines();
        }
    }
}

