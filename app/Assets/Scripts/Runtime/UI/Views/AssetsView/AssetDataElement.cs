//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using Cuboid.Input;
using Cuboid.Models;
using Cuboid.UI;
using Cuboid.Utils;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cuboid.UI
{
    public sealed class AssetDataElement : MonoBehaviour, IData<AssetData>,
        ISpatialPointerEnterHandler,
        ISpatialPointerExitHandler,
        ISpatialPointerClickHandler,
        ISpatialPointerDownHandler,
        ISpatialPointerUpHandler,
        ISpatialBeginDragHandler,
        ISpatialDragHandler,
        ISpatialEndDragHandler,
        ISpatialPointerMoveHandler
    {
        private const float k_AnimationDuration = 0.2f;
        private const float k_OnHoverOffset = -0.55f;
        private const float k_OnPressOffset = -0.25f;
        private const float k_PositionOffsetMultiplier = 1.0f;//0.5f; // The amount to which the item moves to the cursor before dragging.
        private const float k_HoverPositionOffsetMultiplier = 0.2f; // 0.1f; // The amount to which the item moves to the cursor before dragging.

        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private Image _image;
        [SerializeField] private GameObject _loadingSpinner;
        [SerializeField] private Transform _thumbnailTransform;
        [SerializeField] private Transform _thumbnailImageTransform;
        [SerializeField] private Transform _thumbnailAssetTransform;
        [SerializeField] private Image[] _selectedOutlines = new Image[0];

        private RealityAssetsController _controller;
        private RealityAssetsController Controller => _controller == null ? _controller = RealityAssetsController.Instance : _controller;

        private IEnumerator _loadThumbnailCoroutine;
        private IEnumerator _loadAssetCoroutine;

        private GameObject _instantiatedThumbnailGameObject;
        private TransformData _startTransformData;
        private RealityObjectData _realityObjectData;
        private RealityObject _instantiatedRealityObject;
        private Vector3 _raycastOffset;

        private bool _selected;
        public bool Selected
        {
            get => _selected;
            private set
            {
                _selected = value;

                foreach (Image image in _selectedOutlines)
                {
                    image.enabled = _selected;
                }

                _text.color = _selected ? Color.white : Color.black;
            }
        }

        private bool _dragging;
        public bool Dragging
        {
            get => _dragging;
            private set
            {
                _dragging = value;
                _thumbnailTransform.gameObject.SetActive(!_dragging);

                if (!_dragging)
                {
                    ResetOffset(animated: false);
                }
            }
        }

        private bool _hovered = false;
        public bool Hovered
        {
            get => _hovered;
            private set
            {
                _hovered = value;

                if (_hovered && _instantiatedThumbnailGameObject == null && _loadAssetCoroutine == null)
                {
                    _loadAssetCoroutine = SetAsset();
                    StartCoroutine(_loadAssetCoroutine);
                }

                if (!_hovered)
                {
                    ResetOffset(animated: true);
                }

                UpdateHoverPressedAppearance();
            }
        }

        private bool _pressed = false;
        public bool Pressed
        {
            get => _pressed;
            private set
            {
                _pressed = value;
                UpdateHoverPressedAppearance();
            }
        }

        private AssetData _data;
        public AssetData Data
        {
            get => _data;
            set
            {
                if (_data == value) { return; }
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
                UpdateAppearance();
            }
        }

        private bool _showAsset;
        public bool ShowAsset
        {
            get => _showAsset;
            set
            {
                _showAsset = value;
                UpdateAppearance();
            }
        }

        private void UpdateAppearance()
        {
            bool showThumbnailAsset = (Hovered || Pressed) && (_instantiatedThumbnailGameObject != null);
            bool showThumbnail = !showThumbnailAsset && !LoadingThumbnail;

            _thumbnailAssetTransform.gameObject.SetActive(showThumbnailAsset);
            _thumbnailImageTransform.gameObject.SetActive(!showThumbnailAsset);
            _loadingSpinner.SetActive(!showThumbnail);
            _image.gameObject.SetActive(showThumbnail);
        }

        private void UpdateHoverPressedAppearance()
        {
            UpdateAppearance();

            float target = 0.0f;
            if (Pressed)
            {
                target = k_OnPressOffset;
            }
            else if (Hovered)
            {
                target = k_OnHoverOffset;
            }

            _thumbnailTransform.DOLocalMoveZ(target, k_AnimationDuration);
        }

        private void OnDataChanged(AssetData data)
        {
            // cancel loading coroutines
            CancelCoroutines();
            if (_instantiatedThumbnailGameObject != null)
            {
                Destroy(_instantiatedThumbnailGameObject);
                _instantiatedThumbnailGameObject = null;
            }

            LoadingThumbnail = true;
            Selected = false;

            if (data == null) { return; }

            _text.text = _data.AddressableName;

            _loadThumbnailCoroutine = SetThumbnail();
            StartCoroutine(_loadThumbnailCoroutine);
        }

        private IEnumerator SetThumbnail()
        {
            yield return Controller.LoadAssetThumbnail(Data).Execute(Data.Identifier, out CoroutineTask<Sprite> task);
            while (!task.Done) { yield return null; } if (task.Failed) { yield break; }

            Sprite result = task.Result;

            // set the thumbnail
            _image.sprite = result;
            LoadingThumbnail = false;
        }

        private IEnumerator SetAsset()
        {
            yield return Controller.LoadAsset(Data).Execute(Data.Identifier, out CoroutineTask<GameObject> task);
            while (!task.Done) { yield return null; } if (task.Failed) { yield break; }

            GameObject result = task.Result;

            // set the asset

            _instantiatedThumbnailGameObject = ObjectFitUtils.InstantiateObjectInsideTransform(task.Result, _thumbnailAssetTransform);

            // Set layer, because we render the UI / SpatialUI / Controllers by a separate camera
            // on the camera stack. Which clears the depth buffer and thus is always overlayed on
            // top of objects in the Default layer.
            //
            // We need to iterate over all children objects that are instantiated
            _instantiatedThumbnailGameObject.SetLayerRecursively(Layers.UI.layer);

            // Disable all colliders
            Collider[] colliders = _instantiatedThumbnailGameObject.GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                collider.enabled = false;
            }

            // Disable all cameras
            Camera[] cameras = _instantiatedThumbnailGameObject.GetComponentsInChildren<Camera>();
            foreach (Camera camera in cameras)
            {
                camera.enabled = false;
            }

            UpdateHoverPressedAppearance();
        }

        private void CancelCoroutines()
        {
            CoroutineUtils.StopAndClearCoroutine(this, ref _loadThumbnailCoroutine);
            CoroutineUtils.StopAndClearCoroutine(this, ref _loadAssetCoroutine);
        }

        private void ResetOffset(bool animated)
        {
            if (animated)
            {
                _thumbnailTransform.DOLocalMoveX(0, k_AnimationDuration);
            }
            else
            {
                _thumbnailTransform.DOKill();
                _thumbnailTransform.localPosition = Vector3.zero;
            }
        }

        private Vector3 GetPositionOffset(Vector3 worldPosition, Transform transform, float multiplier)
        {
            return transform.InverseTransformPoint(worldPosition) * multiplier;
        }

        #region SpatialPointer events

        void ISpatialPointerEnterHandler.OnSpatialPointerEnter(SpatialPointerEventData eventData)
        {
            Hovered = true;
        }

        void ISpatialPointerExitHandler.OnSpatialPointerExit(SpatialPointerEventData eventData)
        {
            Hovered = false;
        }

        void ISpatialPointerClickHandler.OnSpatialPointerClick(SpatialPointerEventData eventData)
        {

        }

        void ISpatialPointerDownHandler.OnSpatialPointerDown(SpatialPointerEventData eventData)
        {
            Pressed = true;
        }

        void ISpatialPointerUpHandler.OnSpatialPointerUp(SpatialPointerEventData eventData)
        {
            Pressed = false;
        }

        void ISpatialPointerMoveHandler.OnSpatialPointerMove(SpatialPointerEventData eventData)
        {
            // With the pooling, when changing the data of the instantiated RealityAssetUIElement,
            // the SpatialInputModule doesn't know this and will still send move events to this element. 
            // 
            // So as a quick fix, we check whether the element is hovered,
            // because when the data get changed, _hovered is set to false. 
            if (!Hovered) { return; }

            // update the x, y position
            float offsetMultiplier = 0.0f;
            if (Hovered)
            {
                offsetMultiplier = k_HoverPositionOffsetMultiplier;
            }
            else
            {
                offsetMultiplier = k_PositionOffsetMultiplier;
            }

            // Because the thumbnail transform's local Y position also gets set by the ScrollViewPool,
            // we only use the X position
            Vector3 positionOffset = GetPositionOffset(eventData.spatialPosition, _thumbnailTransform, offsetMultiplier);
            _thumbnailTransform.DOLocalMoveX(positionOffset.x, k_AnimationDuration);
        }

        void ISpatialBeginDragHandler.OnSpatialBeginDrag(SpatialPointerEventData eventData)
        {
            Dragging = true;

            RealityAssetObjectData newObjectData = new RealityAssetObjectData()
            {
                Guid = Guid.NewGuid(),
                AssetData = Data,
                Name = new(Data.AddressableName),
                Transform = new Binding<TransformData>(),
                Selected = new Binding<bool>(false)
            };

            _realityObjectData = newObjectData;
            RealitySceneController.Instance.Instantiate(_realityObjectData, (onInstantiateResult) =>
            {
                // when the object has instantiated
                _instantiatedRealityObject = onInstantiateResult;
            },
            // as second because it's optional and only required for instantiating
            // objects for dragging outside of the asset library
            (beforeDataBindingResult) =>
            {
                // This, to calculate the bounds and set the transformdata,
                // before it will bind the new transformdata with default (0, 0, 0) values. 
                Bounds bounds = BoundsUtils.GetBounds(beforeDataBindingResult.gameObject);

                float _assetImportScale = App.Instance.ImportScale.Value;

                _raycastOffset = -bounds.center * _assetImportScale;
                _startTransformData = new TransformData(beforeDataBindingResult.transform);
                _startTransformData = _startTransformData.SetScale(_startTransformData.Scale * _assetImportScale);
            });
        }

        void ISpatialDragHandler.OnSpatialDrag(SpatialPointerEventData eventData)
        {
            if (_instantiatedRealityObject != null)
            {
                Vector3 calculatedPosition = eventData.spatialPosition + _raycastOffset;
                TransformData transformData = _startTransformData.SetPosition(calculatedPosition);
                _instantiatedRealityObject.RealityObjectData.Transform.Value = transformData;
            }
        }

        void ISpatialEndDragHandler.OnSpatialEndDrag(SpatialPointerEventData eventData)
        {
            Dragging = false;

            AddCommand addCommand = new AddCommand(
                RealityDocumentController.Instance,
                RealitySceneController.Instance,
                RealitySceneController.Instance.OpenedRealitySceneIndex,
                _realityObjectData
                );

            UndoRedoController.Instance.Execute(addCommand);

            _instantiatedRealityObject = null;
            _realityObjectData = null;
        }

        #endregion

        #region Action registration

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

        #endregion
    }

}
