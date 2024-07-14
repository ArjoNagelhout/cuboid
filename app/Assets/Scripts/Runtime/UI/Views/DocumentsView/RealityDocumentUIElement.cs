// Copyright (c) 2023 Arjo Nagelhout

using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Cuboid.UI
{
    /// <summary>
    /// Operations:
    /// - Open
    /// - Delete
    /// - Rename
    /// - Duplicate
    /// - Info
    /// - Export
    /// </summary>
    public sealed class RealityDocumentUIElement : MonoBehaviour, IData<RealityDocumentFileInformation>
    {
        [SerializeField] private Image _thumbnail;
        [SerializeField] private TextMeshProUGUI _nameTextMesh;
        [SerializeField] private TextMeshProUGUI _metadataTextMesh;
        [SerializeField] private UnityEngine.UI.Button _button;
        [SerializeField] private GameObject _loadingSpinner;
        [SerializeField] private GameObject _documentInformationPopupPrefab;

        private IEnumerator _loadThumbnailCoroutine;

        private RealityDocumentFileInformation _data;
        public RealityDocumentFileInformation Data
        {
            get => _data;
            set
            {
                _data = value;
                OnDataChanged(_data);
            }
        }

        private bool _loadingThumbnail;
        public bool LoadingThumbnail
        {
            get => _loadingThumbnail;
            private set
            {
                _loadingThumbnail = value;
                _loadingSpinner.SetActive(_loadingThumbnail);
                _thumbnail.gameObject.SetActive(!_loadingThumbnail);
            }
        }

        private void OnDataChanged(RealityDocumentFileInformation data)
        {
            CancelCoroutines();
            LoadingThumbnail = true;

            if (_data == null) { return; }
            _nameTextMesh.text = Path.GetFileNameWithoutExtension(_data.Name);
            _metadataTextMesh.text = ($"Last modified: {_data.LastUpdatedAt.ToString()}");

            // only allow opening if it is not located in the trash.
            _button.interactable = _data.DocumentLocation == DocumentLocation.LocalDocuments;

            _loadThumbnailCoroutine = SetThumbnail();
            StartCoroutine(_loadThumbnailCoroutine);
        }

        private IEnumerator SetThumbnail()
        {
            yield return ThumbnailProvider.Instance.LoadThumbnailAsync(Data.FilePath, false).Execute(Data.FilePath, out CoroutineTask<Sprite> task);
            while (!task.Done) { yield return null; }
            if (task.Failed) { yield break; }

            Sprite thumbnail = task.Result;

            // set the thumbnail
            _thumbnail.sprite = thumbnail;
            LoadingThumbnail = false;
        }

        private void CancelCoroutines()
        {
            CoroutineUtils.StopAndClearCoroutine(this, ref _loadThumbnailCoroutine);
        }

        private void OnEnable()
        {
            OnDataChanged(Data);
        }

        private void OnDisable()
        {
            CancelCoroutines();
        }

        private void OnDestroy()
        {
            CancelCoroutines();
        }

        public void Open()
        {
            DocumentsViewController.Instance.Open(Data);
        }

        public void OpenPermanentlyDeleteDialogBox()
        {
            if (Data == null) { return; }

            DialogBox.Data data = new DialogBox.Data()
            {
                Title = "Permanently delete?",
                Description = "This action cannot be undone.",
                Icon = Icons.Data.Delete,
                Buttons = new List<Button.Data>()
                {
                    new Button.Data()
                    {
                        Text = "Permanently Delete",
                        OnPressed = () =>
                        {
                            Data.PermanentlyDelete();
                            PopupsController.Instance.CloseAllPopups();
                            DocumentsViewController.Instance.Refresh();
                        },
                        Variant = ButtonColors.Variant.Solid
                    },
                    new Button.Data()
                    {
                        Text = "Cancel",
                        OnPressed = () =>
                        {
                            PopupsController.Instance.CloseLastPopup();
                        },
                        Variant = ButtonColors.Variant.Plain
                    }
                }
            };

            PopupsController.Instance.OpenDialogBox(data);
        }

        public void OpenDeleteDialogBox()
        {
            if (Data == null) { return; }

            DialogBox.Data data = new DialogBox.Data()
            {
                Title = "Are you sure you want to delete this document?",
                Description = "Deleted files can be restored from the Trash",
                Icon = Icons.Data.Delete,
                Buttons = new List<Button.Data>()
                {
                    new Button.Data()
                    {
                        Text = "Delete",
                        Variant = ButtonColors.Variant.Solid,
                        OnPressed = () =>
                        {
                            Data.Delete();
                            PopupsController.Instance.CloseAllPopups();
                            DocumentsViewController.Instance.Refresh();
                        }
                    },
                    new Button.Data()
                    {
                        Text = "Cancel",
                        Variant = ButtonColors.Variant.Plain,
                        OnPressed = () =>
                        {
                            PopupsController.Instance.CloseLastPopup();
                        }
                    }
                }
            };
            PopupsController.Instance.OpenDialogBox(data, false, new PopupsController.PopupParams() { ClickingOutsidePopupCloses = false });
        }

        public void OpenMoreContextMenu()
        {
            // Don't open the context menu if the data was not set. 
            if (Data == null) { return; }

            ContextMenu.ContextMenuData trashContextMenuData = new ContextMenu.ContextMenuData()
            {
                Title = Path.GetFileNameWithoutExtension(Data.Name),
                Buttons = new List<Button.Data>()
                {
                    new Button.Data()
                    {
                        Text = "Restore",
                        Icon = Icons.Data.RestoreFromTrash,
                        OnPressed = () =>
                        {
                            Data.Restore();
                            PopupsController.Instance.CloseAllPopups();
                            DocumentsViewController.Instance.Refresh();
                        }
                    },
                    new Button.Data()
                    {
                        Text = "Permanently delete",
                        Icon = Icons.Data.DeleteForever,
                        OnPressed = () =>
                        {
                            OpenPermanentlyDeleteDialogBox();
                        }
                    }
                }
            };

            ContextMenu.ContextMenuData contextMenuData = new ContextMenu.ContextMenuData()
            {
                Title = Path.GetFileNameWithoutExtension(Data.Name),
                Buttons = new List<Button.Data>()
                {
                    new Button.Data()
                    {
                        Text = "Open",
                        Icon = Icons.Data.FileOpen,
                        OnPressed = () =>
                        {
                            Open();
                        }
                    },
                    new Button.Data()
                    {
                        Text = "Rename",
                        Icon = Icons.Data.Edit,
                        OnPressed = () =>
                        {
                            RealityDocumentController.OpenRenamePopup(Data);
                        }
                    },
                    new Button.Data()
                    {
                        Text = "Duplicate",
                        Icon = Icons.Data.FileCopy,
                        OnPressed = () =>
                        {
                            Data.Duplicate();
                            PopupsController.Instance.CloseLastPopup();
                            DocumentsViewController.Instance.Refresh();
                        }
                    },
                    new Button.Data()
                    {
                        Text = "Get Info",
                        Icon = Icons.Data.Info,
                        OnPressed = () =>
                        {
                            RealityDocumentFileInformationPopupUIElement popup = PopupsController.Instance.OpenPopup<RealityDocumentFileInformationPopupUIElement>(_documentInformationPopupPrefab);
                            popup.Data = Data;
                        }
                    },
                    //new Button.ButtonData()
                    //{
                    //    Text = "Export",
                    //    Icon = _iconExport,
                    //    OnPressed = () =>
                    //    {
                    //        Debug.Log("Export");
                    //    }
                    //},
                    new Button.Data()
                    {
                        Text = "Delete",
                        Icon = Icons.Data.Delete,
                        OnPressed = () =>
                        {
                            OpenDeleteDialogBox();
                        }
                    }
                }
            };

            switch (Data.DocumentLocation)
            {
                case DocumentLocation.LocalDocuments:
                    PopupsController.Instance.OpenContextMenu(contextMenuData);
                    break;
                case DocumentLocation.LocalTrash:
                    PopupsController.Instance.OpenContextMenu(trashContextMenuData);
                    break;
            }
        }

        
    }
}

