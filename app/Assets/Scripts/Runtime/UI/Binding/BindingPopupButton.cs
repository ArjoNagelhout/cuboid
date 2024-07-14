//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.UI
{
    [RequireComponent(typeof(PopupButton))]
    public class BindingPopupButton : MonoBehaviour
    {
        [System.Serializable]
        public enum Identifier
        {
            Document_ViewingMode
        }

        [Header("Data Binding")]
        [SerializeField] private Identifier _identifier;

        private PopupButton _popupButton;

        private void Start()
        {
            _popupButton = GetComponent<PopupButton>();

            switch (_identifier)
            {
                case Identifier.Document_ViewingMode:
                    RealityDocument document = RealityDocumentController.Instance.RealityDocument.Value;
                    if (document.ActiveViewingMode == null) { document.ActiveViewingMode = new(); }
                    _popupButton.CreatePopupButton(document.ActiveViewingMode);
                    break;
                default:
                    break;
            }
        }
    }
}
