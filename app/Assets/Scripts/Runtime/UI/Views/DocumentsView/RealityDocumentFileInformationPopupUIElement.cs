//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Cuboid.UI
{
    /// <summary>
    /// 
    /// </summary>
    public class RealityDocumentFileInformationPopupUIElement : MonoBehaviour
    {
        private RealityDocumentFileInformation _data;
        public RealityDocumentFileInformation Data
        {
            get => _data;
            set
            {
                _data = value;
                if (_data == null) { return; }

                _nameTextMesh.text = Path.GetFileNameWithoutExtension(_data.Name);

                _filePathTextMesh.text = _data.FilePath;

                string generalFileInformationText = "";
                // order: 1. file size, 2. opened at. 3. modified. 4. created
                generalFileInformationText += $"{FileUtils.BytesToString(_data.FileSize)}\n";
                generalFileInformationText += $"{_data.LastOpenedAt.ToString()}\n";
                generalFileInformationText += $"{_data.LastUpdatedAt.ToString()}\n";
                generalFileInformationText += $"{_data.CreatedAt.ToString()}";

                _generalFileInformationTextMesh.text = generalFileInformationText;
            }
        }

        [SerializeField] private Image _thumbnail;
        [SerializeField] private TextMeshProUGUI _nameTextMesh;
        [SerializeField] private TextMeshProUGUI _filePathTextMesh;
        [SerializeField] private TextMeshProUGUI _generalFileInformationTextMesh;

        /// <summary>
        /// Method for the close button
        /// </summary>
        public void Close()
        {
            PopupsController.Instance.ClosePopup(gameObject);
        }
    }
}
