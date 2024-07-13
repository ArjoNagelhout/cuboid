// 
// RealityDocumentRenamePopupUIElement.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Cuboid.UI
{
    /// <summary>
    /// 
    /// </summary>
    public class TextInputPopup : MonoBehaviour
    {
        public class Data
        {
            public string Value;
            public string Title = "";
            public string Description = "";
            public string ConfirmText = "Confirm";
        }

        private Data _activeData = null;
        public Data ActiveData
        {
            get => _activeData;
            set
            {
                _activeData = value;
                OnDataChanged(_activeData);
            }
        }

        [SerializeField] private InputField _inputField;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Button _confirmButton;

        public Action<string> OnConfirmValue;

        private void OnDataChanged(Data data)
        {
            _inputField.text = data.Value;
            _titleText.text = data.Title;
            _descriptionText.text = data.Description;
            _confirmButton.ActiveData.Text = data.ConfirmText;
        }

        public void Confirm()
        {
            OnConfirmValue?.Invoke(_inputField.text);
        }

        public void Close()
        {
            PopupsController.Instance.CloseLastPopup();
        }
    }
}

