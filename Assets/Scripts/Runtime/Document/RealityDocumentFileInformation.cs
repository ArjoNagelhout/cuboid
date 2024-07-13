// 
// RealityDocumentFileInformation.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Cuboid
{
    public enum DocumentLocation
    {
        LocalDocuments,
        LocalTrash
    }

    public static class DocumentLocationExtensions
    {
        public static string ToDirectoryPath(this DocumentLocation documentLocation) =>
            documentLocation switch
            {
                DocumentLocation.LocalDocuments => Constants.DocumentsDirectoryPath,
                DocumentLocation.LocalTrash => Constants.TrashDirectoryPath,
                _ => Constants.DocumentsDirectoryPath
            };
    }

    /// <summary>
    /// Information about the RealityDocument for preview purposes
    /// </summary>
    public class RealityDocumentFileInformation
    {
        private const string k_RealityDocumentUntitledPrefix = "Untitled_";

        /// <summary>
        /// Path of the document, for loading it
        /// </summary>
        public string FilePath;

        /// <summary>
        /// Name of the document, with extension for now
        /// </summary>
        public string Name;

        /// <summary>
        /// When the document was created
        /// </summary>
        public DateTime CreatedAt;

        /// <summary>
        /// When the document was last opened
        /// </summary>
        public DateTime LastOpenedAt;

        /// <summary>
        /// When the document was last updated
        /// </summary>
        public DateTime LastUpdatedAt;

        /// <summary>
        /// Where the the document is located at. 
        /// </summary>
        public DocumentLocation DocumentLocation;

        /// <summary>
        /// Filesize in bytes
        /// </summary>
        public long FileSize;

        /// <summary>
        /// Restores the file from the trash into the documents folder
        /// </summary>
        public void Restore()
        {
            if (!File.Exists(FilePath))
            {
                throw new FileNotFoundException();
            }

            MoveFile(Constants.DocumentsDirectoryPath);
        }

        /// <summary>
        /// Permanently deletes the file from the trash, cannot be undone. 
        /// </summary>
        public void PermanentlyDelete()
        {
            if (!File.Exists(FilePath))
            {
                throw new FileNotFoundException();
            }

            File.Delete(FilePath);
        }

        /// <summary>
        /// Moves the file to the trash
        /// </summary>
        public void Delete()
        {
            if (!File.Exists(FilePath))
            {
                throw new FileNotFoundException();
            }

            MoveFile(Constants.TrashDirectoryPath);
        }

        public static void EmptyTrash()
        {
            List<RealityDocumentFileInformation> trashRealityDocumentsInformation = GetRealityDocumentsInformation(DocumentLocation.LocalTrash);

            foreach (RealityDocumentFileInformation information in trashRealityDocumentsInformation)
            {
                File.Delete(information.FilePath);
            }
        }

        public void Duplicate()
        {
            if (!File.Exists(FilePath))
            {
                throw new FileNotFoundException();
            }

            string copyFilePath = GetCopyFilePath(FilePath);
            Debug.Log(copyFilePath);
            File.Copy(FilePath, copyFilePath);
        }

        private static string GetCopyFilePath(string filePath)
        {
            if (!File.Exists(filePath))
                return filePath;

            string filePathWithoutExtension = filePath.Substring(0, filePath.LastIndexOf('.'));
            string fileExtension = Path.GetExtension(filePath);

            // Check if the filename ends in copy with a number, if so, it should start counting from there
            // e.g. file copy 1.json, file copy 2.json, file copy 3.json. 
            int copyIndex = filePathWithoutExtension.LastIndexOf(" copy");
            if (copyIndex != -1) // -1 means copy was not found
            {
                // remove copy from the string
                filePathWithoutExtension = filePathWithoutExtension.Substring(0, copyIndex);
            }

            int i = 0;
            string newFilePath;
            do
            {
                string count = i == 0 ? "" : $" {i}";
                newFilePath = $"{filePathWithoutExtension} copy{count}{fileExtension}";
                i++;
            } while (File.Exists(newFilePath));

            return newFilePath;
        }

        /// <summary>
        /// Renames the file, strips the extension from the name
        /// </summary>
        public void Rename(string newName)
        {
            if (newName.Contains('/'))
            {
                throw new System.Exception("/ not allowed");
            }

            if (!File.Exists(FilePath))
            {
                throw new FileNotFoundException();
            }

            ThumbnailProvider.Instance.InvalidateThumbnail(FilePath);

            string filePath = FilePath;
            string directoryPath = filePath.Substring(0, filePath.LastIndexOf('/'));
            string newFilePath = Path.Combine(directoryPath, newName + UnityPlugin.Constants.k_DocumentFileExtension);

            if (File.Exists(newFilePath))
            {
                throw new System.Exception("File already exists, please use a different name");
            }

            File.Move(FilePath, newFilePath);

            FilePath = newFilePath;
            Name = Path.GetFileName(FilePath);
        }

        /// <summary>
        /// Internal method for moving a file and adding a datestring if the names overlap.
        /// Similar to how macOS handles resolving file name collisions when moving items to the trash. 
        /// </summary>
        private void MoveFile(string targetDirectory)
        {
            string newFilePath = Path.Combine(targetDirectory, Name);

            if (File.Exists(newFilePath))
            {
                string dateString = LastUpdatedAt.ToString("yyyy.MM.dd.HH.mm.ss");

                string name = Name.Split('.')[0]; // remove the extension
                newFilePath = Path.Combine(targetDirectory, name + "_" + dateString + UnityPlugin.Constants.k_DocumentFileExtension);
            }

            File.Move(FilePath, newFilePath);
        }

        /// <summary>
        /// Get all RealityDocuments' Information from the given directory.
        /// 
        /// Throws <see cref="DirectoryNotFoundException"/> if the given directory does not exist.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<RealityDocumentFileInformation> GetRealityDocumentsInformation(DocumentLocation documentLocation)
        {
            string directoryPath = documentLocation.ToDirectoryPath();
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException();
            }

            List<RealityDocumentFileInformation> filesInformation = new List<RealityDocumentFileInformation>();
            
            string[] realityDocumentFilePaths = Directory.GetFiles(directoryPath, "*" + UnityPlugin.Constants.k_DocumentFileExtension);

            foreach (string realityDocumentFilePath in realityDocumentFilePaths)
            {
                RealityDocumentFileInformation realityDocumentInformation = GetRealityDocumentInformation(realityDocumentFilePath);
                realityDocumentInformation.DocumentLocation = documentLocation;
                filesInformation.Add(realityDocumentInformation);
            }

            List<RealityDocumentFileInformation> sortedFilesInformation = filesInformation.OrderByDescending(o => o.LastUpdatedAt).ToList();

            return sortedFilesInformation;
        }

        /// <summary>
        /// Creates a RealityObjectInformation object from the given file at the given file path
        /// </summary>
        public static RealityDocumentFileInformation GetRealityDocumentInformation(string filePath)
        {
            // Todo: Add some checks
            FileInfo fileInfo = new FileInfo(filePath);
            RealityDocumentFileInformation fileInformation = new RealityDocumentFileInformation()
            {
                FilePath = filePath,
                Name = fileInfo.Name,
                CreatedAt = fileInfo.CreationTime,
                LastOpenedAt = fileInfo.LastAccessTime,
                LastUpdatedAt = fileInfo.LastWriteTime,
                FileSize = fileInfo.Length
            };

            return fileInformation;
        }

        public static string GetNewUntitledFilePath(string directoryPath)
        {
            int index = 0;
            int sanity = 9999;
            while (sanity > 0)
            {
                string attempt = k_RealityDocumentUntitledPrefix + index.ToString();
                sanity--;
                index++;

                attempt = Path.Combine(directoryPath, attempt) + UnityPlugin.Constants.k_DocumentFileExtension;

                if (!File.Exists(attempt))
                {
                    return attempt;
                }
            }

            throw new System.Exception();
        }
    }
}
