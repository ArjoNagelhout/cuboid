//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cuboid.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    /// <summary>
    /// A component for making a child RectTransform scroll.
    /// </summary>
    /// <remarks>
    /// ScrollRect will not do any clipping on its own. Combined with a Mask component, it can be turned into a scroll view.
    /// </remarks>
    public class ScrollView : UIBehaviour,
        IInitializePotentialDragHandler,
        IBeginDragHandler,
        IPointerDownHandler,
        IEndDragHandler,
        IDragHandler,
        IScrollHandler,
        ICanvasElement, ILayoutElement, ILayoutGroup
    {
        [SerializeField] private RectTransform _content;
        [SerializeField] private bool _horizontal = true;
        [SerializeField] private bool _vertical = true;
        [SerializeField] private float _elasticity = 0.1f; // The amount of elasticity to use when the content moves beyond the scroll rect.

        /// <summary>
        /// The deceleration rate is the speed reduction per second. A value of 0.5 halves the speed each second. The default is 0.135. The deceleration rate is only used when inertia is enabled.
        /// </summary>
        [SerializeField] private float _decelerationRate = 0.135f; // Only used when inertia is enabled

        /// <summary>
        /// The sensitivity to scroll wheel and track pad scroll events.
        /// </summary>
        [SerializeField] private float _scrollSensitivity = 1.0f;


        [SerializeField] private RectTransform _viewport;
        public RectTransform Viewport
        {
            get => _viewport;
            set { _viewport = value; SetDirtyCaching(); }
        }

        [SerializeField] private Scrollbar _horizontalScrollbar;

        public Action<Vector2> OnScrollPositionChanged;

        public void SetScrollPosition(Vector2 scrollPosition)
        {
            SetContentAnchoredPosition(scrollPosition, true);
            velocity = Vector2.zero;
        }

        /// <summary>
        /// Optional Scrollbar object linked to the horizontal scrolling of the ScrollRect.
        /// </summary>
        public Scrollbar horizontalScrollbar
        {
            get => _horizontalScrollbar;
            set
            {
                if (_horizontalScrollbar)
                    _horizontalScrollbar.onValueChanged.RemoveListener(SetHorizontalNormalizedPosition);
                _horizontalScrollbar = value;
                if (_horizontalScrollbar)
                    _horizontalScrollbar.onValueChanged.AddListener(SetHorizontalNormalizedPosition);
                SetDirtyCaching();
            }
        }

        [SerializeField] private Scrollbar _verticalScrollbar;

        

        /// <summary>
        /// Optional Scrollbar object linked to the vertical scrolling of the ScrollRect.
        /// </summary>
        public Scrollbar verticalScrollbar
        {
            get => _verticalScrollbar;
            set
            {
                if (_verticalScrollbar)
                    _verticalScrollbar.onValueChanged.RemoveListener(SetVerticalNormalizedPosition);
                _verticalScrollbar = value;
                if (_verticalScrollbar)
                    _verticalScrollbar.onValueChanged.AddListener(SetVerticalNormalizedPosition);
                SetDirtyCaching();
            }
        }

        // The offset from handle position to mouse down position
        private Vector2 m_PointerStartLocalCursor = Vector2.zero;
        protected Vector2 m_ContentStartPosition = Vector2.zero;

        protected Bounds _contentBounds;
        private Bounds _viewBounds;

        private Vector2 m_Velocity;

        /// <summary>
        /// The current velocity of the content in units per second.
        /// </summary>
        public Vector2 velocity { get { return m_Velocity; } set { m_Velocity = value; } }

        private bool m_Dragging;
        private bool m_Scrolling;

        private Vector2 m_PrevPosition = Vector2.zero;
        private Bounds m_PrevContentBounds;
        private Bounds m_PrevViewBounds;
        [NonSerialized]
        private bool m_HasRebuiltLayout = false;

        private float m_HSliderHeight;
        private float m_VSliderWidth;

        [System.NonSerialized] private RectTransform m_Rect;
        private RectTransform rectTransform
        {
            get
            {
                if (m_Rect == null)
                    m_Rect = GetComponent<RectTransform>();
                return m_Rect;
            }
        }

        private RectTransform m_HorizontalScrollbarRect;
        private RectTransform m_VerticalScrollbarRect;

        private DrivenRectTransformTracker m_Tracker;

        protected ScrollView()
        {
        }

        /// <summary>
        /// Rebuilds the scroll rect data after initialization.
        /// </summary>
        /// <param name="executing">The current step in the rendering CanvasUpdate cycle.</param>
        public virtual void Rebuild(CanvasUpdate executing)
        {
            if (executing == CanvasUpdate.Prelayout)
            {
                UpdateCachedData();
            }

            if (executing == CanvasUpdate.PostLayout)
            {
                UpdateBounds();
                UpdateScrollbars(Vector2.zero);
                UpdatePrevData();

                m_HasRebuiltLayout = true;
            }
        }

        public virtual void LayoutComplete()
        {
        }

        public virtual void GraphicUpdateComplete()
        {
        }

        void UpdateCachedData()
        {
            Transform transform = this.transform;
            m_HorizontalScrollbarRect = _horizontalScrollbar == null ? null : _horizontalScrollbar.transform as RectTransform;
            m_VerticalScrollbarRect = _verticalScrollbar == null ? null : _verticalScrollbar.transform as RectTransform;
            
            m_HSliderHeight = (m_HorizontalScrollbarRect == null ? 0 : m_HorizontalScrollbarRect.rect.height);
            m_VSliderWidth = (m_VerticalScrollbarRect == null ? 0 : m_VerticalScrollbarRect.rect.width);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (_horizontalScrollbar)
                _horizontalScrollbar.onValueChanged.AddListener(SetHorizontalNormalizedPosition);
            if (_verticalScrollbar)
                _verticalScrollbar.onValueChanged.AddListener(SetVerticalNormalizedPosition);

            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            SetDirty();
        }

        protected override void OnDisable()
        {
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);

            if (_horizontalScrollbar)
                _horizontalScrollbar.onValueChanged.RemoveListener(SetHorizontalNormalizedPosition);
            if (_verticalScrollbar)
                _verticalScrollbar.onValueChanged.RemoveListener(SetVerticalNormalizedPosition);

            m_Dragging = false;
            m_Scrolling = false;
            m_HasRebuiltLayout = false;
            m_Tracker.Clear();
            m_Velocity = Vector2.zero;
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            base.OnDisable();
        }

        /// <summary>
        /// See member in base class.
        /// </summary>
        public override bool IsActive()
        {
            return base.IsActive() && _content != null;
        }

        private void EnsureLayoutHasRebuilt()
        {
            if (!m_HasRebuiltLayout && !CanvasUpdateRegistry.IsRebuildingLayout())
                Canvas.ForceUpdateCanvases();
        }

        /// <summary>
        /// Sets the velocity to zero on both axes so the content stops moving.
        /// </summary>
        public virtual void StopMovement()
        {
            m_Velocity = Vector2.zero;
        }

        public virtual void OnScroll(PointerEventData data)
        {
            if (!IsActive())
                return;

            EnsureLayoutHasRebuilt();
            UpdateBounds();

            Vector2 delta = data.scrollDelta;
            // Down is positive for scroll events, while in UI system up is positive.
            delta.y *= -1;

            if (!_horizontal) { delta.x = 0f; }
            if (!_vertical) { delta.y = 0f; }

            if (data.IsScrolling())
                m_Scrolling = true;

            Vector2 position = _content.anchoredPosition;
            position += delta * _scrollSensitivity;

            m_Velocity = Vector2.zero;

            SetContentAnchoredPosition(position);
            UpdateBounds();
        }

        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            m_Velocity = Vector2.zero;
        }

        /// <summary>
        /// Handling for when the content is beging being dragged.
        /// </summary>
        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;

            UpdateBounds();

            m_PointerStartLocalCursor = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position, eventData.pressEventCamera, out m_PointerStartLocalCursor);
            m_ContentStartPosition = _content.anchoredPosition;
            m_Dragging = true;
        }

        /// <summary>
        /// Handling for when the content has finished being dragged.
        /// </summary>
        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            m_Dragging = false;
        }

        /// <summary>
        /// Handling for when the content is dragged.
        /// </summary>
        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!m_Dragging)
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;

            Vector2 localCursor;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position, eventData.pressEventCamera, out localCursor))
                return;

            UpdateBounds();

            var pointerDelta = localCursor - m_PointerStartLocalCursor;
            Vector2 position = m_ContentStartPosition + pointerDelta;

            // Offset to get content into place in the view.
            Vector2 offset = CalculateOffset(position - _content.anchoredPosition);
            position += offset;

            // elasticity
            if (offset.x != 0)
                position.x = position.x - RubberDelta(offset.x, _viewBounds.size.x);
            if (offset.y != 0)
                position.y = position.y - RubberDelta(offset.y, _viewBounds.size.y);
            

            SetContentAnchoredPosition(position);
        }

        /// <summary>
        /// Sets the anchored position of the content.
        /// </summary>
        protected virtual void SetContentAnchoredPosition(Vector2 position, bool notify = true)
        {
            if (!_horizontal)
                position.x = _content.anchoredPosition.x;
            if (!_vertical)
                position.y = _content.anchoredPosition.y;

            if (position != _content.anchoredPosition)
            {
                _content.anchoredPosition = position;
                UpdateBounds();
            }

            if (notify)
            {
                OnScrollPositionChanged?.Invoke(position);
            }
        }

        protected virtual void LateUpdate()
        {
            if (!_content)
                return;

            EnsureLayoutHasRebuilt();
            UpdateBounds();
            float deltaTime = Time.unscaledDeltaTime;
            Vector2 offset = CalculateOffset(Vector2.zero);
            if (deltaTime > 0.0f)
            {
                if (!m_Dragging && (offset != Vector2.zero || m_Velocity != Vector2.zero))
                {
                    Vector2 position = _content.anchoredPosition;
                    for (int axis = 0; axis < 2; axis++)
                    {
                        // Apply spring physics if movement is elastic and content has an offset from the view.
                        if (offset[axis] != 0)
                        {
                            float speed = m_Velocity[axis];
                            float smoothTime = _elasticity;
                            if (m_Scrolling)
                                smoothTime *= 3.0f;
                            position[axis] = Mathf.SmoothDamp(_content.anchoredPosition[axis], _content.anchoredPosition[axis] + offset[axis], ref speed, smoothTime, Mathf.Infinity, deltaTime);
                            if (Mathf.Abs(speed) < 1)
                                speed = 0;
                            m_Velocity[axis] = speed;
                        }
                        // Else move content according to velocity with deceleration applied.
                        else
                        {
                            m_Velocity[axis] *= Mathf.Pow(_decelerationRate, deltaTime);
                            if (Mathf.Abs(m_Velocity[axis]) < 1)
                                m_Velocity[axis] = 0;
                            position[axis] += m_Velocity[axis] * deltaTime;
                        }
                    }

                    SetContentAnchoredPosition(position);
                }

                if (m_Dragging)
                {
                    Vector3 newVelocity = (_content.anchoredPosition - m_PrevPosition) / deltaTime;
                    m_Velocity = Vector3.Lerp(m_Velocity, newVelocity, deltaTime * 10);
                }
            }

            if (_viewBounds != m_PrevViewBounds || _contentBounds != m_PrevContentBounds || _content.anchoredPosition != m_PrevPosition)
            {
                UpdateScrollbars(offset);
                UpdatePrevData();
            }
            m_Scrolling = false;
        }

        /// <summary>
        /// Helper function to update the previous data fields on a ScrollRect. Call this before you change data in the ScrollRect.
        /// </summary>
        protected void UpdatePrevData()
        {
            if (_content == null)
                m_PrevPosition = Vector2.zero;
            else
                m_PrevPosition = _content.anchoredPosition;
            m_PrevViewBounds = _viewBounds;
            m_PrevContentBounds = _contentBounds;
        }

        private void UpdateScrollbars(Vector2 offset)
        {
            if (_horizontalScrollbar)
            {
                if (_contentBounds.size.x > 0)
                    _horizontalScrollbar.size = Mathf.Clamp01((_viewBounds.size.x - Mathf.Abs(offset.x)) / _contentBounds.size.x);
                else
                    _horizontalScrollbar.size = 1;

                _horizontalScrollbar.value = horizontalNormalizedPosition;
            }

            if (_verticalScrollbar)
            {
                if (_contentBounds.size.y > 0)
                    _verticalScrollbar.size = Mathf.Clamp01((_viewBounds.size.y - Mathf.Abs(offset.y)) / _contentBounds.size.y);
                else
                    _verticalScrollbar.size = 1;

                _verticalScrollbar.value = verticalNormalizedPosition;
            }
        }

        /// <summary>
        /// The scroll position as a Vector2 between (0,0) and (1,1) with (0,0) being the lower left corner.
        /// </summary>
        public Vector2 normalizedPosition
        {
            get
            {
                return new Vector2(horizontalNormalizedPosition, verticalNormalizedPosition);
            }
            set
            {
                SetNormalizedPosition(value.x, 0);
                SetNormalizedPosition(value.y, 1);
            }
        }

        /// <summary>
        /// The horizontal scroll position as a value between 0 and 1, with 0 being at the left.
        /// </summary>
        public float horizontalNormalizedPosition
        {
            get
            {
                UpdateBounds();
                if ((_contentBounds.size.x <= _viewBounds.size.x) || Mathf.Approximately(_contentBounds.size.x, _viewBounds.size.x))
                    return (_viewBounds.min.x > _contentBounds.min.x) ? 1 : 0;
                return (_viewBounds.min.x - _contentBounds.min.x) / (_contentBounds.size.x - _viewBounds.size.x);
            }
            set
            {
                SetNormalizedPosition(value, 0);
            }
        }

        /// <summary>
        /// The vertical scroll position as a value between 0 and 1, with 0 being at the bottom.
        /// </summary>
        public float verticalNormalizedPosition
        {
            get
            {
                UpdateBounds();
                if ((_contentBounds.size.y <= _viewBounds.size.y) || Mathf.Approximately(_contentBounds.size.y, _viewBounds.size.y))
                    return (_viewBounds.min.y > _contentBounds.min.y) ? 1 : 0;

                return (_viewBounds.min.y - _contentBounds.min.y) / (_contentBounds.size.y - _viewBounds.size.y);
            }
            set
            {
                SetNormalizedPosition(value, 1);
            }
        }

        private void SetHorizontalNormalizedPosition(float value) { SetNormalizedPosition(value, 0); }
        private void SetVerticalNormalizedPosition(float value) { SetNormalizedPosition(value, 1); }

        /// <summary>
        /// Set the horizontal or vertical scroll position as a value between 0 and 1, with 0 being at the left or at the bottom.
        /// </summary>
        /// <param name="value">The position to set, between 0 and 1.</param>
        /// <param name="axis">The axis to set: 0 for horizontal, 1 for vertical.</param>
        protected virtual void SetNormalizedPosition(float value, int axis)
        {
            EnsureLayoutHasRebuilt();
            UpdateBounds();
            // How much the content is larger than the view.
            float hiddenLength = _contentBounds.size[axis] - _viewBounds.size[axis];
            // Where the position of the lower left corner of the content bounds should be, in the space of the view.
            float contentBoundsMinPosition = _viewBounds.min[axis] - value * hiddenLength;
            // The new content localPosition, in the space of the view.
            float newAnchoredPosition = _content.anchoredPosition[axis] + contentBoundsMinPosition - _contentBounds.min[axis];

            Vector3 anchoredPosition = _content.anchoredPosition;
            if (Mathf.Abs(anchoredPosition[axis] - newAnchoredPosition) > 0.01f)
            {
                anchoredPosition[axis] = newAnchoredPosition;
                _content.anchoredPosition = anchoredPosition;
                m_Velocity[axis] = 0;
                UpdateBounds();
            }
        }

        private static float RubberDelta(float overStretching, float viewSize)
        {
            return (1 - (1 / ((Mathf.Abs(overStretching) * 0.55f / viewSize) + 1))) * viewSize * Mathf.Sign(overStretching);
        }

        protected override void OnRectTransformDimensionsChange()
        {
            SetDirty();
        }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual void CalculateLayoutInputHorizontal() { }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual void CalculateLayoutInputVertical() { }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float minWidth { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float preferredWidth { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float flexibleWidth { get { return -1; } }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float minHeight { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float preferredHeight { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float flexibleHeight { get { return -1; } }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual int layoutPriority { get { return -1; } }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual void SetLayoutHorizontal()
        {
            m_Tracker.Clear();
            UpdateCachedData();
        }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual void SetLayoutVertical()
        {
            _viewBounds = new Bounds(_viewport.rect.center, _viewport.rect.size);
            _contentBounds = GetBounds();
        }

        /// <summary>
        /// Calculate the bounds the ScrollRect should be using.
        /// </summary>
        protected void UpdateBounds()
        {
            _viewBounds = new Bounds(_viewport.rect.center, _viewport.rect.size);
            _contentBounds = GetBounds();

            if (_content == null)
                return;

            Vector3 contentSize = _contentBounds.size;
            Vector3 contentPos = _contentBounds.center;
            var contentPivot = _content.pivot;
            AdjustBounds(ref _viewBounds, ref contentPivot, ref contentSize, ref contentPos);
            _contentBounds.size = contentSize;
            _contentBounds.center = contentPos;
        }

        internal static void AdjustBounds(ref Bounds viewBounds, ref Vector2 contentPivot, ref Vector3 contentSize, ref Vector3 contentPos)
        {
            // Make sure content bounds are at least as large as view by adding padding if not.
            // One might think at first that if the content is smaller than the view, scrolling should be allowed.
            // However, that's not how scroll views normally work.
            // Scrolling is *only* possible when content is *larger* than view.
            // We use the pivot of the content rect to decide in which directions the content bounds should be expanded.
            // E.g. if pivot is at top, bounds are expanded downwards.
            // This also works nicely when ContentSizeFitter is used on the content.
            Vector3 excess = viewBounds.size - contentSize;
            if (excess.x > 0)
            {
                contentPos.x -= excess.x * (contentPivot.x - 0.5f);
                contentSize.x = viewBounds.size.x;
            }
            if (excess.y > 0)
            {
                contentPos.y -= excess.y * (contentPivot.y - 0.5f);
                contentSize.y = viewBounds.size.y;
            }
        }

        private readonly Vector3[] m_Corners = new Vector3[4];
        private Bounds GetBounds()
        {
            if (_content == null)
                return new Bounds();
            _content.GetWorldCorners(m_Corners);
            var viewWorldToLocalMatrix = _viewport.worldToLocalMatrix;
            return InternalGetBounds(m_Corners, ref viewWorldToLocalMatrix);
        }

        internal static Bounds InternalGetBounds(Vector3[] corners, ref Matrix4x4 viewWorldToLocalMatrix)
        {
            var vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int j = 0; j < 4; j++)
            {
                Vector3 v = viewWorldToLocalMatrix.MultiplyPoint3x4(corners[j]);
                vMin = Vector3.Min(v, vMin);
                vMax = Vector3.Max(v, vMax);
            }

            var bounds = new Bounds(vMin, Vector3.zero);
            bounds.Encapsulate(vMax);
            return bounds;
        }

        private Vector2 CalculateOffset(Vector2 delta)
        {
            return InternalCalculateOffset(ref _viewBounds, ref _contentBounds, _horizontal, _vertical, ref delta);
        }

        internal static Vector2 InternalCalculateOffset(ref Bounds viewBounds, ref Bounds contentBounds, bool horizontal, bool vertical, ref Vector2 delta)
        {
            Vector2 offset = Vector2.zero;

            Vector2 min = contentBounds.min;
            Vector2 max = contentBounds.max;

            // min/max offset extracted to check if approximately 0 and avoid recalculating layout every frame (case 1010178)

            if (horizontal)
            {
                min.x += delta.x;
                max.x += delta.x;

                float maxOffset = viewBounds.max.x - max.x;
                float minOffset = viewBounds.min.x - min.x;

                if (minOffset < -0.001f)
                    offset.x = minOffset;
                else if (maxOffset > 0.001f)
                    offset.x = maxOffset;
            }

            if (vertical)
            {
                min.y += delta.y;
                max.y += delta.y;

                float maxOffset = viewBounds.max.y - max.y;
                float minOffset = viewBounds.min.y - min.y;

                if (maxOffset > 0.001f)
                    offset.y = maxOffset;
                else if (minOffset < -0.001f)
                    offset.y = minOffset;
            }

            return offset;
        }

        /// <summary>
        /// Override to alter or add to the code that keeps the appearance of the scroll rect synced with its data.
        /// </summary>
        protected void SetDirty()
        {
            if (!IsActive())
                return;

            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        /// <summary>
        /// Override to alter or add to the code that caches data to avoid repeated heavy operations.
        /// </summary>
        protected void SetDirtyCaching()
        {
            if (!IsActive())
                return;

            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            m_Velocity = Vector2.zero;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            SetDirtyCaching();
        }

        
#endif
    }
}
