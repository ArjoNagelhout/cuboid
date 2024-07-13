using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace Cuboid.UI
{
    public interface IData<T>
    {
        public T Data { get; set; }
    }

    public class ScrollViewPool : MonoBehaviour, INavigationStackView
    {
        [SerializeField] public ScrollView ScrollView;
        [SerializeField] private RectTransform _contentRect;
        [SerializeField] private RectTransform _viewportRect;
        private IScrollViewPoolInternal _pool;

        [Serializable]
        public class GridLayout : Layout
        {
            // add additional data specific to grid layout
        }

        [Serializable]
        public class ListLayout : Layout
        {
            // add additional data specific to list layout
        }

        [Serializable]
        public class Layout
        {
            /// <summary>
            /// Horizontal and vertical spacing between items
            /// </summary>
            public Vector2 Spacing = Vector2.zero;

            /// <summary>
            /// Padding around all items in the layout,
            /// order: left, right, top, bottom
            /// </summary>
            public Padding Padding = new Padding();

            /// <summary>
            /// Item Size
            /// </summary>
            public Vector2 ItemSize = Vector2.zero;
        }

        [Serializable]
        public struct Padding
        {
            public float Left;
            public float Right;
            public float Top;
            public float Bottom;

            public float Y => Top + Bottom;
            public float X => Left + Right;
        }

        /// <summary>
        /// Creates the pool and returns it
        /// </summary>
        public ScrollViewPoolInternal<T> CreatePool<T>(ScrollViewPoolInternal<T>.Data data)
        {
            if (_pool != null)
            {
                // destroy the old pool
                _pool.Dispose();
            }
            
            ScrollViewPoolInternal<T> pool = new ScrollViewPoolInternal<T>(
                data, ScrollView, _contentRect, _viewportRect, _stackViewState);

            // store the pool
            _pool = pool;

            return pool;
        }

        private void OnDestroy()
        {
            if (_pool != null)
            {
                _pool.Dispose();
            }
        }

        private void LateUpdate()
        {
            // call
            if (_pool != null)
            {
                _pool.LateUpdate();
            }
        }

        // to make sure that if the pool isn't instantiated yet, it will still propagate the events
        public enum StackViewState { None, Enable, Disable, WillEnable }
        private StackViewState _stackViewState = StackViewState.None;

        void INavigationStackView.OnDisable() { (_pool as INavigationStackView)?.OnDisable(); _stackViewState = StackViewState.Disable; }
        void INavigationStackView.OnEnable() { (_pool as INavigationStackView)?.OnEnable(); _stackViewState = StackViewState.Enable; }
        void INavigationStackView.OnWillEnable() { (_pool as INavigationStackView)?.OnWillEnable(); _stackViewState = StackViewState.WillEnable; }
        void INavigationStackView.OnClose() { (_pool as INavigationStackView)?.OnClose(); }

        public interface IScrollViewPoolInternal : IDisposable
        {
            public void LateUpdate();
        }

        /// <summary>
        /// Class similar to <see cref="AssetsPoolingGrid"/>, but then for 2D UI elements,
        /// in a list or a grid. 
        /// </summary>
        public class ScrollViewPoolInternal<T> : IScrollViewPoolInternal, INavigationStackView
        {
            private ScrollView _scrollView;
            private RectTransform _contentRect;
            private RectTransform _viewportRect;

            private Vector2 _viewportSize;

            private List<Cell> _cells = new List<Cell>();
            private Stack<Container> _containers = new Stack<Container>();

            private StackViewState _stackViewState = StackViewState.None;
            private bool _hasScaler = false;
            private float _scalerHeight = 0f;
            private float _scalerOffset = 0f;
            private float _maximumScale = 1f; // gets animated on INavigationStack enabled to 1, on disabled to 0
            private bool _enabled { set => OnEnabledChanged(value); }
            private PopupsController _popupsController;
            private Action<int> _onPopupsCountChanged;
            private Action<Vector2> _onScrollPositionChanged;
            private StoredBinding<Vector2> _storedScrollPosition;

            /// <summary>
            /// Get the first container from the stack
            /// </summary>
            internal Container Pop()
            {
                Container container = _containers.Pop();
                container.GameObject.SetActive(true);
                return container;
            }

            /// <summary>
            /// Put a container on the stack
            /// </summary>
            internal void Push(Container container)
            {
                container.GameObject.SetActive(false); // make sure when they're on the stack, they're not visible
                _containers.Push(container);
            }

            private void OnScrollPositionChanged(Vector2 position)
            {
                _storedScrollPosition.Value = position;
            }

            public ScrollViewPoolInternal(
                Data data,
                ScrollView scrollView,
                RectTransform contentRect,
                RectTransform viewportRect,
                StackViewState stackViewState)
            {
                _scrollView = scrollView;
                _contentRect = contentRect;
                _viewportRect = viewportRect;

                // first get the size
                _viewportSize = _viewportRect.rect.size;

                // make sure the on enable and on disable gets used from the navigation stack view
                _stackViewState = stackViewState;

                _popupsController = PopupsController.Instance;
                _onPopupsCountChanged = OnPopupsCountChanged;

                // then set the data
                ActiveData = data;
            }

            /// <summary>
            /// Data representation of an item in the pool. 
            /// </summary>
            public class Cell
            {
                private ScrollViewPoolInternal<T> _scrollViewPool;

                public Container AttachedContainer;

                public Cell(ScrollViewPoolInternal<T> scrollViewPool, T data, Vector2 position)
                {
                    _scrollViewPool = scrollViewPool;
                    Data = data;
                    Position = position;
                }

                private T _data;
                public T Data
                {
                    get => _data;
                    set
                    {
                        _data = value;
                        OnDataChanged(_data);
                    }
                }

                private Vector2 _position;
                public Vector2 Position
                {
                    get => _position;
                    set
                    {
                        _position = value;
                        OnPositionChanged(_position);
                    }
                }

                private bool _visible;
                public bool Visible
                {
                    get => _visible;
                    set
                    {
                        // only update if it has actually changed
                        if (_visible == value) { return; }
                        _visible = value;
                        OnVisibleChanged(_visible);
                    }
                }

                private void OnDataChanged(T data)
                {
                    if (AttachedContainer == null) { return; }
                    AttachedContainer.Data.Data = data;
                }

                private void OnPositionChanged(Vector2 position)
                {
                    // only update if it has an attached container
                    if (AttachedContainer == null) { return; }

                    // set the anchored position of the container
                    AttachedContainer.RectTransform.anchoredPosition = position;
                }

                private void OnVisibleChanged(bool visible)
                {
                    if (visible)
                    {
                        // now we try to get a container from the stack,
                        // populate it and set its position.

                        // if there are no more containers, don't try to get a container
                        if (_scrollViewPool._containers.Count == 0)
                        {
                            Debug.LogError("No containers anymore");

                            // this should never happen, but it's better than having an empty cell
                            // I suppose it could help when the view gets resized somehow. Currently not being done but you never know^TM
                            _scrollViewPool.InstantiateContainer(_scrollViewPool.ActiveData);
                        }

                        Container container = _scrollViewPool.Pop();

                        if (container == null) { Debug.LogError("Container is null"); return; }

                        // set _attachedContainer
                        AttachedContainer = container;

                        // set its data
                        AttachedContainer.Data.Data = Data;
                        OnPositionChanged(Position); // and position
                    }
                    else
                    {
                        // put the container back on the stack, if we have one
                        if (AttachedContainer == null) { return; }

                        _scrollViewPool.Push(AttachedContainer);

                        AttachedContainer = null;
                    }
                }
            }

            /// <summary>
            /// Container that can be reused. Data can be set. 
            /// </summary>
            public class Container
            {
                public GameObject GameObject;
                public RectTransform RectTransform;
                public ScrollViewScaler Scaler;
                public IData<T> Data;
            }

            public class Data
            {
                /// <summary>
                /// Used for instantiating the items and
                /// calculating the height of the container.
                ///
                /// Prefab should implement <see cref="IData{T}"/> interface. 
                /// </summary>
                public GameObject Prefab;

                /// <summary>
                /// Each item
                /// </summary>
                public List<T> Values;

                /// <summary>
                /// Whether to render the items in a grid or a list,
                /// and their 
                /// </summary>
                public Layout Layout;

                /// <summary>
                /// The identifier used for storing the scroll position
                /// </summary>
                public string Identifier;
            }

            private Data _activeData;
            public Data ActiveData
            {
                get => _activeData;
                set
                {
                    _activeData = value;
                    OnDataChanged(_activeData);
                }
            }

            private float CalculateContentHeight(Vector2 viewportSize, int itemCount, Layout layout)
            {
                if (itemCount == 0) { return 0; }

                switch (layout)
                {
                    case GridLayout:
                        return CalculateContentHeightGrid(viewportSize, itemCount, layout);
                    case ListLayout:
                        return CalculateContentHeightList(itemCount, layout);
                }

                return 0;
            }

            private float CalculateContentHeightList(int itemCount, Layout layout)
            {
                return (itemCount * (layout.ItemSize.y + layout.Spacing.y)) - layout.Spacing.y + layout.Padding.Y;
            }

            private float CalculateContentHeightGrid(Vector2 viewportSize, int itemCount, Layout layout)
            {
                int columns = GetColumnsInViewport(layout);
                int rows = Mathf.CeilToInt((float)itemCount / columns);

                // use the list height formula, but then with rows supplied as itemCount
                return CalculateContentHeightList(rows, layout);
            }

            private Vector2 CalculateCellPosition(int index, Layout layout)
            {
                switch (layout)
                {
                    case GridLayout:
                        return CalculateCellPositionGrid(index, layout as GridLayout);
                    case ListLayout:
                        return CalculateCellPositionList(index, layout as ListLayout);
                }

                return Vector2.zero;
            }

            private Vector2 CalculateCellPositionList(int index, ListLayout layout)
            {
                return GetPosition(0, index, layout);
            }

            private Vector2 CalculateCellPositionGrid(int index, GridLayout layout)
            {
                int columns = GetColumnsInViewport(layout);
                int xIndex = index % columns;
                int yIndex = Mathf.FloorToInt(index / (float)columns);

                return GetPosition(xIndex, yIndex, layout);
            }

            private Vector2 GetPosition(int xIndex, int yIndex, Layout layout)
            {
                float x = xIndex * (layout.ItemSize.x + layout.Spacing.x);
                float y = -yIndex * (layout.ItemSize.y + layout.Spacing.y); // negate y position because y is up

                return new Vector2(x, y);
            }

            private int CalculatePoolSize(int itemCount, Layout layout)
            {
                // calculates the pool size based on how many items could fit in
                // the view (_viewportSize)
                int poolSize = 0;
                switch (layout)
                {
                    case GridLayout:
                        poolSize = CalculatePoolSize(layout as GridLayout);
                        break;
                    case ListLayout:
                        poolSize = CalculatePoolSize(layout as ListLayout);
                        break;
                }
                // make sure it won't calculate a bigger pool size than
                // total items currently in data.Values
                if (poolSize > itemCount)
                {
                    poolSize = itemCount;
                }

                return poolSize;
            }

            private int CalculatePoolSize(ListLayout layout)
            {
                // determine how many rows would fit inside the viewport rect
                return GetRowsInViewport(layout) + 1;
            }

            private int CalculatePoolSize(GridLayout layout)
            {
                int columns = GetColumnsInViewport(layout);
                int rows = GetRowsInViewport(layout) + 1;

                return columns * rows;
            }

            private int GetRowsInViewport(Layout layout)
            {
                // add one row because there is always an "in-between" state
                return Mathf.CeilToInt(_viewportSize.y / (layout.ItemSize.y + layout.Spacing.y));
            }

            private int GetColumnsInViewport(Layout layout)
            {
                return Mathf.FloorToInt(_viewportSize.x / (layout.ItemSize.x + layout.Spacing.x));
            }

            /// <summary>
            /// Use to manually tell the pool that the data has changed
            /// </summary>
            public void DataChanged()
            {
                OnDataChanged(ActiveData);
            }

            private void OnDataChanged(Data data)
            {
                Clear();
                Unregister();

                // set the prefab recttransform values
                RectTransform prefabRectTransform = data.Prefab.GetComponent<RectTransform>();
                SetRectTransform(prefabRectTransform, data.Layout);

                // recalculate content rect size
                int count = data.Values != null ? data.Values.Count : 0;

                float contentHeight = CalculateContentHeight(_viewportSize, count, data.Layout);
                _contentRect.sizeDelta = new Vector2(_contentRect.sizeDelta.x, contentHeight);

                // create containers stack
                int poolSize = CalculatePoolSize(count, data.Layout);
                for (int i = 0; i < poolSize; i++)
                {
                    InstantiateContainer(data);
                }

                SetScalerData();

                // create cells
                for (int i = 0; i < count; i++)
                {
                    T value = data.Values[i];
                    Vector2 position = CalculateCellPosition(i, data.Layout);
                    Cell cell = new Cell(this, value, position);
                    _cells.Add(cell);
                }

                // fire on enable or on disable events based on the cached value of the ScrollViewPool component
                INavigationStackView stackView = this as INavigationStackView;
                switch (_stackViewState)
                {
                    case StackViewState.Enable:
                        stackView.OnEnable();
                        break;
                    case StackViewState.Disable:
                        stackView.OnDisable();
                        break;
                    case StackViewState.WillEnable:
                        stackView.OnWillEnable();
                        break;
                }

                Register();
            }

            private void SetScalerData()
            {
                _hasScaler = false;

                if (_containers == null || _containers.Count == 0) { return; }

                Container container = _containers.Peek();

                ScrollViewScaler scaler = container.Scaler;
                if (scaler == null) { return; }

                RectTransform rectTransform = scaler.RectTransform;

                _scalerOffset = -rectTransform.offsetMax.y;
                _scalerHeight = rectTransform.rect.height;
                _hasScaler = true;
            }

            private void InstantiateContainer(Data data)
            {
                // instantiate containers
                GameObject containerGameObject = Instantiate(data.Prefab, _contentRect, false);
                RectTransform containerRectTransform = containerGameObject.GetComponent<RectTransform>();
                IData<T> containerData = containerGameObject.GetComponent<IData<T>>();
                ScrollViewScaler scaler = containerGameObject.GetComponentInChildren<ScrollViewScaler>();
                Container container = new Container()
                {
                    GameObject = containerGameObject,
                    RectTransform = containerRectTransform,
                    Data = containerData,
                    Scaler = scaler
                };

                // add to the stack
                Push(container);
            }

            /// <summary>
            /// Destroys the cells and containers
            /// </summary>
            private void Clear()
            {
                foreach (Cell cell in _cells)
                {
                    if (cell == null ||
                        cell.AttachedContainer == null ||
                        cell.AttachedContainer.GameObject == null) { continue; }
                    Destroy(cell.AttachedContainer.GameObject);
                }
                _cells.Clear();

                foreach (Container container in _containers)
                {
                    Destroy(container.GameObject);
                }
                _containers.Clear();
            }

            private List<Cell> _cellsToScale = new List<Cell>();
            private List<Cell> _cellsToShow = new List<Cell>();
            private List<Cell> _cellsToHide = new List<Cell>();

            public void LateUpdate()
            {
                // set visibility of items
                if (_activeData == null || _activeData.Values == null || _activeData.Values.Count == 0) { return; }

                float top = _contentRect.anchoredPosition.y;
                float bottom = top + _viewportSize.y;

                _cellsToHide.Clear();
                _cellsToShow.Clear();

                if (_hasScaler)
                {
                    _cellsToScale.Clear();
                }

                foreach (Cell cell in _cells)
                {
                    float y = -cell.Position.y;

                    bool visible = y + ActiveData.Layout.ItemSize.y > top && y < bottom;
                    bool cellVisible = cell.Visible;

                    if (visible != cellVisible)
                    {
                        (visible ? _cellsToShow : _cellsToHide).Add(cell);
                    }

                    if (_hasScaler && visible)
                    {
                        _cellsToScale.Add(cell);
                    }
                }

                foreach (Cell cell in _cellsToHide) { cell.Visible = false; }
                foreach (Cell cell in _cellsToShow) { cell.Visible = true; }

                if (_hasScaler)
                {
                    foreach (Cell cell in _cellsToScale)
                    {
                        ScaleCell(cell, top, bottom);
                    }
                }
            }

            /// <summary>
            /// Scales the cell 
            /// </summary>
            private void ScaleCell(Cell cell, float top, float bottom)
            {
                ScrollViewScaler scaler = cell.AttachedContainer.Scaler;

                float scaleFactor = 1f;
                float yOffset = 0f;

                float y = -cell.Position.y + _scalerOffset;

                float distanceFromTop = top - y;
                float distanceFromBottom = y + _scalerHeight - bottom;

                if (distanceFromTop > _scalerHeight || distanceFromBottom > _scalerHeight)
                {
                    scaleFactor = 0;
                }
                else if (distanceFromTop > 0)
                {
                    scaleFactor -= distanceFromTop / _scalerHeight;
                    yOffset = -distanceFromTop / _scalerHeight / 2;
                }
                else if (distanceFromBottom > 0)
                {
                    scaleFactor -= distanceFromBottom / _scalerHeight;
                    yOffset = distanceFromBottom / _scalerHeight / 2;
                }
                scaler.SetScale(scaleFactor * _maximumScale, yOffset);
            }

            /// <summary>
            /// Function for setting the sizeDelta, pivot and anchors of the item to make sure
            /// layout is predictable and does not rely on setting the right values in the prefab.
            ///
            /// This also allows switching between grid and list layout
            /// </summary>
            private static void SetRectTransform(RectTransform rectTransform, Layout layout)
            {
                switch (layout)
                {
                    case GridLayout:
                        SetRectTransform(rectTransform, layout as GridLayout);
                        break;
                    case ListLayout:
                        SetRectTransform(rectTransform, layout as ListLayout);
                        break;
                }
            }

            private static void SetRectTransform(RectTransform r, GridLayout layout)
            {
                r.anchoredPosition = Vector2.zero;

                // don't stretch horizontally, nor vertically (top left)
                r.anchorMin = new Vector2(0, 1);
                r.anchorMax = new Vector2(0, 1);

                // pivot top left
                r.pivot = new Vector2(0, 1);

                // item size
                r.sizeDelta = layout.ItemSize;
            }

            private static void SetRectTransform(RectTransform r, ListLayout layout)
            {
                r.anchoredPosition = Vector2.zero;

                // stretch horizontally, but not vertically
                r.anchorMin = new Vector2(0, 1);
                r.anchorMax = new Vector2(1, 1);

                // pivot on top
                r.pivot = new Vector2(0, 1);

                // set the item height
                r.sizeDelta = new Vector2(0, layout.ItemSize.y);
            }

            void INavigationStackView.OnDisable() => _enabled = false;
            void INavigationStackView.OnEnable() => _enabled = true;
            void INavigationStackView.OnWillEnable() { _maximumScale = 0; _enabled = true; }
            void INavigationStackView.OnClose() { }

            private void OnEnabledChanged(bool enabled)
            {
                float target = enabled ? 1.0f : 0.0f;
                float duration = enabled ? NavigationStack.k_AnimateInDuration : NavigationStack.k_AnimateOutDuration;
                DOTween.To(() => _maximumScale, v => _maximumScale = v, target, duration)
                        .SetEase(Ease.OutQuart);
            }

            private void OnPopupsCountChanged(int count)
            {
                _enabled = count == 0;
            }

            private void Register()
            {
                // register for storing the scroll position
                if (ActiveData.Identifier != null)
                {
                    _storedScrollPosition = new(string.Join('-', ActiveData.Identifier, nameof(_storedScrollPosition)), Vector2.zero);
                    _onScrollPositionChanged = OnScrollPositionChanged;
                    _scrollView.OnScrollPositionChanged += _onScrollPositionChanged;
                    _scrollView.SetScrollPosition(_storedScrollPosition.Value);
                }

                // register for popups count
                if (_popupsController != null)
                {
                    _popupsController.PopupsCount.Register(_onPopupsCountChanged);
                }
            }

            private void Unregister()
            {
                if (_onScrollPositionChanged != null)
                {
                    _scrollView.OnScrollPositionChanged -= _onScrollPositionChanged;
                    _onScrollPositionChanged = null;
                }

                if (_popupsController != null)
                {
                    _popupsController.PopupsCount.Unregister(_onPopupsCountChanged);
                }
            }

            public void Dispose()
            {
                // unregister any events or actions
                Unregister();
            }
        }
    }
}
