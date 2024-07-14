//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cuboid;
using TMPro;
using UnityEngine.UI;

namespace Cuboid.UI
{
    /// <summary>
    /// Responsible for the Projects panel.
    ///
    /// The projects panel has two main views:
    ///
    /// 1. The view with all projects, that can be opened etc.
    ///
    /// 2. The view of the opened project. With information about:
    /// - Name (renaming is possible)
    /// - Sharing (collaboration)
    /// - Metadata about the project
    /// - Thumbnail
    /// - Exporting (to a 3D file format), future
    /// </summary>
    public sealed class DocumentsViewController : MonoBehaviour
    {
        private static DocumentsViewController _instance;
        public static DocumentsViewController Instance => _instance;

        [SerializeField] private ScrollView _scrollView;
        [SerializeField] private RectTransform _scrollViewContentRectTransform;

        [SerializeField] private RectTransform _documentsListView;
        [SerializeField] private RectTransform _documentsListContentView; // The RectTransform to which the content should be added

        [SerializeField] private RectTransform _documentView;

        [SerializeField] private GameObject _noDocumentsNotice;
        [SerializeField] private TextMeshProUGUI _noDocumentsNoticeText;
        [SerializeField] private string _noLocalDocumentsNoticeString;
        [SerializeField] private string _noDocumentsInTrashNoticeString;

        [SerializeField] private TextMeshProUGUI _headerText;

        [SerializeField] private GameObject _backButton;
        [SerializeField] private GameObject _saveNewButton;
        [SerializeField] private GameObject _emptyButton;

        [Header("RealityDocumentUIElement instantiation")]
        [SerializeField] private GameObject _realityDocumentUIElementPrefab;
        [SerializeField] private ScrollViewPool _scrollViewPool;
        [SerializeField] private ScrollViewPool.ListLayout _listLayout;
        [SerializeField] private ScrollViewPool.GridLayout _gridLayout;
        private ScrollViewPool.ScrollViewPoolInternal<RealityDocumentFileInformation> _createdPool;

        /// <summary>
        /// Enum for selecting list or grid layout for documents
        /// </summary>
        [Serializable]
        public enum Layout
        {
            Grid,
            List
        }

        [SerializeField] private Layout ActiveLayout = Layout.List;

        private UndoRedoController _undoRedoController;
        private RealityDocumentController _realityDocumentController;

        private List<RealityDocumentUIElement> _instantiatedRealityDocumentUIElements = new List<RealityDocumentUIElement>();

        [System.NonSerialized]
        public StoredBinding<DocumentLocation> DocumentLocation;

        private Action<RealityDocumentFileInformation> _onOpenedFileChanged;
        private Action<UndoRedoData> _onUndoRedoDataChanged;

        private StoredBinding<Vector2> _currentScrollPositionBinding = null;
        private Dictionary<DocumentLocation, StoredBinding<Vector2>> _documentLocationScrollPositions = new Dictionary<DocumentLocation, StoredBinding<Vector2>>();

        private void Awake()
        {
            // Singleton implemention
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }

            DocumentLocation = new("DocumentsViewController_DocumentLocation", Cuboid.DocumentLocation.LocalDocuments);
        }

        private void Start()
        {
            _realityDocumentController = RealityDocumentController.Instance;
            _undoRedoController = UndoRedoController.Instance;

            _scrollView.OnScrollPositionChanged += OnScrollPositionChanged;

            // initialize the ScrollViewPool (before registering the document location binding)
            _createdPool = _scrollViewPool.CreatePool<RealityDocumentFileInformation>(new ScrollViewPool.ScrollViewPoolInternal<RealityDocumentFileInformation>.Data()
            {
                Layout = ActiveLayout == Layout.Grid ? _gridLayout : _listLayout,
                Prefab = _realityDocumentUIElementPrefab,
                Values = new List<RealityDocumentFileInformation>()
            });

            // Can immediately be registered here because the binding is defined in this class
            DocumentLocation.Register(OnDocumentLocationChanged);

            _onOpenedFileChanged = OnOpenedFileChanged;
            _onUndoRedoDataChanged = OnUndoRedoDataChanged;

            Register();
        }

        public void Refresh()
        {
            OnDocumentLocationChanged(DocumentLocation.Value);
        }

        public void Back()
        {
            _realityDocumentController.Close();
        }

        public void Save()
        {
            _realityDocumentController.Save(() =>
            {
                // close all popups when saved
                PopupsController.Instance.CloseAllPopups();
            });
        }

        public void EmptyTrash()
        {
            PopupsController.Instance.OpenDialogBox(new DialogBox.Data()
            {
                Title = "Are you sure you want to empty the trash?",
                Description = "You can't undo this action",
                Buttons = new List<Button.Data>()
                {
                    new Button.Data()
                    {
                        Text = "Empty Trash",
                        Icon = Icons.Data.DeleteSweep,
                        OnPressed = () =>
                        {
                            RealityDocumentFileInformation.EmptyTrash();
                            Refresh();
                            PopupsController.Instance.CloseAllPopups();
                        },
                        Variant = ButtonColors.Variant.Solid
                    },
                    new Button.Data()
                    {
                        Text = "Cancel",
                        OnPressed = () => PopupsController.Instance.CloseAllPopups(),
                        Variant = ButtonColors.Variant.Plain
                    }
                }
            });
        }

