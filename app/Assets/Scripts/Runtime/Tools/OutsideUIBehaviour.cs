using System;
using System.Collections;
using System.Collections.Generic;
using Cuboid.Input;
using UnityEngine;

namespace Cuboid
{
    /// <summary>
    /// A base class that can be subclassed by tools for example to receive events
    /// from the SpatialInputModule about input outside the UI. 
    /// </summary>
    public abstract class OutsideUIBehaviour : MonoBehaviour
    {
        protected SpatialInputModule _spatialInputModule;

        private Action<SpatialPointerEventData> _outsideUIPointerEnter;
        private Action<SpatialPointerEventData> _outsideUIPointerExit;
        private Action<SpatialPointerEventData> _outsideUIPointerDown;
        private Action<SpatialPointerEventData> _outsideUIPointerUp;
        private Action<SpatialPointerEventData> _outsideUIPointerClick;
        private Action<SpatialPointerEventData> _outsideUIBeginDrag;
        private Action<SpatialPointerEventData> _outsideUIDrag;
        private Action<SpatialPointerEventData> _outsideUIEndDrag;
        private Action<SpatialPointerEventData> _outsideUIDrop;
        private Action<SpatialPointerEventData> _outsideUIScroll;
        private Action<SpatialPointerEventData> _outsideUIMove;

        private Action<SpatialPointerEventData> _pointerDown;

        protected virtual void Start()
        {
            _spatialInputModule = SpatialInputModule.Instance;

            _outsideUIPointerEnter = OutsideUIPointerEnter;
            _outsideUIPointerExit = OutsideUIPointerExit;
            _outsideUIPointerDown = OutsideUIPointerDown;
            _outsideUIPointerUp = OutsideUIPointerUp;
            _outsideUIPointerClick = OutsideUIPointerClick;
            _outsideUIBeginDrag = OutsideUIBeginDrag;
            _outsideUIDrag = OutsideUIDrag;
            _outsideUIEndDrag = OutsideUIEndDrag;
            _outsideUIDrop = OutsideUIDrop;
            _outsideUIScroll = OutsideUIScroll;
            _outsideUIMove = OutsideUIMove;

            _pointerDown = PointerDown;

            Register();
        }

        protected virtual void OutsideUIPointerEnter(SpatialPointerEventData eventData)
        {
            //Debug.Log("OutsideUI Pointer Enter");
        }

        protected virtual void OutsideUIPointerExit(SpatialPointerEventData eventData)
        {
            //Debug.Log("OutsideUI Pointer Exit");
        }

        protected virtual void OutsideUIPointerDown(SpatialPointerEventData eventData)
        {
            //Debug.Log("OutsideUI Pointer Down");
        }

        protected virtual void OutsideUIPointerUp(SpatialPointerEventData eventData)
        {
            //Debug.Log("OutsideUI Pointer Up");
        }

        protected virtual void OutsideUIPointerClick(SpatialPointerEventData eventData)
        {
            //Debug.Log("OutsideUI Pointer Click");
        }

        protected virtual void OutsideUIBeginDrag(SpatialPointerEventData eventData)
        {
            //Debug.Log("OutsideUI Begin Drag");
        }

        protected virtual void OutsideUIDrag(SpatialPointerEventData eventData)
        {
            //Debug.Log("OutsideUI Drag");
        }

        protected virtual void OutsideUIEndDrag(SpatialPointerEventData eventData)
        {
            //Debug.Log("OutsideUI End Drag");
        }

        protected virtual void OutsideUIDrop(SpatialPointerEventData eventData)
        {
            //Debug.Log("OutsideUI Drop");
        }

        protected virtual void OutsideUIScroll(SpatialPointerEventData eventData)
        {
            //Debug.Log("OutsideUI Scroll");
        }

        protected virtual void OutsideUIMove(SpatialPointerEventData eventData)
        {
            //Debug.Log("OutsideUI Move");
        }

        protected virtual void PointerDown(SpatialPointerEventData eventData)
        {

        }

        #region Action registration

        protected virtual void Register()
        {
            if (_spatialInputModule != null)
            {
                _spatialInputModule.outsideUIPointerEnter += _outsideUIPointerEnter;
                _spatialInputModule.outsideUIPointerExit += _outsideUIPointerExit;
                _spatialInputModule.outsideUIPointerDown += _outsideUIPointerDown;
                _spatialInputModule.outsideUIPointerUp += _outsideUIPointerUp;
                _spatialInputModule.outsideUIPointerClick += _outsideUIPointerClick;
                _spatialInputModule.outsideUIBeginDrag += _outsideUIBeginDrag;
                _spatialInputModule.outsideUIDrag += _outsideUIDrag;
                _spatialInputModule.outsideUIEndDrag += _outsideUIEndDrag;
                _spatialInputModule.outsideUIDrop += _outsideUIDrop;
                _spatialInputModule.outsideUIScroll += _outsideUIScroll;
                _spatialInputModule.outsideUIMove += _outsideUIMove;

                _spatialInputModule.pointerDown += _pointerDown;
            }
        }

        protected virtual void Unregister()
        {
            if (_spatialInputModule != null)
            {
                _spatialInputModule.outsideUIPointerEnter -= _outsideUIPointerEnter;
                _spatialInputModule.outsideUIPointerExit -= _outsideUIPointerExit;
                _spatialInputModule.outsideUIPointerDown -= _outsideUIPointerDown;
                _spatialInputModule.outsideUIPointerUp -= _outsideUIPointerUp;
                _spatialInputModule.outsideUIPointerClick -= _outsideUIPointerClick;
                _spatialInputModule.outsideUIBeginDrag -= _outsideUIBeginDrag;
                _spatialInputModule.outsideUIDrag -= _outsideUIDrag;
                _spatialInputModule.outsideUIEndDrag -= _outsideUIEndDrag;
                _spatialInputModule.outsideUIDrop -= _outsideUIDrop;
                _spatialInputModule.outsideUIScroll -= _outsideUIScroll;
                _spatialInputModule.outsideUIMove -= _outsideUIMove;

                _spatialInputModule.pointerDown -= _pointerDown;
            }
        }

        protected void OnEnable()
        {
            Register();
        }

        protected void OnDisable()
        {
            Unregister();
        }

        protected void OnDestroy()
        {
            Unregister();
        }

        #endregion
    }
}
