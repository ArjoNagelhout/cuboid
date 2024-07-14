//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace Cuboid.UI
{
    /// <summary>
    /// Notification that can be used to show errors, warnings or success messages.
    ///
    /// Can't be interacted with. 
    /// </summary>
    public class Notification : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _description;

        private const float k_DefaultDisplayDurationInSeconds = 1.0f;

        public class Data
        {
            public Sprite Icon;

            /// <summary>
            /// The color of the icon, can be useful for showing a warning icon vs
            /// an error, or success. This, because the icon set is not colored,
            /// so we have to set the color manually. 
            /// </summary>
            public Color IconColor = Color.white;

            public string Title;
            public string Description;

            public float DisplayDurationInSeconds = k_DefaultDisplayDurationInSeconds;
        }

        private Data _activeData;
        public Data ActiveData
        {
            get => _activeData;
            set
            {
                _activeData = value;
                OnDataChanged(_activeData);
            }
        }

        public void DataChanged()
        {
            OnDataChanged(ActiveData);
        }

        private void OnDataChanged(Data data)
        {
            bool hasIcon = data.Icon != null;
            _icon.color = hasIcon ? data.IconColor : Color.clear;

            bool hasTitle = data.Title != null;
            _title.gameObject.SetActive(hasTitle);
            if (hasTitle)
            {
                _title.text = data.Title;
            }

            bool hasDescription = data.Description != null;
            _description.gameObject.SetActive(hasDescription);
            if (hasDescription)
            {
                _description.text = data.Description;
            }
        }
    }

}