        public void OpenSaveAsPopup()
        {
            PopupsController.Instance.OpenTextInputPopup(new TextInputPopup.Data()
            {
                Title = "Save Document",
                Description = "Please enter a new name for the document.",
                ConfirmText = "Save As",
                Value = Path.GetFileNameWithoutExtension(_realityDocumentController.OpenedFile.Value.FilePath)
            }, onConfirmValue: (newValue) =>
            {
                RealityDocumentController.Instance.SaveAs(newValue);
                PopupsController.Instance.CloseAllPopups();
                Refresh();
                UpdateUI();
            });
        }

        public void OpenRenamePopup()
        {
            RealityDocumentController.OpenRenamePopup(_realityDocumentController.OpenedFile.Value);
        }

        public void UpdateUI()
        {
            UpdateUI(_needsSaving, _realityDocumentController.OpenedFile.Value);
        }

        public void Open(RealityDocumentFileInformation document)
        {
            RealityDocumentController.Instance.Open(document);
        }

        private void OnDocumentLocationChanged(DocumentLocation documentLocation)
        {
            LoadRealityDocuments(documentLocation);
        }

        private void LoadRealityDocuments(DocumentLocation documentLocation)
        {
            // get documents
            List<RealityDocumentFileInformation> documentsInformation = RealityDocumentFileInformation.GetRealityDocumentsInformation(documentLocation);

            // set no documents notice
            _noDocumentsNotice.SetActive(documentsInformation.Count == 0);
            switch (documentLocation)
            {
                case Cuboid.DocumentLocation.LocalDocuments:
                    _noDocumentsNoticeText.text = _noLocalDocumentsNoticeString;
                    break;
                case Cuboid.DocumentLocation.LocalTrash:
                    _noDocumentsNoticeText.text = _noDocumentsInTrashNoticeString;
                    break;
            }

            // set empty trash button
            _emptyButton.SetActive(documentLocation == Cuboid.DocumentLocation.LocalTrash && documentsInformation.Count > 0);

            // set the data
            _createdPool.ActiveData.Values = documentsInformation;
            _createdPool.DataChanged();

            // set the scroll position
            if (_documentLocationScrollPositions.TryGetValue(documentLocation, out StoredBinding<Vector2> storedBinding))
            {
                _currentScrollPositionBinding = storedBinding;
            }
            else
            {
                // store a new value
                StoredBinding<Vector2> newStoredBinding = new StoredBinding<Vector2>("DocumentLocation_" + documentLocation.ToString() + "_ScrollPosition", Vector2.zero);
                _documentLocationScrollPositions.Add(documentLocation, newStoredBinding);

                _currentScrollPositionBinding = newStoredBinding;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollViewContentRectTransform);
            _scrollView.SetScrollPosition(_currentScrollPositionBinding.Value);
        }

        private void OnScrollPositionChanged(Vector2 position)
        {
            if (_currentScrollPositionBinding != null)
            {
                _currentScrollPositionBinding.Value = position;
            }
        }

        /// <summary>
        /// Cached data from UndoRedoController
        /// </summary>
        private bool _needsSaving = false;

        /// <summary>
        /// Cached data from RealityDocumentController
        /// </summary>
        private RealityDocumentFileInformation _openedFileInfo = null;

        /// <summary>
        /// When a command is executed, needs saving is set to true. 
        /// </summary>
        private void OnUndoRedoDataChanged(UndoRedoData undoRedoData)
        {
            _needsSaving = undoRedoData.NeedsSaving;
            UpdateUI(_needsSaving, _openedFileInfo);
        }

        private void OnOpenedFileChanged(RealityDocumentFileInformation openedFileInfo)
        {
            _openedFileInfo = openedFileInfo;
            UpdateUI(_needsSaving, _openedFileInfo);
            Refresh();
        }

        private void UpdateUI(bool needsSaving, RealityDocumentFileInformation openedFileInfo)
        {
            bool opened = openedFileInfo != null;

            // Set views
            _documentView.gameObject.SetActive(opened);
            _documentsListView.gameObject.SetActive(!opened);

            // Update header text
            string newUntitledName = RealityDocumentFileInformation.GetNewUntitledFilePath(Constants.DocumentsDirectoryPath);
            newUntitledName = newUntitledName.Substring(newUntitledName.LastIndexOf('/')+1);

            string documentsString = needsSaving ? Path.GetFileNameWithoutExtension(newUntitledName) : "Documents";

            string headerString = opened ? Path.GetFileNameWithoutExtension(openedFileInfo.Name) : documentsString;
            if (needsSaving) { headerString += "*"; }
            _headerText.text = headerString;

            // Set buttons
            _backButton.SetActive(opened);
            _saveNewButton.SetActive(needsSaving); // also show if opened?
        }

        #region Action registration

        private void Register()
        {
            if (_realityDocumentController != null)
            {
                _realityDocumentController.OpenedFile.Register(_onOpenedFileChanged);
            }

            if (_undoRedoController != null)
            {
                _undoRedoController.UndoRedoData.Register(_onUndoRedoDataChanged);
            }
        }

        private void Unregister()
        {
            if (_realityDocumentController != null)
            {
                _realityDocumentController.OpenedFile.Unregister(_onOpenedFileChanged);
            }

            if (_undoRedoController != null)
            {
                _undoRedoController.UndoRedoData.Unregister(_onUndoRedoDataChanged);
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

