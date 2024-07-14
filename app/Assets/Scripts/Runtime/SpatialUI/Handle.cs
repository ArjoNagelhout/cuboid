// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cuboid.Utils;
using Cuboid.Input;

namespace Cuboid
{
    /// <summary>
    /// Generic data that is the same for each Handle
    /// </summary>
    public class HandleData
    {
        /// <summary>
        /// The index is used for identifying which of the handles is being pressed
        /// when modifying using the tool class. 
        /// </summary>
        public int Index;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Handle<T> : MonoBehaviour,
        ISpatialPointerEnterHandler,
        ISpatialPointerExitHandler,
        ISpatialPointerDownHandler,
        ISpatialPointerUpHandler,
        ISpatialDragHandler,
        ISpatialBeginDragHandler,
        ISpatialEndDragHandler,
        ISpatialPointerConfiguration
        where T : HandleData
    {
        [SerializeField] private Vector3 _referenceScale = Vector3.one;

        private T _data;
        public T Data
        {
            get => _data;
            set
            {
                _data = value;
            }
        }

        private bool _hovered = false;
        public bool Hovered
        {
            get => _hovered;
            private set
            {
                _hovered = value;
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

        [Header("Configuration")]
        [SerializeField] private bool _useDefaultConfiguration = true;
        [SerializeField] private SpatialPointerConfiguration _configuration;
        public SpatialPointerConfiguration Configuration
        {
            get => _useDefaultConfiguration ? SpatialPointerConfiguration.Default : _configuration;
            set => _configuration = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newPosition"></param>
        /// <param name="data"></param>
        public delegate void PositionUpdated(Vector3 pressedPosition, Vector3 newPosition, T data);

        public Action HandleBeginDrag;
        public Action HandleEndDrag;

        /// <summary>
        /// Should be listened to by the tool etc.
        /// </summary>
        public PositionUpdated positionUpdated;

        public Action<SpatialPointerEventData, T> HandleDrag;

        protected virtual void Awake()
        {
            UpdateScale();
            UpdateHoverPressedAppearance();
        }

        private void Start()
        {
            UpdateHoverPressedAppearance();
        }

        private void UpdateScale()
        {
            transform.localScale = ConstantScaleOnScreen.GetConstantScale(transform.position, _referenceScale);
        }

        /// <summary>
        /// Can be overridden, updates scale
        /// </summary>
        protected virtual void LateUpdate()
        {
            UpdateScale();
        }

        protected virtual void UpdateHoverPressedAppearance()
        {

        }

        void ISpatialPointerEnterHandler.OnSpatialPointerEnter(SpatialPointerEventData eventData)
        {
            Hovered = true;
        }

        void ISpatialPointerExitHandler.OnSpatialPointerExit(SpatialPointerEventData eventData)
        {
            Hovered = false;
        }

        void ISpatialPointerDownHandler.OnSpatialPointerDown(SpatialPointerEventData eventData)
        {
            Pressed = true;
        }

        void ISpatialPointerUpHandler.OnSpatialPointerUp(SpatialPointerEventData eventData)
        {
            Pressed = false;
        }

        void ISpatialDragHandler.OnSpatialDrag(SpatialPointerEventData eventData)
        {
            UpdatePosition(eventData);
            HandleDrag?.Invoke(eventData, Data);
        }

        private Vector3 _raycastOffset = Vector3.zero;
        private Vector3 _pressedPosition = Vector3.zero;

        private void UpdatePosition(SpatialPointerEventData eventData)
        {
            positionUpdated?.Invoke(_pressedPosition, eventData.spatialPosition + _raycastOffset, Data);
        }

        void ISpatialBeginDragHandler.OnSpatialBeginDrag(SpatialPointerEventData eventData)
        {
            _pressedPosition = transform.position;
            _raycastOffset = _pressedPosition - eventData.spatialPosition;
            HandleBeginDrag?.Invoke();
        }

        void ISpatialEndDragHandler.OnSpatialEndDrag(SpatialPointerEventData eventData)
        {
            HandleEndDrag?.Invoke();
        }
    }
}
