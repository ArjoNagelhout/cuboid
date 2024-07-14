//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.IO;
using System.IO.Compression;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cuboid.Models;
using Newtonsoft.Json;
using Cuboid.UI;
using System.Text;

namespace Cuboid
{
    /// <summary>
    /// Responsible for saving and loading a RealityDocument
    /// </summary>
    public class RealityDocumentController : MonoBehaviour
    {
        private static RealityDocumentController _instance;
        public static RealityDocumentController Instance => _instance;

        [NonSerialized]
        public Binding<RealityDocumentFileInformation> OpenedFile = new(null);

        [NonSerialized]
        public Binding<RealityDocument> RealityDocument = new(null);

        private void Awake()
        {
            // Singleton implementation
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }
            RealityDocument.Value = Cuboid.RealityDocument.CreateEmptyRealityDocument();
        }

        private void Start()
        {
            
        }

        public static void OpenRenamePopup(RealityDocumentFileInformation fileInformation, Action onConfirmValue = null)
        {
            PopupsController.Instance.OpenTextInputPopup(new TextInputPopup.Data()
            {
                Title = "Rename",
                Description = "Please enter a new name for the document:",
                ConfirmText = "Rename",
                Value = Path.GetFileNameWithoutExtension(fileInformation.FilePath)
            }, onConfirmValue: (newName) =>
            {
                fileInformation.Rename(newName);
                PopupsController.Instance.CloseAllPopups();
                DocumentsViewController.Instance.Refresh();
                DocumentsViewController.Instance.UpdateUI();
            });
        }

        public void Open(RealityDocumentFileInformation information)
        {
            if (!File.Exists(information.FilePath))
            {
                throw new FileNotFoundException();
            }

            if (UndoRedoController.Instance.UndoRedoData.Value.NeedsSaving)
            {
                ShowSaveDialog(
                    () =>
                    {
                        InternalSave();
                        InternalOpen(information);
                    },
                    () =>
                    {
                        InternalOpen(information);
                    });
            }
            else
            {
                InternalOpen(information);
                PopupsController.Instance.CloseAllPopups();
            }
        }

        public void Close()
        {
            if (UndoRedoController.Instance.UndoRedoData.Value.NeedsSaving)
            {
                ShowSaveDialog(
                    () =>
                    {
                        InternalSave();
                        InternalClose();
                    },
                    () =>
                    {
                        InternalClose();
                    });
            }
            else
            {
                InternalClose();
            }
        }

        private void ShowSaveDialog(Action onSave, Action onDontSave)
        {
            PopupsController.Instance.OpenDialogBox(new DialogBox.Data()
            {
                Title = "Save the document?",
                Description = "The document has been modified. Save the file before closing?",
                Buttons = new List<Button.Data>()
                {
                    new Button.Data()
                    {
                        Text = "Save",
                        Variant = ButtonColors.Variant.Solid,
                        OnPressed = () =>
                        {
                            PopupsController.Instance.CloseAllPopups();
                            onSave?.Invoke();
                        }
                    },
                    new Button.Data()
                    {
                        Text = "Don't Save",
                        Variant = ButtonColors.Variant.Plain,
                        OnPressed = () =>
                        {
                            PopupsController.Instance.CloseAllPopups();
                            onDontSave?.Invoke();
                        }
                    },
                    new Button.Data()
                    {
                        Text = "Cancel",
                        Variant = ButtonColors.Variant.Plain,
                        OnPressed = () =>
                        {
                            PopupsController.Instance.CloseAllPopups();
                        }
                    }
                }
            }, popupParams: new PopupsController.PopupParams() { ClickingOutsidePopupCloses = false });
        }

        /// <summary>
        /// onSaveComplete can be used to call something like PopupsController.Instance.CloseAllPopups();
        /// </summary>
        /// <param name="onSaveComplete"></param>
        public void Save(Action onSaveComplete)
        {
            InternalSave();
            onSaveComplete?.Invoke();
            
        }

