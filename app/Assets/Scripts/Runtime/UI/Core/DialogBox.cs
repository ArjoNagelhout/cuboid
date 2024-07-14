// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Cuboid.UI.ContextMenu;

namespace Cuboid.UI
{
    /// <summary>
    /// 
    /// </summary>
    public class DialogBox : MonoBehaviour
    {
        /// <summary>
        /// 
        /// </summary>
        public class Data
        {
            /// <summary>
            /// 
            /// </summary>
            public string Title;

            /// <summary>
            /// 
            /// </summary>
            public string Description;

            /// <summary>
            /// 
            /// </summary>
            public Sprite Icon;

            /// <summary>
            /// 
            /// </summary>
            public List<Button.Data> Buttons;
        }

        [SerializeField] private TextMeshProUGUI _titleTextMesh;
        [SerializeField] private TextMeshProUGUI _descriptionTextMesh;
        [SerializeField] private Image _image;

        private List<Button> _instantiatedButtons = new List<Button>();

        private Data _activeData;
        public Data ActiveData
        {
            get => _activeData;
            set
            {
                _activeData = value;
                if (_activeData == null) { return; }
                OnDataChanged(_activeData);
            }
        }

        private void OnDataChanged(Data data)
        {
            // first remove all old instantiated items
            foreach (Button button in _instantiatedButtons)
            {
                Destroy(button.gameObject);
            }

            _instantiatedButtons.Clear();

            bool hasTitle = data.Title != null;
            if (hasTitle)
            {
                _titleTextMesh.text = data.Title;
            }
            _titleTextMesh.gameObject.SetActive(hasTitle);

            bool hasDescription = data.Description != null;
            if (hasDescription)
            {
                _descriptionTextMesh.text = data.Description;
            }
            _descriptionTextMesh.gameObject.SetActive(hasDescription);

            // instantiate all items
            foreach (Button.Data buttonData in data.Buttons)
            {
                _instantiatedButtons.Add(UIController.Instance.InstantiateButton(transform, buttonData));
            }
        }
    }
}

