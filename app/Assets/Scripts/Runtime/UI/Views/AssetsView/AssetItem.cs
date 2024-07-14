//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Cuboid.Utils;
using Cuboid.Input;
using Cuboid.Models;

namespace Cuboid.UI
{
    /// <summary>
    /// Notes:
    ///
    /// The RealityAssetUIElement is selectable and draggable
    /// Its Data (of type RealityAssetData), should be expected to be swapped
    /// at any time.
    ///
    /// It should get whether the asset is selected or dragged from certain controllers
    /// that hold this in a dictionary.
    ///
    /// TODO: Make the instantiation and loading RealityAssetData's GameObjects from asset bundles
    /// two separate processes. So that it can be wrapped -> "GetGameObject", which executes
    /// coroutine if it is not already being run. And the Action<GameObject> returns the gameobject on completion,
    /// or directly if it was already loaded. 
    /// </summary>
    public class AssetItem : MonoBehaviour,
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
        private AssetData _data;
        public AssetData Data
        {
            get => _data;
            set
            {
                CancelThumbnailLoadingCoroutines();

                if (_instantiatedThumbnailGameObject != null)
                {
                    Destroy(_instantiatedThumbnailGameObject);
                    _instantiatedThumbnailGameObject = null;
                }

                _thumbnailTransform.DOKill();
                Hovered = false;
                Pressed = false;
                Dragged = false;
                
                _data = value;

                LoadThumbnailSprite();

                _text.text = _data.AddressableName;

                if (_popupsController != null)
                {
                    OnPopupsCountChanged(_popupsController.PopupsCount.Value);
                }
            }
        }

        private const float k_AnimationDuration = 0.2f;
        private const float k_OnHoverOffset = -0.55f;
        private const float k_OnPressOffset = -0.25f;
        private const float k_PositionOffsetMultiplier = 1.0f;//0.5f; // The amount to which the item moves to the cursor before dragging.
        private const float k_HoverPositionOffsetMultiplier = 0.2f; // 0.1f; // The amount to which the item moves to the cursor before dragging.

        private AssetsViewController _assetsViewController;

        private PopupsController _popupsController;
        private Action<int> _onPopupsCountChanged;

        private Action<HashSet<AssetData>> _onSelectionChanged;
        private Action<HashSet<AssetData>> _onDraggedChanged;

        [SerializeField] private Transform _thumbnailTransform;
        [SerializeField] private Transform _thumbnailImageTransform;
        [SerializeField] private Transform _thumbnailAssetTransform;
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private Image _image;

        [SerializeField] private Image[] _selectedOutlines = new Image[0];

        /// <summary>
        /// The gameobject
        /// </summary>
        private GameObject _instantiatedThumbnailGameObject;

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

        private bool _dragged;
        public bool Dragged
        {
            get => _dragged;
            private set
            {
                _dragged = value;

                _thumbnailTransform.gameObject.SetActive(!_dragged);
            }
        }

