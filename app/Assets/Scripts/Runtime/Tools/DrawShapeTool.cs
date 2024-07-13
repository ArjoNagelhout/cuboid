using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cuboid.Input;
using Cuboid.Models;
using Cuboid.Utils;
using static Cuboid.Input.SpatialInputModule;
using Shapes;
using Cuboid.UI;

namespace Cuboid
{
    /// <summary>
    /// Allows the user to draw a cuboid in the 3D space.
    ///
    /// Follows multiple steps.
    ///
    /// Should implement raycasting with objects in the scene when not yet
    /// </summary>
    [PrettyTypeNameAttribute(Name = "Draw Shape Tool")]
    public class DrawShapeTool : OutsideUIBehaviour,
        IToolHasProperties
    {
        private static DrawShapeTool _instance;
        public static DrawShapeTool Instance => _instance;

        public enum Shape
        {
            Cuboid,
            Ellipsoid,
            Cylinder,
            Cone,
            Torus,

            // 2D
            Rectangle,
            Sphere,
            Line
        }

        //[RuntimeSerializedPropertyEnum]
        //[NonSerialized] public StoredBinding<Shape> ActiveShape;

        // These are the steps that should be walked through, when pressing down and up immediately on the
        // same spot, it will only confirm that specific step.
        // when dragging, it will confirm.
        private enum Step
        {
            None = 0,
            Point = 1,
            Line = 2,
            Rectangle = 3,
            Cuboid = 4
        }

        private Step CurrentStep = Step.None;

        private Vector3 _startPoint = Vector3.zero;
        private Vector3 _linePoint = Vector3.zero;
        private Vector3 _rectanglePoint = Vector3.zero;
        private Vector3 _cuboidPoint = Vector3.zero;

        [SerializeField] private GameObject _startPointVisual;
        [SerializeField] private Line _lineVisual;
        [SerializeField] private GameObject _linePointVisual;
        [SerializeField] private GameObject _cuboidVisual;

        private SpatialInputModule.GetConfigurationOutsideUIDelegate _getConfiguration;
        private SpatialInputModule.GetReticleDataOutsideUIDelegate _getReticleData;

        [SerializeField] private SpatialPointerConfiguration _configuration;
        [SerializeField] private SpatialPointerReticleData _reticleData;

        public enum DrawingMode
        {
            /// <summary>
            /// draw an entire rectangle in one go, instead of first having to define,
            /// the line.
            ///
            /// The orientation is then taken from the raycast object
            /// and otherwise from the world orientation. 
            /// </summary>
            SkipLine,

            /// <summary>
            /// Doesn't skip any steps. 
            /// </summary>
            AllSteps
        }

        //[RuntimeSerializedPropertyEnum(Label = "Drawing Mode")]
        //public StoredBinding<DrawingMode> ActiveDrawingMode;

        private SpatialPointerEventData _capturedEventData;

        private void Awake()
        {
            // Singleton implementation
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }

            //ActiveShape = new("DrawShapeTool_ActiveShape", Shape.Cuboid);
            //ActiveDrawingMode = new("DrawShapeTool_ActiveDrawingMode", DrawingMode.AllSteps);
        }

        protected override void Start()
        {
            _configuration.customGetSpatialPointerInputPosition = CalculateSpatialPointerPosition;
            _configuration.customIsOverDragThreshold = IsOverDragThreshold;

            _getConfiguration = () => { return _configuration; };
            _getReticleData = () => { return _reticleData; };

            base.Start();

            UpdateVisual(CurrentStep);
            InitializeValues();
        }

        private void UpdateVisual(Step step)
        {
            // updates the visual based on the current step
            _startPointVisual.SetActive(step >= Step.Point);
            _lineVisual.enabled = step >= Step.Line;
            _linePointVisual.SetActive(step >= Step.Line);

            _startPointVisual.transform.position = _startPoint;
            _linePointVisual.transform.position = _linePoint;
            _lineVisual[0] = _startPoint;
            _lineVisual[1] = _linePoint;

            _cuboidVisual.SetActive(step >= Step.Rectangle);

            if (step == Step.Rectangle || step == Step.Cuboid)
            {
                Vector3? cuboidPoint = step == Step.Cuboid ? _cuboidPoint : null; // make sure to set the cuboidPoint to null when drawing the rectangle
                TransformData cuboidTransformData = CalculateCuboidTransformData(_startPoint, _linePoint, _rectanglePoint, cuboidPoint);
                _cuboidVisual.transform.SetFromTransformData(cuboidTransformData);

                if (_instantiatedRealityObject != null)
                {
                    _instantiatedRealityObject.RealityObjectData.Transform.Value = cuboidTransformData;
                }
            }
        }

