//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using Cuboid.Input;
using System.Linq;
using UnityEngine;
using static Cuboid.Input.SpatialInputModule;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

namespace Cuboid.UI
{
    /// <summary>
    /// Responsible for opening and closing popups in the UI
    /// </summary>
    public sealed class PopupsController : MonoBehaviour
    {
        // Singleton implementation
        private static PopupsController _instance;
        public static PopupsController Instance => _instance;

        private SpatialInputModule _spatialInputModule;
        private SpatialInputModule.InterceptPointerCurrentRaycast _interceptPointerCurrentRaycast;

        private UIController _uiController;
        private Action<UIController.Panel> _onActivePanelChanged;

        [SerializeField] private Color _popupUnderlayColor;

        [SerializeField] private float _popupFlashDuration = 0.1f;
        [SerializeField] private float _popupFlashScale = 1.05f;
        [SerializeField] internal float _popupEnterStartScale = 0.95f;
        [SerializeField] internal float _popupEnterDuration = 0.2f;
        [SerializeField] private float _popupEnterZOffset = 20f;
        
        [SerializeField] internal float _popupExitDuration = 0.1f;
        [SerializeField] internal Vector3 _popupExitScale = Vector3.zero;

        /// <summary>
        /// The prefab that will be used for instantiating the context menu. 
        /// </summary>
        [Header("Popups")]
        [SerializeField] private GameObject _contextMenuPrefab;
        [SerializeField] private GameObject _dialogBoxPrefab;
        [SerializeField] private GameObject _inputTextPrefab;

        public GameObject TooltipPopupPrefab;

        public GameObject FullKeyboardPrefab;
        public GameObject NumericKeyboardPopupPrefab;

        /// <summary>
        /// The prefab that should be instantiated on top of the entire panel 
        /// </summary>
        [SerializeField] private GameObject _popupUnderlayPrefab;

        /// <summary>
        /// List of popups that are stacked on top of each other in the menu.
        /// Only the upper most one is visible. 
        /// </summary>
        internal List<PopupData> _popups = new List<PopupData>();

        public Binding<int> PopupsCount = new();

        internal Transform _popupTransform => _uiController.InstantiatedMain.transform;

        public class PopupParams
        {
            /// <summary>
            /// If this flag is enabled, the <see cref="OnClickedOutsidePopup"/> method
            /// will not "flash" the popup, but rather close it.
            /// </summary>
            public bool ClickingOutsidePopupCloses = true;

            /// <summary>
            /// This is for popups that change values of objects
            /// </summary>
            public bool DontInterceptUndoButtons = false;

            /// <summary>
            /// e.g. for a keyboard, so that the user can type with two hands.
            /// This would then hide the UI, but that doesn't need to be handled by the Popup controller itself?
            /// Or maybe it does...
            /// Hmm...
            /// </summary>
            public bool WorldSpace = false;

            internal static PopupParams _defaultPopupParams = new PopupParams();
        }

        /// <summary>
        /// internal data class for each popup that has been instantiated, this allows us
        /// to store additional data per popup that have been set via the
        /// <see cref="OpenPopup{T}(GameObject, bool)"/> method. 
        /// </summary>
        internal class PopupData
        {
            public GameObject InstantiatedPopup;

            /// <summary>
            /// The underlay that gets placed on the popup below, and destroyed when this popup is destroyed
            /// </summary>
            public GameObject InstantiatedUnderlay;

            /// <summary>
            /// Used for animating the image opacity from original to 0 when popup gets closed
            /// </summary>
            internal Image _underlayImage;

            private PopupParams _params;

            internal RectTransform _rectTransform;

            internal bool _instantiatedAsWorldSpacePopup;

            /// <summary>
            /// Returns set params, or default if null
            /// </summary>
            public PopupParams Params => _params == null ? PopupParams._defaultPopupParams : _params;

            public PopupData(
                GameObject instantiatedPopup,
                GameObject instantiatedUnderlay,
                Image underlayImage,
                bool instantiatedAsWorldSpacePopup = false,
                PopupParams popupParams = null)
            {
                InstantiatedPopup = instantiatedPopup;
                InstantiatedUnderlay = instantiatedUnderlay;
                _underlayImage = underlayImage;
                _instantiatedAsWorldSpacePopup = instantiatedAsWorldSpacePopup;
                _params = popupParams;
                _rectTransform = InstantiatedPopup.GetComponent<RectTransform>();
            }
        }

        [SerializeField] private float _zDistanceBetweenPopups = 20f;
        [SerializeField] private float _zOffsetPopups = 40f;