        private bool _hovered = false;
        public bool Hovered
        {
            get => _hovered;
            private set
            {
                _hovered = value;

                if (_hovered && _instantiatedThumbnailGameObject == null)
                {
                    LoadThumbnailGameObject();
                }
                if (!Hovered)
                {
                    ResetOffset();
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

        private void Start()
        {
            _popupsController = PopupsController.Instance;

            _onSelectionChanged = OnSelectionChanged;
            _onDraggedChanged = OnDraggedChanged;
            _onPopupsCountChanged = OnPopupsCountChanged;

            Register();
        }

        private void OnPopupsCountChanged(int popupsCount)
        {
            bool show = popupsCount == 0;
            _thumbnailTransform.gameObject.SetActive(show);
        }

        private void UpdateHoverPressedAppearance()
        {
            bool showThumbnailAsset = (Hovered || Pressed) && (_instantiatedThumbnailGameObject != null);

            _thumbnailImageTransform.gameObject.SetActive(!showThumbnailAsset);
            _thumbnailAssetTransform.gameObject.SetActive(showThumbnailAsset);

            float target = 0.0f;
            if (Pressed)
            {
                target = k_OnPressOffset;
            } else if (Hovered)
            {
                target = k_OnHoverOffset;
            }

            _thumbnailTransform.DOLocalMoveZ(target, k_AnimationDuration);
        }

        private void OnDraggedChanged(HashSet<AssetData> draggedRealityAssets)
        {
            Dragged = draggedRealityAssets.Contains(Data);
        }

        private void OnSelectionChanged(HashSet<AssetData> selectedRealityAssets)
        {
            Selected = selectedRealityAssets.Contains(Data);
        }

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

            Vector3 positionOffset = GetPositionOffset(eventData.spatialPosition, _thumbnailTransform, offsetMultiplier);
            _thumbnailTransform.DOLocalMoveX(positionOffset.x, k_AnimationDuration);
            _thumbnailTransform.DOLocalMoveY(positionOffset.y, k_AnimationDuration);
        }

        private void ResetOffset()
        {
            _thumbnailTransform.DOLocalMoveX(0, k_AnimationDuration);
            _thumbnailTransform.DOLocalMoveY(0, k_AnimationDuration);
        }

        private Vector3 GetPositionOffset(Vector3 worldPosition, Transform transform, float multiplier)
        {
            return transform.InverseTransformPoint(worldPosition) * multiplier;
        }

        private TransformData _startTransformData;
        private RealityObjectData _realityObjectData;
        private RealityObject _instantiatedRealityObject;
        private Vector3 _raycastOffset;

        void ISpatialBeginDragHandler.OnSpatialBeginDrag(SpatialPointerEventData eventData)
        {
            //_assetsViewController.BeginDrag(this);

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

                _raycastOffset = - bounds.center * _assetImportScale;
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
            //_assetsViewController.EndDrag(this);

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

        #region Data loading coroutines

        private IEnumerator _loadThumbnailSpriteCoroutine;
        private IEnumerator _loadThumbnailGameObjectCoroutine;

        /// <summary>
        /// Starts LoadThumbnailSprite coroutine if not already started
        /// </summary>
        private void LoadThumbnailSprite()
        {
            if (_loadThumbnailSpriteCoroutine == null)
            {
                _loadThumbnailSpriteCoroutine = LoadThumbnailSpriteCoroutine();
                StartCoroutine(_loadThumbnailSpriteCoroutine);
                //Debug.Log("Started coroutine");
            }
        }

        /// <summary>
        /// Starts LoadThumbnailGameObject coroutine if not already started
        /// </summary>
        private void LoadThumbnailGameObject()
        {
            if (_loadThumbnailGameObjectCoroutine == null)
            {
                _loadThumbnailGameObjectCoroutine = LoadThumbnailGameObjectCoroutine();
                StartCoroutine(_loadThumbnailGameObjectCoroutine);
            }
        }

        /// <summary>
        /// Coroutine for loading thumbnail Sprite
        /// </summary>
        private IEnumerator LoadThumbnailSpriteCoroutine()
        {
            RealityAssetsController.Instance.LoadAssetThumbnail(Data).Execute(Data.Identifier, out CoroutineTask<Sprite> task);
            while (!task.Done) { yield return null; }
            _image.sprite = task.Result;
        }

        /// <summary>
        /// Coroutine for loading thumbnail GameObject,
        /// on loading it 
        /// </summary>
        private IEnumerator LoadThumbnailGameObjectCoroutine()
        {
            RealityAssetsController.Instance.LoadAsset(Data).Execute(Data.Identifier, out CoroutineTask<GameObject> task);
            while (!task.Done) { yield return null; }

            _instantiatedThumbnailGameObject = ObjectFitUtils.InstantiateObjectInsideTransform(task.Result, _thumbnailAssetTransform);

            // Set layer, because we render the UI / SpatialUI / Controllers by a separate camera
            // on the camera stack. Which clears the depth buffer and thus is always overlayed on
            // top of objects in the Default layer.
            //
            // We need to iterate over all children objects that are instantiated because otherwise
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

        private void CancelThumbnailLoadingCoroutines()
        {
            StopAndClearCoroutine(ref _loadThumbnailGameObjectCoroutine);
            StopAndClearCoroutine(ref _loadThumbnailSpriteCoroutine);
        }

        private void StopAndClearCoroutine(ref IEnumerator enumerator)
        {
            if (enumerator == null) { return; }
            StopCoroutine(enumerator);
            enumerator = null;
        }

        #endregion

        #region Action registration

        private void Register()
        {
            if (_popupsController != null)
            {
                _popupsController.PopupsCount.Register(_onPopupsCountChanged);
            }
        }

        private void Unregister()
        {
            if (_popupsController != null)
            {
                _popupsController.PopupsCount.Unregister(_onPopupsCountChanged);
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

            _thumbnailTransform.DOKill();

            // Stop coroutines as well
            CancelThumbnailLoadingCoroutines();
        }

        #endregion
    }
}

