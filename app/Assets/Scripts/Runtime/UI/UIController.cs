// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cuboid.Input;
using Cuboid.Utils;
using System.Threading.Tasks;
using UnityEngine.UI;
using System.Linq;
using DG.Tweening;

namespace Cuboid.UI
{
    /// <summary>
    /// The UIController is responsible for instantiating menu panels, such as
    /// the Assets, Documents and Settings panel.
    ///
    /// It also is responsible for instantiating the Dock. 
    /// </summary>
    public class UIController : MonoBehaviour
    {
        private Action<bool> _onApplicationFocusChanged;
        private App _app;

        public enum Panel
        {
            /// <summary>
            /// Assets Library Panel
            /// (look through assets and drag and drop them into the scene)
            /// </summary>
            Assets = 0,

            /// <summary>
            /// Documents Panel
            /// (select the active panel, share with people)
            /// </summary>
            Documents,

            /// <summary>
            /// Settings panel
            /// (login, set handedness)
            /// </summary>
            Settings,

            /// <summary>
            /// Properties panel for the selected object
            /// </summary>
            Properties,

            /// <summary>
            /// Tools panel
            /// </summary>
            Tools,

            /// <summary>
            /// Colors panel
            /// </summary>
            Colors,

            /// <summary>
            /// Cuboid credits panel
            /// </summary>
            Credits
        }

        // Singleton implementation
        private static UIController _instance;
        public static UIController Instance => _instance;

        [NonSerialized] public StoredBinding<Panel> ActivePanel;

        [Header("Main")]
        [SerializeField] private float _dockOffset = 0.0f;
        [SerializeField] private float _mainOffset = 0.0f;

        [SerializeField] private GameObject _dockPrefab;
        [SerializeField] private GameObject _mainPrefab;

        private Transform _leftMenuTransform;
        private Transform _rightMenuTransform;

        [SerializeField] private PrefabDictionary<Panel> _panelPrefabs = new PrefabDictionary<Panel>();

        private InputController _inputController;

        private GameObject _instantiatedDock;
        private GameObject _instantiatedMain;

        public GameObject InstantiatedDock => _instantiatedDock;
        public GameObject InstantiatedMain => _instantiatedMain;

        private Canvas _dockCanvas;
        private Canvas _mainCanvas;

        /// <summary>
        /// The panel that has been instantiated, so that it can be removed on active panel change
        /// </summary>
        private GameObject _instantiatedPanel = null;

        private Action<Handedness.Hand> _onDominantHandChanged;

        [SerializeField] private float _menuTransformAnimationInDuration = 0.3f;
        [SerializeField] private float _menuTransformAnimationOutDuration = 0.3f;

        private void Awake()
        {
            // Singleton implementation
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }

            ActivePanel = new("ActivePanel", Panel.Assets);
        }
        
        private void Start()
        {
            _app = App.Instance;
            _inputController = InputController.Instance;

            _leftMenuTransform = _inputController.LeftHandXRController.MenuTransform;
            _rightMenuTransform = _inputController.RightHandXRController.MenuTransform;

            // Make sure the menu is hidden / shown when the application focus is lost or regained by the OVRManager,
            // as per VRC.Quest.Input.4 (https://developer.oculus.com/resources/vrc-quest-input-4/)
            _onApplicationFocusChanged = (focus) =>
            {
                if (_instantiatedDock != null) { _instantiatedDock.SetActive(focus); }
                if (_instantiatedMain != null) { _instantiatedMain.SetActive(focus); }
            };

            _onDominantHandChanged = OnDominantHandChanged;

            // Instantiate UI
            _instantiatedDock = Instantiate(_dockPrefab, transform);
            _instantiatedMain = Instantiate(_mainPrefab, transform);

            _dockCanvas = _instantiatedDock.GetComponent<Canvas>();
            _mainCanvas = _instantiatedMain.GetComponent<Canvas>();

            ActivePanel.Register(OnActivePanelChanged);

            Register();
        }

        private void OnActivePanelChanged(Panel panel)
        {
            // Remove the previous panel if it's still active
            if (_instantiatedPanel != null)
            {
                Destroy(_instantiatedPanel);
                _instantiatedPanel = null;
            }

            _panelPrefabs.InstantiateAsync(panel, _instantiatedMain.transform, (result) =>
            {
                _instantiatedPanel = result;
            });
        }

