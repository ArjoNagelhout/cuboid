// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

namespace Cuboid.UI
{
    public interface INavigationStackView
    {
        /// <summary>
        /// Gets called when the close animation starts,
        /// or when the open animation starts
        ///
        /// disable all interactable elements
        /// </summary>
        public void OnDisable();

        /// <summary>
        /// Gets called when the open animation is finished,
        ///
        /// enable all interactable elements
        /// </summary>
        public void OnEnable();

        /// <summary>
        /// Gets called when this view is closed on the navigation stack
        /// </summary>
        public void OnClose();

        /// <summary>
        /// Gets called when the open animation is started. 
        /// </summary>
        public void OnWillEnable();
    }

    /// <summary>
    /// Animatable stack of views
    /// </summary>
    public sealed class NavigationStack : MonoBehaviour
    {
        /// <summary>
        /// Cached data for the instantiated container and view
        /// </summary>
        public class ViewData
        {
            public string Title;

            public INavigationStackView[] Views;

            public GameObject ContainerGameObject;

            public RectTransform ContainerRectTransform;

            public GameObject GameObject;

            public RectTransform RectTransform;

            public Action OnLoaded;

            internal List<Graphic> _disabledRaycastTargetGraphics;
            internal ScrollView _disabledScrollView;
        }

        [Header("Values")]
        /// <summary>
        /// The Z offset that should be used between panels
        /// </summary>
        [SerializeField] private float _zOffset;
        public const float k_AnimateInDuration = 0.3f;
        public const float k_AnimateOutDuration = 0.3f;

        // component references
        [Header("Component References")]
        [SerializeField] private Button _backButton;
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private RectTransform _container;

        // stack of views
        [NonSerialized] public Binding<List<ViewData>> Views = new(new());

        private void Awake()
        {
        }

        private void Start()
        {
            Views.OnValueChanged += OnViewsChanged;
        }

        private void OnViewsChanged(List<ViewData> views)
        {
            // set back button and title appearance
            bool backButtonVisible = false;
            string backButtonText = null;
            string title = null;

            if (views.Count >= 1)
            {
                ViewData topView = views[views.Count - 1];
                title = topView.Title;
            }
            if (views.Count >= 2)
            {
                ViewData previousView = views[views.Count - 2];
                backButtonVisible = true;
                backButtonText = previousView.Title;
            }

            // set values
            _backButton.gameObject.SetActive(backButtonVisible);
            _backButton.ActiveData.Text = backButtonText;
            _backButton.DataChanged();
            _title.text = title;
        }

        /// <summary>
        /// Opens a view and returns the component that is attached to it of type T
        /// </summary>
        public T OpenView<T>(GameObject prefab, string title, bool animated = true, Action onLoaded = null)
        {
            if (prefab == null)
            {
                Debug.LogError($"{nameof(prefab)} is null");
                return default;
            }

            // instantiate the container
            GameObject containerGameObject = InstantiateContainer($"Container_{title}", _container, out RectTransform containerRectTransform);

            // instantiate the view itself
            GameObject viewGameObject = Instantiate(prefab, containerRectTransform, false);
            RectTransform viewRectTransform = viewGameObject.GetComponent<RectTransform>();

            // fill to its parent rect transform
            viewRectTransform.Fill();
             
            // get components
            T component = viewGameObject.GetComponent<T>();
            INavigationStackView[] views = viewGameObject.GetComponentsInChildren<INavigationStackView>();

            ViewData data = new ViewData()
            {
                Title = title,
                GameObject = viewGameObject,
                RectTransform = viewRectTransform,
                Views = views,
                ContainerGameObject = containerGameObject,
                ContainerRectTransform = containerRectTransform
            };

            // disable previous view, if any
            ViewData lastView = LastView;
            
            if (lastView != null)
            {
                DisableView(lastView);
            }

            // store the view
            Views.Value.Add(data);
            Views.ValueChanged();

            if (animated && Views.Value.Count > 0)
            {
                // animate the view
                AnimateViewIn(data);
            }

            SetContainersZOffset(k_AnimateInDuration, animated);

            StartCoroutine(OnLoaded(onLoaded));

            return component;
        }

        private IEnumerator OnLoaded(Action onLoaded)
        {
            // Hackiest piece of shit code ever. But we need to launch.
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            onLoaded?.Invoke();
        }

        /// <summary>
        /// Instantiates the container with a mask. 
        /// </summary>
        private GameObject InstantiateContainer(string name, RectTransform parent, out RectTransform rectTransform)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.layer = Layers.UI.layer;
            rectTransform = gameObject.AddComponent<RectTransform>();
            gameObject.AddComponent<Image>();

            Mask mask = gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            rectTransform.SetParent(parent, false);
            rectTransform.Fill();

            return gameObject;
        }

