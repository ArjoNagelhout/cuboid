using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cuboid.Models;
using Cuboid.Input;

namespace Cuboid
{
    /// <summary>
    /// The base class for the HandleTool, for the
    /// - TranslateTool
    /// - RotateTool
    /// - ScaleTool
    /// 
    /// </summary>
    public abstract class AxisHandleTool<T, T2> : MonoBehaviour,
        IToolHasDefaultSelectBehaviour
        where T : AxisHandle<T2>
        where T2 : AxisHandleData, new()
    {
        [SerializeField] protected Transform _handleTransform;

        [SerializeField] protected HandleToolDataScriptableObject _handleToolData;

        protected SelectionController _selectionController;
        private Action<Selection> _onSelectionChanged;
        private Action<TransformData> _onSelectionTransformDataChanged;

        private bool _isDragging = false;

        private bool _visible;
        protected bool Visible
        {
            get => _visible;
            set
            {
                _visible = value;
                // update the handle visibility, since all handles should be a child of the gizmo transform,
                // simply set that one active
                _handleTransform.gameObject.SetActive(_visible);
            }
        }

        private bool _canUpdateHandlesRotation = true;

        private void Awake()
        {
            InstantiateHandles();
        }

        #region AxisHandleTool API

        protected virtual void InstantiateHandles()
        {

        }

        protected virtual void UpdateHandlesRotation()
        {

        }

        #endregion

        private void Start()
        {
            _selectionController = SelectionController.Instance;

            _onSelectionChanged = OnSelectionChanged;
            _onSelectionTransformDataChanged = OnSelectionTransformDataChanged;

            Register();
        }

        private void LateUpdate()
        {
            if (_canUpdateHandlesRotation)
            {
                UpdateHandlesRotation();
            }
        }

        #region Rotation towards camera

        protected Vector3 GetLocalCameraPosition()
        {
            // updates the rotation of the handles to always face the camera.
            Vector3 cameraPosition = Camera.main.transform.position;
            // get the dominant hand controller

            TransformData initialTransformData = _selectionController.InitialSelectionTransformData;

            // use the local camera position to determine in which quadrant the camera is
            Vector3 localCameraPosition = initialTransformData.WorldToLocalMatrix.MultiplyPoint3x4(cameraPosition);
            return localCameraPosition;
        }

        /// <summary>
        /// Method for rotating the handles when it is on the axis
        /// (e.g. for the scale or translate axis handles). 
        /// </summary>
        protected void RotateAxisHandles(T[] handles)
        {
            Vector3 localCameraPosition = GetLocalCameraPosition();

            foreach (T handle in handles)
            {
                Axis axis = handle.Data.axis;
                (Axis, Axis) otherAxes = handle.Data.axis.GetOtherAxes();

                // flip the axis along its axis by adding 180 degrees to the y rotation
                float r = localCameraPosition[(int)axis] > 0 ? 0f : -180f;

                Vector3 rotation = _handleToolData.AxisRotations[(int)axis]; // first get the base rotation
                rotation[(int)otherAxes.Item1] += r; // then add the rotation of the first other axis
                handle.transform.localEulerAngles = rotation;
            }
        }

        /// <summary>
        /// Method for rotating the handles when it is on the plane
        /// perpendicular to the axis (e.g. for the translate along plane handle or
        /// rotate handle)
        /// </summary>
        protected void RotatePlaneHandles(T[] handles)
        {
            Vector3 localCameraPosition = GetLocalCameraPosition();

            foreach (T handle in handles)
            {
                Axis axis = handle.Data.axis;

                // get other indices
                (Axis, Axis) otherAxes = handle.Data.axis.GetOtherAxes();
                int xSign = localCameraPosition[(int)otherAxes.Item1] > 0 ? 1 : -1;
                int ySign = localCameraPosition[(int)otherAxes.Item2] > 0 ? 1 : -1;

                float r = 0;
                if (xSign == 1 && ySign == 1)
                {
                    r = 0;
                }
                else if (xSign == 1 && ySign == -1)
                {
                    r = 90f;
                }
                else if (xSign == -1 && ySign == -1)
                {
                    r = 180f;
                }
                else if (xSign == -1 && ySign == 1)
                {
                    r = 270f;
                }

                // rotate clockwise for Y axis, counter-clockwise for other axes
                int sign = (axis == Axis.Y) ? 1 : -1;

                Vector3 rotation = _handleToolData.AxisRotations[(int)axis] + new Vector3(0, 0, sign * r);
                handle.transform.localEulerAngles = rotation;
            }
        }

        #endregion

        #region Instantiation

        protected T[] InstantiateHandles(GameObject prefab, Action<T2> setData = null)
        {
            T[] handles = new T[3];

            for (int i = 0; i < 3; i++) // <3
            {
                GameObject handleInstance = Instantiate(prefab, _handleTransform, false);
                T handle = handleInstance.GetComponent<T>();
                Axis axis = (Axis)i;
                T2 data = new T2()
                {
                    Index = i,
                    axis = axis
                };
                setData?.Invoke(data);
                handle.Data = data;

                handle.positionUpdated += OnHandlePositionUpdated;
                handle.HandleDrag += OnHandleDrag;
                handle.HandleBeginDrag += OnHandleBeginDrag;
                handle.HandleEndDrag += OnHandleEndDrag;

                handle.PressedMaterial = _handleToolData.PressedMaterial;
                handle.DefaultMaterial = _handleToolData.DefaultMaterials[i];
                handle.HoveredMaterial = _handleToolData.HoveredMaterials[i];
                handleInstance.transform.localEulerAngles = _handleToolData.AxisRotations[i];

                handles[i] = handle;
            }

            return handles;
        }

        #endregion

        #region Handle events

        protected virtual void OnHandleBeginDrag()
        {
            // don't update the handles rotation when moving, because this will confuse the user
            _canUpdateHandlesRotation = false;
            _isDragging = true;
        }

        protected virtual void OnHandleEndDrag()
        {
            if (!_isDragging) { return; }

            ApplyTransformations();
            _canUpdateHandlesRotation = true;
            _isDragging = false;
        }

        private void ApplyTransformations()
        {
            Debug.Log($"{name} called ApplyTransformations");

            TransformCommand transformCommand = _selectionController.GetTransformCommand();
            UndoRedoController.Instance.Execute(transformCommand);
        }

        protected virtual void OnHandleDrag(SpatialPointerEventData eventData, T2 data)
        {

        }

        protected virtual void OnHandlePositionUpdated(Vector3 pressedPosition, Vector3 position, T2 data)
        {

        }

        #endregion

        private void OnSelectionChanged(Selection selection)
        {
            // update the visibility
            Visible = selection.ContainsObjects;
        }

        private void OnSelectionTransformDataChanged(TransformData transformData)
        {
            // update the transform
            _handleTransform.SetPositionAndRotation(transformData.Position, transformData.Rotation);
        }

        #region Action registration

        private void Register()
        {
            if (_selectionController != null)
            {
                _selectionController.Selection.Register(_onSelectionChanged);
                _selectionController.SelectionTransformDataChanged += _onSelectionTransformDataChanged;
                _onSelectionTransformDataChanged.Invoke(_selectionController.CurrentSelectionTransformData);
            }
        }

        private void Unregister()
        {
            if (_selectionController != null)
            {
                _selectionController.Selection.Unregister(_onSelectionChanged);
                _selectionController.SelectionTransformDataChanged -= _onSelectionTransformDataChanged;
            }

            if (_isDragging)
            {
                ApplyTransformations();
                _isDragging = false;
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