        /// <summary>
        /// Calculates the cuboid transform data based on the input startPoint, linePoint, rectanglePoint and cuboidPoint
        /// If no cuboidPoint is supplied, it will use a default height of 0.01f and thus display as a rectangle. 
        /// </summary>
        private static TransformData CalculateCuboidTransformData(Vector3 startPoint, Vector3 linePoint, Vector3 rectanglePoint, Vector3? cuboidPoint = null)
        {
            Vector3 right = linePoint - startPoint;
            Vector3 forward = rectanglePoint - linePoint;
            // if no cuboidpoint is supplied, use the cross product between the forward and right vectors to determine the up vector
            Vector3 up = cuboidPoint.HasValue ? cuboidPoint.Value - rectanglePoint : Vector3.Cross(forward, right);

            // Position, either take the center between the rectangle point and the start point or the cuboid point and the start point
            Vector3 endPoint = cuboidPoint.HasValue ? cuboidPoint.Value : rectanglePoint;
            Vector3 position = startPoint + (endPoint - startPoint) / 2;

            // Rotation
            Quaternion rotation = forward != Vector3.zero ? Quaternion.LookRotation(forward, up) : Quaternion.identity;

            // Scale
            float height = cuboidPoint.HasValue ? up.magnitude : 0.01f;
            Vector3 scale = new Vector3(right.magnitude, height, forward.magnitude);

            return new TransformData(position, rotation, scale);
        }

        private Vector3 _planeDirection;
        private Vector3 _planeOrigin;
        private Vector3 _planeRight;
        private bool _outsideUIRaycastWithScene = false;

        private bool IsOverDragThreshold(SpatialPointerEventData eventData)
        {
            switch (_spatialInputModule.CurrentSpatialPointerMode.Value)
            {
                case SpatialPointerMode.WorldSpace:
                    return SpatialInputModule.IsSpatialPointerOverDragThreshold(eventData);
                case SpatialPointerMode.Raycast:
                    return SpatialInputModule.IsSpatialPointerOverDragThresholdRaycast(eventData);
            }
            return false;
        }

        private bool CalculateSpatialPointerPosition(SpatialPointerEventData eventData, out Vector3 result, out float distance)
        {
            bool valid = false;
            result = Vector3.zero;
            distance = 0f;

            switch (_spatialInputModule.CurrentSpatialPointerMode.Value)
            {
                case SpatialPointerMode.WorldSpace:
                    {
                        _spatialInputModule.UpdateDistanceOutsideUI(eventData);
                        SpatialInputModule.GetSpatialPointerInputPositionWorldSpace(eventData, out result, _spatialInputModule.DistanceOutsideUI);
                        valid = true;
                    }
                    break;
                case SpatialPointerMode.Raycast:
                    {
                        if (_outsideUIRaycastWithScene)
                        {
                            valid = eventData.pointerCurrentRaycast.isValid;
                            result = eventData.pointerCurrentRaycast.worldPosition;
                            distance = eventData.pointerCurrentRaycast.distance;
                        }

                        if (!valid)
                        {
                            Plane plane = new Plane(_planeDirection, _planeOrigin);
                            valid = SpatialInputModule.RaycastWithPlane(eventData, plane, out result, out distance);
                        }
                    }
                    break;
            }

            return valid;
        }
        
        private void InitializeValues()
        {
            _planeDirection = Vector3.up;
            _planeOrigin = Vector3.zero;
            _planeRight = Vector3.Cross(Vector3.right, _planeDirection);
            _outsideUIRaycastWithScene = true;
        }

