// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using Cuboid.Input;
using Cuboid.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Cuboid.UI;
using DG.Tweening;
using System.Linq;

namespace Cuboid
{
    /// <summary>
    /// Interface that can be added to tools to show that it supports the default select behaviour.
    ///
    /// The DefaultSelectBehaviour instance will be enabled or disabled depending on whether this
    /// interface is implemented in the tool. 
    /// </summary>
    public interface IToolHasDefaultSelectBehaviour
    {
    }
    
    /// <summary>
    /// Tools should be able to enable or disable the default select behaviour.
    ///
    /// Initially this class was added as a component to the active tool, but this would result
    /// in the movement of an object abruptly stopping while moving the object around.
    ///
    /// So, what we want to do is to have tools either enable or disable the functionality of this class.
    /// 
    /// </summary>
    public sealed class DefaultSelectBehaviour : OutsideUIBehaviour
    {
        private static DefaultSelectBehaviour _instance;
        public static DefaultSelectBehaviour Instance => _instance;

        private SelectionController _selectionController;

        private Vector3 _raycastOffset;

        private SelectCommand _selectCommand;

        private RealityObject _pressedRealityObject;
        private Vector3 _pressedWorldPosition;

        private bool _isDragging = false;

        private void Awake()
        {
            // Singleton implementation
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }
        }

        protected override void Start()
        {
            _selectionController = SelectionController.Instance;

            base.Start();
        }

        private static bool GetRealityObject(SpatialPointerEventData eventData, out RealityObject realityObject)
        {
            realityObject = null;
            RaycastResult raycastResult = eventData.outsideUIPressRaycastResult;
            
            if (!raycastResult.isValid || raycastResult.gameObject == null) { return false; }

            realityObject = raycastResult.gameObject.GetComponentInParent<RealityObject>();
            return realityObject != null;
        }

        protected override void OutsideUIPointerDown(SpatialPointerEventData eventData)
        {
            base.OutsideUIPointerDown(eventData);

            // pressed = true
            _selectCommand = null;

            GetRealityObject(eventData, out _pressedRealityObject);

            _canCloseContextMenu = true;
            _pressedWorldPosition = eventData.spatialTargetPosition;
            StartOpenContextMenuCoroutine();
        }

        protected override void OutsideUIPointerUp(SpatialPointerEventData eventData)
        {
            base.OutsideUIPointerUp(eventData);

            StopOpenContextMenuCoroutine();
        }

        protected override void PointerDown(SpatialPointerEventData eventData)
        {
            base.PointerDown(eventData);

            if (!ContextMenuOpen) { return; } // only needs to be closed if currently open

            // only close if the context menu is not a parent
            // traverse up the hierarchy, make sure it's this context menu
            Transform contextMenu = _instantiatedSpatialContextMenu.transform;
            Transform target = eventData.pointerCurrentRaycast.gameObject.transform;

            bool close = true;
            while (target != null)
            {
                if (target == contextMenu)
                {
                    close = false;
                    break;
                }
                target = target.parent;
            }
            if (close)
            {
                CloseContextMenu();
            }
        }

        protected override void OutsideUIPointerClick(SpatialPointerEventData eventData)
        {
            base.OutsideUIPointerClick(eventData);

            if (_pressedRealityObject == null)
            {
                if (ModifiersController.Instance.ShiftModifier.Value)
                {
                    ToggleContextMenu();
                }
                else
                {
                    if (!_selectionController.Selection.Value.ContainsObjects)
                    {
                        ToggleContextMenu();
                    }
                    else
                    {
                        if (ContextMenuOpen)
                        {
                            CloseContextMenu();
                        }
                    }
                    _selectionController.DeselectAll();
                }
                return;
            }

            // if the selection already contains the pressed reality object, open the context menu instantly
            if (InSelection)
            {
                ToggleContextMenu();
            }
            else
            {
                // select
                Select(eventData);
                CloseContextMenu();
            }

            if (_selectCommand != null)
            {
                UndoRedoController.Instance.Add(_selectCommand);
            }
        }

        private bool InSelection => _pressedRealityObject != null && _selectionController.Selection.Value.SelectedRealityObjects.Contains(_pressedRealityObject.RealityObjectData);

        protected override void OutsideUIBeginDrag(SpatialPointerEventData eventData)
        {
            base.OutsideUIBeginDrag(eventData);

            CloseContextMenu();
            StopOpenContextMenuCoroutine();

            if (_pressedRealityObject == null) { return; }

            // select if not in selection, start dragging

            if (!InSelection)
            {
                Select(eventData);
            }

            RecalculateOffset(eventData);

            _isDragging = true;
        }

