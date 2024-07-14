//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Cuboid.UI
{
    public class TooltipPopup : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _description;

        private Tooltip.TooltipData _data;
        public Tooltip.TooltipData Data
        {
            get => _data;
            set
            {
                _data = value;
                OnDataChanged(_data);
            }
        }

        private void OnDataChanged(Tooltip.TooltipData data)
        {
            bool hasTitle = data.Title != null;
            bool hasDescription = data.Description != null;

            _title.gameObject.SetActive(hasTitle);
            _description.gameObject.SetActive(hasDescription);

            if (hasTitle)
            {
                _title.text = data.Title;
            }

            if (hasDescription)
            {
                _description.text = data.Description;
            }
        }
    }
}
