// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Cuboid.Input
{
    /// <summary>
    /// The RealityRayInteractor is a substitute for the XRRayInteractor from
    /// the XR Interaction Toolkit.
    /// </summary>
    public class RayInteractor : MonoBehaviour
    {
        [SerializeField] private Handedness.Hand _hand;

        [SerializeField] private float _maxRaycastDistance = 100f;

        /// <summary>
        /// 
        /// </summary>
        public int PointerId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public LayerMask LayerMask = -1;

        /// <summary>
        /// 
        /// </summary>
        public Vector2 ScrollDelta { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        [NonSerialized] public List<Vector3> RaycastPoints = new List<Vector3>();

        /// <summary>
        /// The last ray cast done for this model.
        /// </summary>
        /// <seealso cref="PointerEventData.pointerCurrentRaycast"/>
        public RaycastResult CurrentRaycast { get; private set; }

        /// <summary>
        /// The endpoint index within the list of ray cast points that the <see cref="CurrentRaycast"/> refers to when a hit occurred.
        /// Otherwise, a value of <c>0</c> if no hit occurred.
        /// </summary>
        /// <seealso cref="CurrentRaycast"/>
        /// <seealso cref="RaycastPoints"/>
        /// <seealso cref="SpatialPointerEventData.rayHitIndex"/>
        public int CurrentRaycastEndpointIndex { get; private set; }

        /// <summary>
        /// Whether the state of the select option has changed this frame.
        /// </summary>
        public ButtonDeltaState SelectDelta { get; private set; }

        public ButtonDeltaState JoystickButtonDeltaState { get; private set; }

        public float DistanceOffset { get; set; }

        public Vector3 SpatialPosition { get; set; } = Vector3.zero;

        public Vector3 SpatialTargetPosition { get; set; } = Vector3.zero;

        // Whether the SpatialInputModule should try to interact with anything else
        public bool OutsideUICaptured { get; set; } = false;

        // set default configuration (e.g. drag threshold, stabilization radius)
        public SpatialPointerConfiguration Configuration { get; set; } = SpatialPointerConfiguration.Default;

        // set default reticle data (e.g. pressed color)
        public SpatialPointerReticleData ReticleData { get; set; } = SpatialPointerReticleData.Default;

        /// <summary>
        /// Checks whether this model has meaningfully changed this frame.
        /// This is used by the UI system to avoid excessive work. Use <see cref="OnFrameFinished"/> to reset.
        /// </summary>
        public bool ChangedThisFrame { get; private set; }

        private List<SamplePoint> _samplePoints = new List<SamplePoint>();

        public Transform RayOriginTransform;

        private SpatialInputModule _realityInputModule;
        private InputController _inputController;

        [SerializeField] private LineRenderer _lineRenderer;

        [SerializeField] private GameObject _spatialPointerReticlePrefab;
        private SpatialPointerReticle _spatialPointerReticle;

        [Header("Visual Appearance")]
        [SerializeField] private RayInteractorVisualsScriptableObject _visuals;

        private Action<bool> _onPrimaryButtonChanged;
        private Action<Vector2> _onScroll2DChanged;
        private Action _onJoystickButtonPressed;

        private void Awake()
        {
            // Instantiate reticle, this, because the Shapes package creates material instances,
            // which will be changed every time the app is played or the scene is loaded.
            //
            // This in turn gets tracked in version control and makes commit messages messy.
            //
            // Should be in Awake because it will get fired also if the component is disabled (which happens due to handedness changing on start)
            // And because the SceneController will load the new scene, and it will instantiate its reticle in that scene, which gets unloaded
            // in its entirety on switching documents, and thus gets destroyed. 
            //
            // This throws NullReferenceErrors, which results in unpredictable behaviour in Unity (e.g. not executing the rest of a method,
            // without crashing the application, kind of like a derailed train that still manages to move forward :)
            _spatialPointerReticle = Instantiate(_spatialPointerReticlePrefab, null, false).GetComponent<SpatialPointerReticle>();
        }

        private void Start()
        {
            _realityInputModule = SpatialInputModule.Instance;
            _inputController = InputController.Instance;

            _onPrimaryButtonChanged = OnPrimaryButtonChanged;
            _onScroll2DChanged = OnScroll2DChanged;
            _onJoystickButtonPressed = OnJoystickButtonPressed;

            Register();
        }

        /// <summary>
        /// Should copy all relevant data from the eventData, in order to store it
        /// for the next frame. 
        /// </summary>
        public void CopyTo(SpatialPointerEventData eventData)
        {
            eventData.rayPoints = RaycastPoints;
            eventData.layerMask = LayerMask;
            eventData.pointerId = PointerId;
            eventData.scrollDelta = ScrollDelta;
            eventData.distance = DistanceOffset;
            eventData.spatialPosition = SpatialPosition;
            eventData.configuration = Configuration;
            eventData.reticleData = ReticleData;
            eventData.outsideUICaptured = OutsideUICaptured;
        }

        /// <summary>
        /// This is called at the start of the frame, to get the data of the
        /// specific raycaster to the eventData. 
        /// </summary>
        public void CopyFrom(SpatialPointerEventData eventData)
        {
            CurrentRaycast = eventData.pointerCurrentRaycast;
            SpatialPosition = eventData.spatialPosition;
            DistanceOffset = eventData.distance;
            SpatialTargetPosition = eventData.spatialTargetPosition;
            Configuration = eventData.configuration;
            ReticleData = eventData.reticleData;
            OutsideUICaptured = eventData.outsideUICaptured;

            _spatialPointerReticle.Data = ReticleData;
            _spatialPointerReticle.OnSpatialPointerEventDataUpdated(eventData);
        }

        private void OnPrimaryButtonChanged(bool pressed)
        {
            SelectDelta = pressed ? ButtonDeltaState.Pressed : ButtonDeltaState.Released;
            _lineRenderer.colorGradient = pressed ? _visuals.Pressed : _visuals.Normal;
        }

        private void OnScroll2DChanged(Vector2 value)
        {
            ScrollDelta = value;
        }

        private void OnJoystickButtonPressed()
        {
            JoystickButtonDeltaState = ButtonDeltaState.Pressed;
        }

        public void UpdateModel()
        {
            RaycastPoints.Clear();

            UpdateSamplePoints(2, _samplePoints);
            int pointsCount = _samplePoints.Count;
            if (pointsCount > 0)
            {
                if (RaycastPoints.Capacity < pointsCount)
                {
                    RaycastPoints.Capacity = pointsCount;
                }

                for (int i = 0; i < pointsCount; i++)
                {
                    RaycastPoints.Add(_samplePoints[i].Position);
                }
            }

            ChangedThisFrame = true;
        }

        public void OnFrameFinished()
        {
            SelectDelta = ButtonDeltaState.NoChange;
            JoystickButtonDeltaState = ButtonDeltaState.NoChange;
            ChangedThisFrame = false;

            _lineRenderer.SetPosition(0, RayOriginTransform.position);
            _lineRenderer.SetPosition(1, SpatialTargetPosition);
        }

        /// <summary>
        /// Approximates the curve into a polygonal chain of endpoints, whose line segments can be used as
        /// the rays for doing Physics ray casts.
        /// </summary>
        /// <param name="count">The number of sample points to calculate.</param>
        /// <param name="samplePoints">The result list of sample points to populate.</param>
        void UpdateSamplePoints(int count, List<SamplePoint> samplePoints)
        {
            Assert.IsTrue(count >= 2);

            samplePoints.Clear();
            SamplePoint samplePoint = new SamplePoint
            {
                Position = RayOriginTransform.position,
                Parameter = 0f,
            };
            samplePoints.Add(samplePoint);

            samplePoint.Position = samplePoints[0].Position + RayOriginTransform.forward * _maxRaycastDistance;
            samplePoint.Parameter = 1f;
            samplePoints.Add(samplePoint);
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

        private void Register()
        {
            if (_realityInputModule != null)
            {
                _realityInputModule.RegisterInteractor(this);
            }

            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = true;
            }

            if (_spatialPointerReticle != null)
            {
                _spatialPointerReticle.gameObject.SetActive(true);
            }

            if (_inputController != null)
            {
                _inputController.ControllersInputData[_hand].PrimaryButton.Register(_onPrimaryButtonChanged);
                _inputController.ControllersInputData[_hand].Scroll2D.Register(_onScroll2DChanged);
                _inputController.ControllersInputData[_hand].JoystickButtonPressed += _onJoystickButtonPressed;
            }
        }

        private void Unregister()
        {
            if (_realityInputModule != null)
            {
                _realityInputModule.UnregisterInteractor(this);
            }

            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = false;
            }

            if (_spatialPointerReticle != null)
            {
                _spatialPointerReticle.gameObject.SetActive(false);
            }

            if (_inputController != null)
            {
                _inputController.ControllersInputData[_hand].PrimaryButton.Unregister(_onPrimaryButtonChanged);
                _inputController.ControllersInputData[_hand].Scroll2D.Unregister(_onScroll2DChanged);
                _inputController.ControllersInputData[_hand].JoystickButtonPressed -= _onJoystickButtonPressed;
            }
        }

        /// <summary>
        /// A point within a polygonal chain of endpoints which form line segments
        /// to approximate the curve. Each line segment is where the ray cast starts and ends.
        /// </summary>
        private struct SamplePoint
        {
            /// <summary>
            /// The world space position of the sample.
            /// </summary>
            public Vector3 Position { get; set; }

            /// <summary>
            /// For <see cref="LineType.ProjectileCurve"/>, this represents flight time at the sample.
            /// For <see cref="LineType.BezierCurve"/> and <see cref="LineType.StraightLine"/>, this represents
            /// the parametric parameter <i>t</i> of the curve at the sample (with range [0, 1]).
            /// </summary>
            public float Parameter { get; set; }
        }
    }
}