        protected override void OutsideUIDrag(SpatialPointerEventData eventData)
        {
            base.OutsideUIDrag(eventData);

            if (!_isDragging) { return; }

            if (_pressedRealityObject == null) { return; }

            UpdatePosition(eventData);
        }

        protected override void OutsideUIEndDrag(SpatialPointerEventData eventData)
        {
            base.OutsideUIEndDrag(eventData);

            if (!_isDragging) { return; }

            ApplyTransformations();

            _isDragging = false;
        }

        private void ApplyTransformations()
        {
            Debug.Log($"{name} called ApplyTransformations");

            if (_pressedRealityObject == null) { return; }

            // apply commands
            TransformCommand transformCommand = SelectionController.Instance.GetTransformCommand();

            // selectCommand already performed, so do this before adding to the children
            // otherwise the selectCommand will get fired again. 
            transformCommand.Do();

            if (_selectCommand != null)
            {
                // This means that on drag the object was selected
                // Thus, we should merge the transform and select command
                _selectCommand.AddChild(transformCommand);
                UndoRedoController.Instance.Add(_selectCommand);
            }
            else
            {
                UndoRedoController.Instance.Add(transformCommand);
            }
        }

        /// <summary>
        /// Calculates the new TransformData (only position changes) for the selection, based on where the user points their
        /// controller
        /// </summary>
        /// <returns></returns>
        private TransformData CalculateNewTransformData(Vector3 newPosition)
        {
            TransformData initialTransformData = _selectionController.CurrentSelectionTransformData;
            return initialTransformData.SetPosition(newPosition); // struct, so SetPosition is more like "ReturnWithChangedPosition"
        }

        private void Select(SpatialPointerEventData eventData)
        {
            if (ModifiersController.Instance.ShiftModifier.Value)
            {
                _selectCommand = new SelectCommand(_selectionController,
                    new List<RealityObjectData>() { _pressedRealityObject.RealityObjectData }, SelectCommand.SelectOperation.Toggle);
            }
            else
            {
                // Select only this object
                _selectCommand = new SelectCommand(_selectionController,
                    new List<RealityObjectData>() { _pressedRealityObject.RealityObjectData }, SelectCommand.SelectOperation.SetTo);
            }

            if (_selectCommand != null)
            {
                _selectCommand.Do(); // Make sure to add the command when finished dragging. 
            }

            RecalculateOffset(eventData); // Because the selection has changed
        }

        private void UpdatePosition(SpatialPointerEventData eventData)
        {
            // update position
            Vector3 newPosition = eventData.spatialPosition + _raycastOffset;
            _selectionController.SetCurrentSelectionTransformData(CalculateNewTransformData(newPosition));
            _selectionController.UpdateRealityObjectInstanceTransforms();
        }

        private void RecalculateOffset(SpatialPointerEventData eventData)
        {
            // use the offset from the spatial press position, so that the point at which the
            // user has clicked, the asset gets placed.
            _raycastOffset = _selectionController.CurrentSelectionTransformData.Position -
                eventData.spatialPressPosition;
        }

        #region Context menu

        [SerializeField] private GameObject _spatialContextMenuPrefab;
        private SpatialContextMenu _instantiatedSpatialContextMenu = null;
        private Cuboid.UI.ContextMenu.ContextMenuData _data;

        private IEnumerator _openContextMenuCoroutine = null;
        private const float k_TimeBeforeContextMenuOpens = 1.0f;
        private bool _canCloseContextMenu = true;