        private void AnimateViewIn(ViewData data)
        {
            DisableView(data);
            WillEnableView(data);

            float width = data.RectTransform.rect.width;
            data.RectTransform.anchoredPosition = data.RectTransform.anchoredPosition.SetX(width);
            data.RectTransform.DOAnchorPosX(0f, k_AnimateInDuration)
                .SetEase(Ease.OutQuart)
                .OnComplete(() => EnableView(data));
        }

        private void AnimateViewOut(ViewData data, Action onComplete)
        {
            DisableView(data);

            float width = data.RectTransform.rect.width;

            data.RectTransform.DOKill();
            data.RectTransform.DOAnchorPosX(width, k_AnimateOutDuration)
                .SetEase(Ease.OutQuart)
                .OnComplete(() => onComplete?.Invoke());

            SetContainersZOffset(k_AnimateOutDuration);
        }

        private void CloseViewsAboveAndIncluding(INavigationStackView view)
        {
            int index = Views.Value.FindIndex(data => data.Views.Contains(view));
            if (index == -1)
            {
                Debug.LogError($"{nameof(Views)} does not contain view {nameof(view)}");
                return;
            }

            // close all views up until the current index (all higher indices get closed)
            int count = Views.Value.Count - 1;
            for (int i = index; i < count; i++)
            {
                // only animate the top one, all ones below should be destroyed immediately
                ViewData data = Views.Value[i];

                DisableView(data);
                CloseView(data);
            }

            Views.Value.RemoveRange(index, count - index);
            Views.ValueChanged();

            CloseTopViewAnimated();
        }

        private void CloseTopViewAnimated()
        {
            if (Views.Value.Count == 0)
            {
                Debug.LogError($"{nameof(Views)} does not contain any views");
                return;
            }

            int topViewIndex = Views.Value.Count - 1;
            ViewData topView = Views.Value[topViewIndex];

            Views.Value.RemoveAt(topViewIndex);
            Views.ValueChanged();

            AnimateViewOut(topView, () =>
            {
                CloseView(topView);
            });

            // set previous view enabled
            ViewData lastView = LastView;
            if (lastView != null) { WillEnableView(lastView); EnableView(lastView); }
        }

        public void Back()
        {
            CloseTopViewAnimated();
        }

        private void SetContainersZOffset(float duration, bool animated = true)
        {
            int count = Views.Value.Count;
            for (int i = 0; i < count; i++)
            {
                ViewData data = Views.Value[i];
                RectTransform r = data.ContainerRectTransform;

                // kill all previously running animations
                r.DOKill();

                // now start the animation to the target z offset
                int index = count - 1 - i;
                float target = _zOffset * index;

                if (animated)
                {
                    r.DOLocalMoveZ(target, duration);
                }
                else
                {
                    r.localPosition = r.localPosition.SetZ(target);
                }
            }
        }

        private void EnableView(ViewData data)
        {
            if (data == null || data.Views == null) { return; }

            if (data._disabledScrollView != null)
            {
                data._disabledScrollView.enabled = true;
            }

            if (data._disabledRaycastTargetGraphics != null)
            {
                foreach (Graphic graphic in data._disabledRaycastTargetGraphics)
                {
                    graphic.raycastTarget = true;
                }
            }

            foreach (INavigationStackView view in data.Views)
            {
                view.OnEnable();
            }
        }

        private void CloseView(ViewData data)
        {
            if (data == null || data.Views == null) { return; }

            foreach (INavigationStackView view in data.Views)
            {
                view.OnClose();
            }

            Destroy(data.ContainerGameObject);
        }

        private void WillEnableView(ViewData data)
        {
            if (data == null || data.Views == null) { return; }

            foreach (INavigationStackView view in data.Views)
            {
                view.OnWillEnable();
            }
        }

        private void DisableView(ViewData data)
        {
            if (data == null || data.Views == null) { return; }

            // disable scroll view
            ScrollView scrollView = data.GameObject.GetComponent<ScrollView>();
            if (scrollView != null && scrollView.enabled)
            {
                data._disabledScrollView = scrollView;
                scrollView.enabled = false;
            }

            // disable graphic raycast targets in children
            Graphic[] graphics = data.GameObject.GetComponentsInChildren<Graphic>();
            data._disabledRaycastTargetGraphics = new List<Graphic>();

            foreach (Graphic graphic in graphics)
            {
                // Don't disable the panel itself, just its child components
                if (graphic.rectTransform == data.RectTransform) { continue; }

                if (graphic.raycastTarget)
                {
                    data._disabledRaycastTargetGraphics.Add(graphic);
                    graphic.raycastTarget = false;
                }
            }

            foreach (INavigationStackView view in data.Views)
            {
                view.OnDisable();
            }
        }

        private ViewData LastView
        {
            get
            {
                List<ViewData> data = Views.Value;
                if (data == null || data.Count == 0) { return null; }

                return data[data.Count - 1];
            }
        }

    }
}
