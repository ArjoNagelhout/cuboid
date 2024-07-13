using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Cuboid.Models;
using Cuboid.Utils;

namespace Cuboid
{
    /// <summary>
    /// Allows the user to scale and rotate the selection using a selection box
    /// that is placed around all selected objects.
    /// 
    /// Face and corner handles are used for scaling.
    /// Edge handles are used for rotation.
    /// 
    /// 0 ---o--- 0 -o- 0
    /// |         |     |
    /// o    o    o  o  o
    /// |         |     |
    /// 0 ---o--- 0 -o- 0
    /// 
    /// </summary>
    public class SelectionBoundsTool : MonoBehaviour,
        IToolHasDefaultSelectBehaviour
    {
        /// <summary>
        /// Whether to show the rotation handles on the edges of the selection bounds. 
        /// </summary>
        private StoredBinding<bool> RotationHandles;

        /// <summary>
        /// Whether to show the scale handles on the corners and faces of the selection bounds
        /// </summary>
        private StoredBinding<bool> ScaleHandles;


        private SelectionController _selectionController;
        private Action<TransformData> _onSelectionTransformDataChanged;
        private Action<Selection> _onSelectionChanged;

        private SelectionBoundsHandle[] _handles;

        [SerializeField] private GameObject _faceHandlePrefab; // scale along axis
        [SerializeField] private GameObject _edgeHandlePrefab; // rotation
        [SerializeField] private GameObject _cornerhandlePrefab; // scale

        [SerializeField] private Transform _selectionBoxVisual;

        private bool _dragging = false;

        private bool __visible;
        private bool _visible
        {
            get => _visible;
            set
            {
                __visible = value;
                _selectionBoxVisual.gameObject.SetActive(__visible);

                foreach (SelectionBoundsHandle handle in _handles)
                {
                    handle.gameObject.SetActive(__visible);
                }
            }
        }

        private void Awake()
        {
            RotationHandles = new("SelectionBoundsTool_RotationHandles", false);
            ScaleHandles = new("SelectionBoundsTool_ScaleHandles", true);

            _handles = CreateHandles();
        }

        private void Start()
        {
            _selectionController = SelectionController.Instance;

            _onSelectionChanged = OnSelectionChanged;
            _onSelectionTransformDataChanged = OnSelectionTransformDataChanged;

            Register();
        }

        private void OnHandleBeginDrag()
        {
            _dragging = true;
        }

        private void OnSelectionChanged(Selection selection)
        {
            _visible = selection.ContainsObjects;
        }

        public void OnSelectionTransformDataChanged(TransformData transformData)
        {
            // Update the visual selection box
            _selectionBoxVisual.SetFromTransformData(transformData);
            UpdateHandlePositions(_handles, transformData);
        }

        private void OnHandleEndDrag()
        {
            _dragging = false;
            ApplyTransformations();
        }

        private void ApplyTransformations()
        {
            TransformCommand transformCommand = _selectionController.GetTransformCommand();
            UndoRedoController.Instance.Execute(transformCommand);
        }

        /// <summary>
        /// Gets called by a Handle when its position has been recalculated
        /// Should recalculate the Scale box bounds
        /// </summary>
        /// <param name="newPosition"></param>
        /// <param name="index"></param>
        private void OnHandlePositionUpdated(Vector3 pressedPosition, Vector3 newPosition, SelectionBoundsHandleData data)
        {
            TransformData initialTransformData = _selectionController.InitialSelectionTransformData;
            TransformData newTransformData = initialTransformData;

            if (data.handleType == SelectionBoundsHandleData.HandleType.Edge)
            {
                newTransformData = CalculateNewRotation(newPosition, data, initialTransformData);
                _selectionController.SetCurrentSelectionTransformData(newTransformData);
                _selectionController.UpdateRealityObjectInstanceTransforms();
            }
            else
            {
                newTransformData = CalculateNewScale(newPosition, data, initialTransformData);
                _selectionController.SetCurrentSelectionTransformData(newTransformData);
                _selectionController.UpdateRealityObjectInstanceTransforms();
            }
        }

        public static Vector3 GetRotationPlaneNormal(Vector3 handleRelativePosition, TransformData initialTransformData)
        {
            Vector3 direction = Vector3.up;
            if (handleRelativePosition.x == 0)
            {
                direction = Vector3.right;
            }
            else if (handleRelativePosition.y == 0)
            {
                direction = Vector3.up;
            }
            else if (handleRelativePosition.z == 0)
            {
                direction = Vector3.forward;
            }

            return (initialTransformData.Rotation * direction).normalized;
        }

        /// <summary>
        /// This method constructs the plane along which the new handle position
        /// will be positioned for the rotation (edge) handle. 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static Plane ConstructRotationPlaneFromSelectionHandleData(SelectionBoundsHandleData data, TransformData initialTransformData)
        {
            Vector3 planeInNormal = GetRotationPlaneNormal(data.Position, initialTransformData);
            Vector3 planeInPoint = initialTransformData.Position;

            return new Plane(planeInNormal, planeInPoint);
        }

        /// <summary>
        /// Calculates the new scale
        /// around the axis that is perpendicular to the plane the rotation handle is on.
        /// </summary>
        /// <param name="newPosition"></param>
        /// <param name="data"></param>
        /// <param name="initialTransformData"></param>
        /// <returns></returns>
        private static TransformData CalculateNewRotation(Vector3 newPosition, SelectionBoundsHandleData data, TransformData initialTransformData)
        {
            Vector3 initialHandlePosition = initialTransformData.LocalToWorldMatrix.MultiplyPoint3x4(data.Position / 2);
            Vector3 initialHandlePositionRelativeToScaleBox = initialHandlePosition - initialTransformData.Position;

            // Construct the plane
            Plane rotationPlane = ConstructRotationPlaneFromSelectionHandleData(data, initialTransformData);
            Vector3 axisToRotateAround = rotationPlane.normal;

            // Project the newPosition vector onto the plane
            newPosition = rotationPlane.ClosestPointOnPlane(newPosition);

            // Then calculate the delta angle
            float deltaAngle = Vector3.SignedAngle(initialHandlePositionRelativeToScaleBox, newPosition - initialTransformData.Position, axisToRotateAround);

            // Calculate the new rotation
            Quaternion deltaRotation = Quaternion.AngleAxis(deltaAngle, axisToRotateAround);
            Quaternion newRotation = deltaRotation * initialTransformData.Rotation;

            return initialTransformData.SetRotation(newRotation);
        }

        /// <summary>
        /// Calculates the new scale and position for the selection transform
        /// (and thus the selected RealityObjects) based on the translated handle.
        /// </summary>
        /// <param name="newPosition"></param>
        /// <param name="data"></param>
        /// <param name="initialTransformData"></param>
        private TransformData CalculateNewScale(Vector3 newPosition, SelectionBoundsHandleData data, TransformData initialTransformData)
        {
            Vector3 localNewPosition = initialTransformData.WorldToLocalMatrix.MultiplyPoint3x4(newPosition);
            Vector3 localInitialHandlePosition = data.Position / 2;
            Vector3 localDeltaHandlePosition = localNewPosition - localInitialHandlePosition;

            if (ModifiersController.Instance.ShiftModifier.Value)
            {
                Vector3 scaleVector = initialTransformData.Scale;
                Vector3 invertedScaleVector = new Vector3(1 / scaleVector.x, 1 / scaleVector.y, 1 / scaleVector.z);

                Vector3 scaledDeltaPosition = Vector3.Scale(localDeltaHandlePosition, scaleVector);
                Vector3 scaledProjectionVector = Vector3.Scale(localInitialHandlePosition, scaleVector);

                Vector3 scaledDeltaResult = Vector3.Project(scaledDeltaPosition, scaledProjectionVector);
                localDeltaHandlePosition = Vector3.Scale(scaledDeltaResult, invertedScaleVector);
            }

            if (data.handleType == SelectionBoundsHandleData.HandleType.Face)
            {
                localDeltaHandlePosition = Vector3.Project(localDeltaHandlePosition, localInitialHandlePosition);
            }

            Vector3 localDeltaPosition = localDeltaHandlePosition / 2;

            // Make sure the scale gets applied to the right side
            Vector3 localDeltaScale = Vector3.Scale(localDeltaHandlePosition, data.Position);

            if (ModifiersController.Instance.ShiftModifier.Value)
            {
                // Extend the scale to all axes.
                localDeltaScale = localDeltaScale.ExtendToAllAxes();
            }

            if (ModifiersController.Instance.OptionModifier.Value)
            {
                localDeltaPosition = Vector3.zero;
                localDeltaScale *= 2;
            }

            TransformData localDeltaTransform = new TransformData(
                Vector3.zero + localDeltaPosition,
                Quaternion.identity,
                Vector3.one + localDeltaScale);

            // now we need to get the initial transform, offset by the position and scale in local space
            return new TransformData(initialTransformData.Matrix * localDeltaTransform.Matrix);
        }

        /// <summary>
        /// Updates all handle positions based on the scale box bounds
        /// </summary>
        /// <param name="handles"></param>
        private static void UpdateHandlePositions(SelectionBoundsHandle[] handles, TransformData currentTransformData)
        {
            foreach (SelectionBoundsHandle handle in handles)
            {
                Vector3 localPosition = handle.Data.Position / 2;
                handle.transform.localPosition = currentTransformData.LocalToWorldMatrix.MultiplyPoint3x4(localPosition);
            }
        }

        /// <summary>
        /// Creates and returns the rotation and scale handles for
        /// the selection box at its faces, corners and edges. 
        /// </summary>
        /// <returns></returns>
        private SelectionBoundsHandle[] CreateHandles()
        {
            int count = RotationHandles.Value ? 6 + 8 + 12 : 6 + 8; // 6 + 8 + 12
            SelectionBoundsHandle[] handles = new SelectionBoundsHandle[count];//  + 12]; // + 12]; // 6 faces, 8 corners, 12 sides

            int handleIndex = 0;
            for (int z = -1; z <= 1; z++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        // If the sum is 3: corner
                        // 2: edge
                        // 1: face
                        int sum = math.abs(x) + math.abs(y) + math.abs(z);

                        string handleName = "";
                        SelectionBoundsHandleData.HandleType handleType = SelectionBoundsHandleData.HandleType.Corner;
                        GameObject handlePrefab = _cornerhandlePrefab;

                        if (sum == 1) // face
                        {
                            handleName = "Face_Scaling";
                            handleType = SelectionBoundsHandleData.HandleType.Face;
                            handlePrefab = _faceHandlePrefab;
                        }
                        else if (sum == 3) // corner
                        {
                            handleName = "Corner_Scaling";
                            handleType = SelectionBoundsHandleData.HandleType.Corner;
                            handlePrefab = _cornerhandlePrefab;
                        }
                        else if (sum == 2 && RotationHandles.Value) // edge, only if rotation is enabled
                        {
                            handleName = "Edge_Rotation";
                            handleType = SelectionBoundsHandleData.HandleType.Edge;
                            handlePrefab = _edgeHandlePrefab;
                        }
                        else
                        {
                            continue; // this is at the center (0, 0, 0), doesn't need a handle
                        }

                        SelectionBoundsHandleData selectionHandleData = new SelectionBoundsHandleData()
                        {
                            Index = handleIndex,
                            Position = new Vector3(x, y, z),
                            handleType = handleType
                        };

                        GameObject instantiatedHandle = Instantiate(handlePrefab, transform, false);
                        instantiatedHandle.name = $"{handleName} ({x}, {y}, {z})";
                        SelectionBoundsHandle selectionBoundsHandle = instantiatedHandle.GetComponent<SelectionBoundsHandle>();
                        selectionBoundsHandle.Data = selectionHandleData;

                        selectionBoundsHandle.positionUpdated += OnHandlePositionUpdated;
                        selectionBoundsHandle.HandleBeginDrag += OnHandleBeginDrag;
                        selectionBoundsHandle.HandleEndDrag += OnHandleEndDrag;

                        handles[handleIndex] = selectionBoundsHandle;

                        handleIndex++;
                    }
                }
            }
            return handles;
        }

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

            if (_dragging)
            {
                ApplyTransformations();
                _dragging = false;
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
    }
}