        private bool _contextMenuOpen = false;
        public bool ContextMenuOpen
        {
            get => _contextMenuOpen;
            set
            {
                if (_contextMenuOpen == value) { return; }
                _contextMenuOpen = value;

                if (_contextMenuOpen)
                {
                    // open
                    if (_instantiatedSpatialContextMenu == null)
                    {
                        _instantiatedSpatialContextMenu = Instantiate(_spatialContextMenuPrefab, null, false).GetComponent<SpatialContextMenu>();
                        _instantiatedSpatialContextMenu.ContextMenuTransform.localScale = Vector3.zero;
                    }

                    _instantiatedSpatialContextMenu.ContextMenuTransform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack, 1.2f);
                    _instantiatedSpatialContextMenu.transform.position = _pressedWorldPosition;

                    List<UI.Button.Data> pasteButtons = new()
                    {
                        //new Button.ButtonData()
                        //{
                        //    Text = "Paste",
                        //    Icon = Icons.Data.ContentPaste,
                        //    Disabled = !ClipboardController.CanPaste,
                        //    OnPressed = () =>
                        //    {
                        //        if (ClipboardController.CanPaste)
                        //        {
                        //            ClipboardController.Paste(_pressedWorldPosition);
                        //            CloseContextMenu();
                        //        }
                        //    }
                        //},
                        new Button.Data()
                        {
                            Text = "Paste",//Text = "Paste at Original Location",
                            Icon = Icons.Data.ContentPaste,
                            Disabled = !ClipboardController.CanPaste,
                            OnPressed = () =>
                            {
                                if (ClipboardController.CanPaste)
                                {
                                    ClipboardController.Paste();
                                    CloseContextMenu();
                                }
                            }
                        },
                    };

                    UI.Button.Data selectAllButton = new Button.Data()
                    {
                        Text = "Select All",
                        OnPressed = () =>
                        {
                            SelectionController.Instance.SelectAll();
                            CloseContextMenu();
                        }
                    };

                    List<UI.Button.Data> buttons = new();

                    if (_pressedRealityObject)
                    {
                        buttons.AddRange(new List<UI.Button.Data>()
                        {
                            new UI.Button.Data()
                            {
                                Text = "Cut",
                                Icon = Icons.Data.ContentCut,
                                OnPressed = () =>
                                {
                                    ClipboardController.Cut();
                                    CloseContextMenu();
                                }
                            },
                            new UI.Button.Data()
                            {
                                Text = "Copy",
                                Icon = Icons.Data.ContentCopy,
                                OnPressed = () =>
                                {
                                    ClipboardController.Copy();
                                    CloseContextMenu();
                                }
                            },
                        });
                        buttons.AddRange(pasteButtons);
                        buttons.Add(new Button.Data()
                        {
                            Text = "Duplicate",
                            Icon = Icons.Data.ContentDuplicate,
                            OnPressed = () =>
                            {
                                ClipboardController.Duplicate();
                                CloseContextMenu();
                            }
                        });
                        buttons.Add(selectAllButton);
                        buttons.Add(new Button.Data()
                        {
                            Text = "Deselect",
                            OnPressed = () =>
                            {
                                SelectionController.Instance.DeselectAll();
                                CloseContextMenu();
                            }
                        });
                        buttons.Add(new Button.Data()
                        {
                            Text = "Delete",
                            Icon = Icons.Data.Delete,
                            OnPressed = () =>
                            {
                                SelectionController.Instance.DeleteSelection();
                                CloseContextMenu();
                            }
                        });
                    }
                    else
                    {
                        buttons.AddRange(pasteButtons);
                        buttons.Add(selectAllButton);
                    }

                    _instantiatedSpatialContextMenu.ContextMenu.Data = new UI.ContextMenu.ContextMenuData()
                    {
                        Title = _selectionController.Selection.Value.GetString(""),
                        Buttons = buttons
                    };
                }
                else
                {
                    // close
                    _instantiatedSpatialContextMenu.ContextMenuTransform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.OutQuart);
                }
            }
        }

        private IEnumerator OpenContextMenuCoroutine()
        {
            yield return new WaitForSeconds(k_TimeBeforeContextMenuOpens);
            _canCloseContextMenu = false;
            ToggleContextMenu();
        }

        private void CloseContextMenu()
        {
            ContextMenuOpen = false;
        }

        private void ToggleContextMenu()
        {
            if (!ContextMenuOpen)
            {
                ContextMenuOpen = true;
            }
            else if (_canCloseContextMenu)
            {
                ContextMenuOpen = false;
            }
        }

        private void StartOpenContextMenuCoroutine()
        {
            StopOpenContextMenuCoroutine();
            _openContextMenuCoroutine = OpenContextMenuCoroutine();
            StartCoroutine(_openContextMenuCoroutine);
        }

        private void StopOpenContextMenuCoroutine()
        {
            if (_openContextMenuCoroutine != null)
            {
                StopCoroutine(_openContextMenuCoroutine);
                _openContextMenuCoroutine = null;
            }
        }

        protected override void Unregister()
        {
            base.Unregister();

            if (_isDragging)
            {
                ApplyTransformations();
                _isDragging = false;
            }
        }

        #endregion
    }
}