        [Header("World Space Popups")]
        [SerializeField] internal Transform _worldSpacePopupTransform;
        [SerializeField] private FloatInFrontOfHeadset _floatInFrontOfHeadset;

        private void Awake()
        {
            // Singleton implementation
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }
        }

        private void Start()
        {
            _uiController = UIController.Instance;
            _onActivePanelChanged = OnActivePanelChanged;

            _spatialInputModule = SpatialInputModule.Instance;
            _interceptPointerCurrentRaycast = InterceptPointerCurrentRaycast;

            Register();
        }

        private void OnActivePanelChanged(UIController.Panel panel)
        {
            CloseAllPopups();
        }

        /// <summary>
        /// this is called when the user presses outside the popup
        /// </summary>
        private void OnClickedOutsidePopup()
        {
            if (_popups.Count == 0) { return; }

            PopupData popupData = _lastPopup;

            if (popupData.Params.ClickingOutsidePopupCloses)
            {
                CloseLastPopup();
            }
            else
            {
                Transform popupTransform = popupData.InstantiatedPopup.transform;

                // "flash" the uppermost popup so that the user gets notified that they first need to interact with
                // the popup in order to interact with the UI under the popup. 

                popupTransform.DOKill();
                popupTransform.DOScale(_popupFlashScale, _popupFlashDuration).OnComplete(() => { popupTransform.DOScale(1.0f, _popupFlashDuration); });
            }
        }

        private PopupData _lastPopup => _popups[_popups.Count - 1];

        // gets undo buttons
        private Transform[] __undoButtons = null;
        private Transform[] _undoButtons
        {
            get
            {
                if (__undoButtons != null)
                {
                    return __undoButtons;
                }

                // otherwise, get them by tag
                GameObject[] objects = GameObject.FindGameObjectsWithTag(Constants.k_TagUndoButton);
                if (objects.Length == 0)
                {
                    return null;
                }
                Debug.Log("Contained object");
                Transform[] transforms = new Transform[objects.Length];
                for (int i = 0; i < objects.Length; i++)
                {
                    transforms[i] = objects[i].transform;
                }
                __undoButtons = transforms;
                return __undoButtons;
            }
        }

        private bool InterceptPointerCurrentRaycast(ref RaycastResult raycastResult)
        {
            if (_popups.Count == 0) { return false; }

            if (raycastResult.isValid)
            {
                Transform target = raycastResult.gameObject.transform;

                if (_lastPopup.Params.DontInterceptUndoButtons)
                {
                    // test if it's undo buttons
                    Transform[] undoButtons = _undoButtons;
                    if (undoButtons != null)
                    {
                        for (int i = 0; i < undoButtons.Length; i++)
                        {
                            if (undoButtons[i] == target)
                            {
                                // don't intercept
                                return false;
                            }
                        }
                    }
                }

                Transform contextMenu = _lastPopup.InstantiatedPopup.transform;

                bool intercept = true;
                while (target != null)
                {
                    if (target == contextMenu)
                    {
                        intercept = false;
                        break;
                    }
                    target = target.parent;
                }
                if (intercept)
                {
                    // redirect the events to the underlay, which will perform a flash to notify
                    // the user that the popup is active. 
                    raycastResult.gameObject = _lastPopup.InstantiatedUnderlay;
                    return true;
                }
            }
            else
            {
                // also intercept when not valid
                raycastResult.gameObject = _lastPopup.InstantiatedUnderlay;
                return true;
            }

            return false;
        }

        private void DestroyPopup(PopupData popup)
        {
            GameObject go = popup.InstantiatedPopup;

            if (go == null) { return; }

            if (!go.activeInHierarchy)
            {
                // if it's not visible, don't animate
                Destroy(go);
                return;
            }

            Transform popupTransform = go.transform;

            // first disable all raycast targets
            Image[] images = popupTransform.GetComponentsInChildren<Image>();
            for (int i = 0; i < images.Length; i++)
            {
                images[i].raycastTarget = false;
            }

            // animate underlay as well
            GameObject underlay = popup.InstantiatedUnderlay;
            Image underlayImage = popup._underlayImage;
            underlayImage.raycastTarget = false;

            Color endColor = _popupUnderlayColor;
            endColor.a = 0f;
            underlayImage.DOKill();
            underlayImage.DOColor(endColor, _popupExitDuration).OnComplete(() =>
            {
                Destroy(underlay);
            });

            // then animate, on animation complete destroy
            popupTransform.DOLocalMoveZ(-_popupEnterZOffset, _popupExitDuration).SetEase(Ease.OutQuart).SetRelative(true);
            popupTransform.DOScale(_popupExitScale, _popupExitDuration).SetEase(Ease.OutQuart).OnComplete(() =>
            {
                popupTransform.DOKill();
                Destroy(go);
            });
        }