        /// <summary>
        /// The data for the object that is currently being drawn. 
        /// </summary>
        private RealityShapeObjectData _realityShapeObjectData;
        private RealityObject _instantiatedRealityObject;

        private void InstantiateShape()
        {
            Guid guid = Guid.NewGuid();
            RealityShapeObjectData newObjectData = new RealityShapeObjectData()
            {
                Guid = guid,
                Name = new($"Shape_{guid}"),
                Transform = new(),
                Selected = new(false),

                Color = new(ColorsController.Instance.ActiveColor.Value),
                CornerQuality = new(0), // use tool settings
                CornerRadius = new(0.025f) // use tool settings
            };

            _realityShapeObjectData = newObjectData;
            RealitySceneController.Instance.Instantiate(_realityShapeObjectData, (onInstantiateResult) =>
            {
                // when the object has instantiated
                _instantiatedRealityObject = onInstantiateResult;
            });
        }

        private void SetSpatialInputModuleData(SpatialPointerEventData eventData, Step step)
        {
            switch (step)
            {
                case Step.Point:
                    {
                        _capturedEventData = eventData;
                        eventData.outsideUICaptured = true;
                        if (eventData.outsideUIPressRaycastResult.isValid)
                        {
                            _planeDirection = eventData.outsideUIPressRaycastResult.worldNormal;
                            _planeOrigin = eventData.outsideUIPressRaycastResult.worldPosition;

                            Transform raycastTransform = eventData.outsideUIPressRaycastResult.gameObject.transform;
                            bool useRight = _planeDirection.RoughlyEquals(raycastTransform.up) || _planeDirection.RoughlyEquals(-raycastTransform.up); // check for both up and down vectors
                            Vector3 localRight = useRight ? raycastTransform.right : raycastTransform.up;
                            //Debug.Log($"_planeDirection: {_planeDirection}, useRight: {useRight}, right: {raycastTransform.right}, up: {raycastTransform.up}, localRight: {localRight}");
                            _planeRight = Vector3.Cross(_planeDirection, localRight);
                        }
                        _outsideUIRaycastWithScene = false;
                    }
                    break;
                case Step.Line:
                    {
                        _planeOrigin = _linePoint;
                        _outsideUIRaycastWithScene = false;
                    }
                    break;
                case Step.Rectangle:
                    {
                        // now the next direction should be perpendicular to the rectangle
                        Vector3 right = _linePoint - _startPoint;
                        Vector3 forward = _rectanglePoint - _linePoint;
                        Vector3 up = Vector3.Cross(forward, right); // up needs to be constructed, because with the rectangle step the cuboid point is not set yet. 

                        _planeOrigin = _rectanglePoint;
                        _planeDirection = forward;
                        _outsideUIRaycastWithScene = false; // only raycast with the plane
                    }
                    break;
                case Step.Cuboid:

                    Complete(eventData);

                    break;
                default:
                    break;
            }
        }

        private void IncreaseStep(SpatialPointerEventData eventData)
        {
            if (CurrentStep == Step.None)
            {
                InstantiateShape();
            }

            CurrentStep++;

            UpdatePositions(eventData, CurrentStep);
            SetSpatialInputModuleData(eventData, CurrentStep);
            UpdateVisual(CurrentStep);

            Debug.Log(CurrentStep);
        }

        private void Complete(SpatialPointerEventData eventData)
        {
            CurrentStep = Step.None;

            // add the object to the scene
            AddCommand addCommand = new AddCommand(
                RealityDocumentController.Instance,
                RealitySceneController.Instance,
                RealitySceneController.Instance.OpenedRealitySceneIndex,
                _realityShapeObjectData
                );

            UndoRedoController.Instance.Execute(addCommand);

            _instantiatedRealityObject = null;
            _realityShapeObjectData = null;

            // release the capture
            eventData.outsideUICaptured = false;

            InitializeValues();
        }