        public void SaveAs(string newName)
        {
            // only allow execution if a file is opened
            if (OpenedFile.Value == null) { return; }

            string currentFilePath = OpenedFile.Value.FilePath;
            string directoryPath = currentFilePath.Substring(0, currentFilePath.LastIndexOf('/'));

            string newFilePath = Path.Combine(directoryPath, newName + UnityPlugin.Constants.k_DocumentFileExtension);

            SaveRealityDocumentToFile(RealityDocument.Value, newFilePath);

            OpenedFile.Value = RealityDocumentFileInformation.GetRealityDocumentInformation(newFilePath);

            UndoRedoController.Instance.SetLastStackCount();
        }

        private void InternalSave()
        {
            // Either save as new untitled, or as the currently opened file
            if (OpenedFile.Value == null)
            {
                // ask to save as new
                string path = RealityDocumentFileInformation.GetNewUntitledFilePath(Constants.DocumentsDirectoryPath);
                SaveRealityDocumentToFile(RealityDocument.Value, path);
                OpenedFile.Value = RealityDocumentFileInformation.GetRealityDocumentInformation(path);
            }
            else
            {
                SaveRealityDocumentToFile(RealityDocument.Value, OpenedFile.Value.FilePath);
            }
            UndoRedoController.Instance.SetLastStackCount();
        }

        private void InternalOpen(RealityDocumentFileInformation information)
        {
            RealityDocument.Value = LoadRealityDocumentFromFile(information.FilePath);
            OpenedFile.Value = information;
            UndoRedoController.Instance.ClearStacks();
        }

        private void InternalClose()
        {
            RealityDocument.Value = Cuboid.RealityDocument.CreateEmptyRealityDocument();
            OpenedFile.Value = null;
            UndoRedoController.Instance.ClearStacks();
        }
        
        private static void SaveRealityDocumentToFile(RealityDocument realityDocument, string path)
        {
            // take snapshot from camera
            byte[] thumbnail = ScreenshotController.Instance.CaptureScreenshot();
            string jsonString = JsonConvert.SerializeObject(realityDocument, Formatting.Indented, JsonSerialization.GlobalSerializerSettings);
            byte[] json = Encoding.UTF8.GetBytes(jsonString);

            using (FileStream fileStream = new FileStream(path, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Create, true))
                {
                    ZipArchiveEntry thumbnailEntry = archive.CreateEntry(UnityPlugin.Constants.k_ThumbnailEntryName, System.IO.Compression.CompressionLevel.NoCompression);
                    using (Stream zipStream = thumbnailEntry.Open())
                    {
                        zipStream.Write(thumbnail);
                    }
                    ZipArchiveEntry jsonEntry = archive.CreateEntry(UnityPlugin.Constants.k_DocumentEntryName, System.IO.Compression.CompressionLevel.NoCompression);
                    using (Stream zipStream = jsonEntry.Open())
                    {
                        zipStream.Write(json);
                    }
                }
            }
        }

        private static RealityDocument LoadRealityDocumentFromFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException();
            }

            byte[] jsonBytes = null;

            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Read, true))
                {
                    ZipArchiveEntry jsonEntry = archive.GetEntry(UnityPlugin.Constants.k_DocumentEntryName);
                    using (Stream zipStream = jsonEntry.Open())
                    {
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            zipStream.CopyTo(memoryStream);
                            jsonBytes = memoryStream.ToArray();
                        }
                    }
                }
            }

            if (jsonBytes == null)
            {
                throw new Exception($"{nameof(RealityDocument)} at path {path} does not contain a valid {nameof(UnityPlugin.Constants.k_DocumentEntryName)} entry");
            }

            string json = Encoding.UTF8.GetString(jsonBytes);

            RealityDocument realityDocument = JsonConvert.DeserializeObject<RealityDocument>(json, JsonSerialization.GlobalSerializerSettings);

            return realityDocument;
        }
    }
}