        /// <summary>
        /// Set the new offsets and reparent the instantiated menu UI to the dominant hand
        /// </summary>
        private void OnDominantHandChanged(Handedness.Hand hand)
        {
            AttachUI(hand, _instantiatedMain, _mainOffset);
            AttachUI(hand, _instantiatedDock, _dockOffset);
        }
        
        /// <summary>
        /// Attaches a specific UI element with a given offset to the non dominant hand of the
        /// user. (For menu panels)
        /// </summary>
        /// <param name="instantiatedUI"></param>
        /// <param name="offset"></param>
        private void AttachUI(Handedness.Hand hand, GameObject instantiatedUI, float offset)
        {
            if (instantiatedUI == null) { return; }
            // attach to the opposite hand
            Transform menuTransform = hand == Handedness.Hand.LeftHand ? _rightMenuTransform : _leftMenuTransform;
            instantiatedUI.transform.SetParent(menuTransform, false);

            // If left handed, menu is attached to the right hand, offset should be to the left (negative)
            float offsetMultiplier = hand == Handedness.Hand.LeftHand ? -1f : 1f;
            instantiatedUI.transform.localPosition = new Vector3(offset * offsetMultiplier, 0, 0);
        }

        private ScrollView[] _tempScrollViews = new ScrollView[0];

        private bool _uiVisible = true;
        /// <summary>
        /// Whether to show or hide the UI
        /// </summary>
        public bool UIVisible
        {
            get => _uiVisible;
            set
            {
                if (_uiVisible == value) { return; } // don't change
                _uiVisible = value;

                void AnimateIn(Transform menuTransform, Action onComplete = null)
                {
                    menuTransform.DOKill();
                    menuTransform.DOScale(1.0f, _menuTransformAnimationInDuration)
                        .SetEase(Ease.OutBack, 1.2f)
                        .OnComplete(() => { onComplete?.Invoke(); });
                }

                void AnimateOut(Transform menuTransform, Action onComplete = null)
                {
                    menuTransform.DOKill();
                    menuTransform.DOScale(0.0f, _menuTransformAnimationOutDuration)
                        .SetEase(Ease.OutQuart)
                        .OnComplete(() => { onComplete?.Invoke(); });
                }

                if (_uiVisible)
                {
                    // enable the canvas, and animate to 1 (pop in)
                    _dockCanvas.enabled = true;
                    _mainCanvas.enabled = true;

                    AnimateIn(_leftMenuTransform);
                    AnimateIn(_rightMenuTransform, () =>
                    {
                        foreach (ScrollView _ in _tempScrollViews)
                        {
                            _.enabled = true;
                        }
                    });
                }
                else
                {
                    // animate to zero and disable the canvas (pop out)

                    // HACK: Fucking unity, why does the scroll view go all the way down if only the scale of the canvas changes.
                    // Shouldn't be necessary. 
                    _tempScrollViews = FindObjectsOfType<ScrollView>();
                    foreach (ScrollView _ in _tempScrollViews)
                    {
                        _.enabled = false;
                    }

                    AnimateOut(_leftMenuTransform);
                    AnimateOut(_rightMenuTransform, () =>
                    {
                        _dockCanvas.enabled = false;
                        _mainCanvas.enabled = false;
                    });
                }

            }
        }

        #region Colors

        [Header("UI components")]
        public ButtonColors Colors = new ButtonColors();

        [SerializeField] private GameObject _buttonPrefab;

        public Button InstantiateButton(Transform parent, Button.Data data)
        {
            GameObject buttonGameObject = Instantiate(_buttonPrefab, parent, false);
            Button button = buttonGameObject.GetComponent<Button>();
            button.ActiveData = data;
            return button;
        }

        #endregion

        #region Icons

        [Header("Icons")]
        public IconsScriptableObject Icons;

        #endregion

        #region Action registration

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
            if (_inputController != null)
            {
                _inputController.Handedness.DominantHand.Register(_onDominantHandChanged);
            }

            if (_app != null)
            {
                _app.OnApplicationFocusChanged += _onApplicationFocusChanged;
            }
        }

        private void Unregister()
        {
            if (_inputController != null)
            {
                _inputController.Handedness.DominantHand.Unregister(_onDominantHandChanged);
            }

            if (_app != null)
            {
                _app.OnApplicationFocusChanged -= _onApplicationFocusChanged;
            }
        }

        #endregion
    }
}