        private void UpdatePositions(SpatialPointerEventData eventData, Step step)
        {
            Vector3 newPosition = eventData.spatialPosition;

            switch (step)
            {
                case Step.Point:
                    _startPoint = newPosition;
                    break;
                case Step.Line:
                    {
                        if (ModifiersController.Instance.ShiftModifier.Value)
                        {
                            // reproject the new position to steps of 45 degrees.

                            Vector3 deltaPosition = newPosition - _startPoint;
                            float angleInDegrees = - Vector3.SignedAngle(deltaPosition, _planeRight, _planeDirection);
                            float snappedAngle = angleInDegrees.Snap(360, 8);

                            Quaternion rotation = Quaternion.AngleAxis(snappedAngle, _planeDirection);

                            // now use that to construct the new vector
                            Vector3 directionVector = rotation * _planeRight;

                            // reproject newPosition
                            Vector3 correctedDeltaPosition = Vector3.Project(deltaPosition, directionVector);

                            newPosition = _startPoint + correctedDeltaPosition;
                        }
                        _linePoint = newPosition;
                    }
                    break;
                case Step.Rectangle:
                    {
                        // project the position onto the vector
                        Vector3 deltaPosition = newPosition - _linePoint;

                        Vector3 right = _linePoint - _startPoint;
                        Vector3 forward = Vector3.Cross(right, _planeDirection);

                        Vector3 correctedDeltaPosition = Vector3.Project(deltaPosition, forward);
                        newPosition = _linePoint + correctedDeltaPosition;
                        _rectanglePoint = newPosition;
                    }
                    break;
                case Step.Cuboid:
                    {
                        Vector3 deltaPosition = newPosition - _rectanglePoint;

                        Vector3 right = _linePoint - _startPoint;
                        Vector3 forward = _rectanglePoint - _linePoint;
                        Vector3 up = Vector3.Cross(forward, right); // up needs to be constructed, because with the rectangle step the cuboid point is not set yet.

                        Vector3 correctedDeltaPosition = Vector3.Project(deltaPosition, up);
                        newPosition = _rectanglePoint + correctedDeltaPosition;
                        _cuboidPoint = newPosition;
                    }
                    return;
                default:
                    break;
            }
        }

        protected override void OutsideUIPointerClick(SpatialPointerEventData eventData)
        {
            base.OutsideUIPointerClick(eventData);

            if (eventData.outsideUIValidPointerPosition)
            {
                IncreaseStep(eventData);
            }
        }

        protected override void OutsideUIMove(SpatialPointerEventData eventData)
        {
            base.OutsideUIMove(eventData);

            if (CurrentStep == Step.None) { return; }

            UpdatePositions(eventData, CurrentStep + 1);
            UpdateVisual(CurrentStep + 1);
        }

        protected override void OutsideUIBeginDrag(SpatialPointerEventData eventData)
        {
            base.OutsideUIBeginDrag(eventData);

            if (eventData.outsideUIValidPointerPosition)
            {
                IncreaseStep(eventData);
            }
        }

        protected override void OutsideUIEndDrag(SpatialPointerEventData eventData)
        {
            base.OutsideUIEndDrag(eventData);

            // if begin drag has completed the cuboid, we don't want to immediately start drawing
            // the next cuboid on drag end. 
            if (CurrentStep != Step.None)
            {
                IncreaseStep(eventData);
            }
        }

        protected override void Register()
        {
            base.Register();

            if (_spatialInputModule != null)
            {
                _spatialInputModule.outsideUIGetReticleData += _getReticleData;
                _spatialInputModule.outsideUIGetConfiguration += _getConfiguration;
            }
        }

        protected override void Unregister()
        {
            base.Unregister();

            // Destroy the instantiated reality object
            // it gets set to null on Complete, so it only gets destroyed
            // if the drawing operation wasn't completed yet. 
            if (_instantiatedRealityObject != null)
            {
                Destroy(_instantiatedRealityObject.gameObject);
                _realityShapeObjectData = null;
            }

            CurrentStep = Step.None;

            if (_spatialInputModule != null)
            {
                _spatialInputModule.outsideUIGetReticleData -= _getReticleData;
                _spatialInputModule.outsideUIGetConfiguration -= _getConfiguration;

                if (_capturedEventData != null)
                {
                    _capturedEventData.outsideUICaptured = false;
                    _capturedEventData = null;
                }
            }
        }
    }
}
