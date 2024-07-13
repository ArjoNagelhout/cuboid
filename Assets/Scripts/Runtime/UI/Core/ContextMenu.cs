// 
// ContextMenu.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Cuboid.UI
{
    /// <summary>
    /// Context menu that can be instantiated and populated with actions
    /// </summary>
    public class ContextMenu : MonoBehaviour
    {
        /// <summary>
        /// 
        /// </summary>
        public class ContextMenuData
        {
            /// <summary>
            /// 
            /// </summary>
            public string Title;

            /// <summary>
            /// 
            /// </summary>
            public List<Button.Data> Buttons;
        }

        [SerializeField] private TextMeshProUGUI _textMesh;

        private List<Button> _instantiatedButtons = new List<Button>();
        
        private ContextMenuData _data;
        public ContextMenuData Data
        {
            get => _data;
            set
            {
                _data = value;
                OnDataChanged(_data);
            }
        }

        private void OnDataChanged(ContextMenuData data)
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
                _textMesh.text = data.Title;
            }
            _textMesh.gameObject.SetActive(hasTitle);

            // instantiate all items
            foreach (Button.Data buttonData in data.Buttons)
            {
                //buttonData.Variant = ButtonColors.Variant.Plain;
                _instantiatedButtons.Add(UIController.Instance.InstantiateButton(transform, buttonData));
            }
        }
    }
}