        /// <summary>
        /// Sets all dependencies on the world space popup:
        ///
        /// - [x] disable main and dock UI canvases
        /// - [x] enable both controllers' ray interaction
        /// - [x] disable "secondary button" events for non-dominant controller
        /// </summary>
        private void UpdateWorldSpacePopupDependencies()
        {
            bool hasWorldSpacePopup = _popups.Count > 0 && _popups.Any(p => p.Params.WorldSpace);

            // enable / disable main and dock UI canvases
            _uiController.UIVisible = !hasWorldSpacePopup;

            _floatInFrontOfHeadset.enabled = hasWorldSpacePopup;

            // enable / disable both controllers' ray interaction
            InputController.Instance.Handedness.BothHandsEnabled = hasWorldSpacePopup;
        }

        /// <summary>
        /// Instantiates the popup, adds it to the stack and returns the instantiated
        /// popup so that additional data can be set. 
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        public T OpenPopup<T>(GameObject popupPrefab, bool closeOtherPopups = false,
            PopupParams popupParams = null)
            where T : MonoBehaviour
        {
            // Close other popups if needed
            if (closeOtherPopups)
            {
                foreach (PopupData popup in _popups)
                {
                    DestroyPopup(popup);
                }
                _popups.Clear();
            }

            // Now instantiate the popup itself
            Transform popupParentTransform = transform;
            int popupIndex = -1; // used for how much depth is needed for the popup
            bool instantiatedAsWorldSpacePopup = false;

            int worldSpacePopupIndex = _popups.FindIndex(p => p.Params.WorldSpace);
            if (worldSpacePopupIndex != -1)
            {
                // instantiate popup in world space on top of the world space popup
                popupParentTransform = _worldSpacePopupTransform;
                popupIndex = _popups.Count - worldSpacePopupIndex; // make sure the popup is offset
                instantiatedAsWorldSpacePopup = true;
            }
            else if (popupParams != null && popupParams.WorldSpace)
            {
                // instantiates popup in world space
                popupParentTransform = _worldSpacePopupTransform;
                popupIndex = 0;
                instantiatedAsWorldSpacePopup = true;
            }
            else
            {
                // instantiate popup on the main canvas
                popupParentTransform = _popupTransform;
                popupIndex = _popups.Count;
                instantiatedAsWorldSpacePopup = false;
            }

            GameObject underlay = Instantiate(_popupUnderlayPrefab, popupParentTransform, false);
            RectTransform underlayRectTransform = underlay.GetComponent<RectTransform>();
            underlayRectTransform.pivot = new Vector2(0.5f, 0.5f);

            // Set dimensions of the popup underlay
            if (popupIndex == 0)
            {
                // just stretch its parent (InstantiatedMain)
                underlayRectTransform.sizeDelta = Vector2.zero;
                underlayRectTransform.anchoredPosition = Vector2.zero;
                underlayRectTransform.anchorMin = Vector2.zero;
                underlayRectTransform.anchorMax = Vector2.one;
            }
            else
            {
                // Use the dimensions of the previous popup
                RectTransform rect = _lastPopup._rectTransform;

                underlayRectTransform.localPosition = rect.localPosition;
                underlayRectTransform.sizeDelta = rect.sizeDelta;
                underlayRectTransform.anchoredPosition = rect.anchoredPosition;
                underlayRectTransform.anchorMin = rect.anchorMin;
                underlayRectTransform.anchorMax = rect.anchorMax;
            }

            UnityEngine.UI.Button button = underlay.GetComponentInChildren<UnityEngine.UI.Button>();
            button.onClick.AddListener(() =>
            {
                OnClickedOutsidePopup();
            });

            Image underlayImage = underlay.GetComponent<Image>();

            // animate underlay image opacity
            Color startColor = _popupUnderlayColor;
            startColor.a = 0f;
            underlayImage.color = startColor;
            underlayImage.DOColor(_popupUnderlayColor, _popupEnterDuration);

            // Instantiate popup
            GameObject instantiatedPopup = Instantiate(popupPrefab, popupParentTransform, false);
            PopupData popupData = new PopupData(instantiatedPopup, underlay, underlayImage,
                instantiatedAsWorldSpacePopup, popupParams);

            _popups.Add(popupData);
            UpdatePopupsCount();

            Transform popupTransform = instantiatedPopup.transform;

            // animate: enter the popup with a small animation. 
            Vector3 localPosition = instantiatedPopup.transform.localPosition;
            float endPosition = -_zOffsetPopups + popupIndex * -_zDistanceBetweenPopups;
            float startPosition = endPosition + (popupIndex > 0 ? _popupEnterZOffset : 0);

            popupTransform.localPosition = localPosition.SetZ(startPosition);
            popupTransform.localScale = Vector3.one * _popupEnterStartScale;
            popupTransform.DOScale(1.0f, _popupEnterDuration);
            popupTransform.DOLocalMoveZ(endPosition, _popupEnterDuration);

            UpdateWorldSpacePopupDependencies();

            return instantiatedPopup.GetComponent<T>();
        }

        private void UpdatePopupsCount()
        {
            PopupsCount.Value = _popups.Count;
        }

        /// <summary>
        /// Opens a context menu
        /// </summary>
        /// <param name="data"></param>
        public void OpenContextMenu(ContextMenu.ContextMenuData data,
            bool closeOtherPopups = false,
            PopupParams popupParams = null)
        {
            ContextMenu contextMenu = OpenPopup<ContextMenu>(_contextMenuPrefab, closeOtherPopups, popupParams);
            contextMenu.Data = data;
        }

        /// <summary>
        /// Opens a dialog box
        /// </summary>
        /// <param name="data"></param>
        public void OpenDialogBox(DialogBox.Data data,
            bool closeOtherPopups = false,
            PopupParams popupParams = null)
        {
            DialogBox dialogBox = OpenPopup<DialogBox>(_dialogBoxPrefab, closeOtherPopups, popupParams);
            dialogBox.ActiveData = data;
        }

        public void OpenTextInputPopup(TextInputPopup.Data data,
            Action<string> onConfirmValue,
            bool closeOtherPopups = false,
            PopupParams popupParams = null)
        {
            TextInputPopup textInputPopup = OpenPopup<TextInputPopup>(_inputTextPrefab, closeOtherPopups, popupParams);
            textInputPopup.ActiveData = data;
            textInputPopup.OnConfirmValue = onConfirmValue;
        }

        /// <summary>
        /// Don't call this before opening a new popup in the same frame!
        /// Because Unity delays destruction of the object using Object.Destroy()
        /// until the end of the frame, the OpenPopup dialog will think the
        /// _instantiatedUnderlay is not null and thus thinks it doesn't need to be recreated.
        ///
        /// This results in the underlay hiding every other opening of a menu in this manner. 
        /// </summary>
        public void CloseAllPopups()
        {
            foreach (PopupData popup in _popups)
            {
                DestroyPopup(popup);
            }
            _popups.Clear();
            UpdatePopupsCount();

            //if (_instantiatedUnderlay != null)
            //{
            //    Destroy(_instantiatedUnderlay);
            //}

            UpdateWorldSpacePopupDependencies();
        }

        /// <summary>
        /// Close this specific popup (will close all popups that are above it)
        /// </summary>
        /// <param name="popupGameObject"></param>
        public void ClosePopup(GameObject popup)
        {
            if (_popups.Count == 0) { return; }

            int foundAt = _popups.FindIndex(p => p.InstantiatedPopup == popup);
            //int foundAt = _instantiatedPopups.IndexOf(popup);
            if (foundAt != -1)
            {
                // destroy all popups above the provided popup, including this one
                for (int i = foundAt; i < _popups.Count; i++)
                {
                    DestroyPopup(_popups[i]);
                }
                // then remove them from the list
                _popups.RemoveRange(foundAt, _popups.Count - foundAt);
            }

            UpdatePopupsCount();

            UpdateWorldSpacePopupDependencies();
        }

        /// <summary>
        /// Closes the uppermost popup (the one that was last added)
        /// </summary>
        public void CloseLastPopup()
        {
            if (_popups.Count == 0) { return; }

            PopupData lastPopup = _lastPopup;
            DestroyPopup(lastPopup);
            _popups.Remove(lastPopup);

            UpdatePopupsCount();

            UpdateWorldSpacePopupDependencies();
        }

        #region Action registration

        private void Register()
        {
            if (_uiController != null)
            {
                _uiController.ActivePanel.Register(_onActivePanelChanged);
            }

            if (_spatialInputModule != null)
            {
                _spatialInputModule.interceptPointerCurrentRaycast += _interceptPointerCurrentRaycast;
            }
        }

        private void Unregister()
        {
            if (_uiController != null)
            {
                _uiController.ActivePanel.Unregister(_onActivePanelChanged);
            }

            if (_spatialInputModule != null)
            {
                _spatialInputModule.interceptPointerCurrentRaycast -= _interceptPointerCurrentRaycast;
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
