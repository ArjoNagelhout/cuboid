//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cuboid.UI
{
    /// <summary>
    /// 
    /// </summary>
    public class OpenColorsPanelButton : MonoBehaviour,
        IPointerClickHandler
    {
        private UIController _uiController;
        private Action<UIController.Panel> _onActivePanelChanged;

        private ColorsController _colorsController;
        private Action<RealityColor> _onActiveColorChanged;

        [SerializeField] private Image _selectedImage;

        [SerializeField] private Image _colorImage;

        private void Start()
        {
            _colorsController = ColorsController.Instance;
            _onActiveColorChanged = OnActiveColorChanged;

            _uiController = UIController.Instance;
            _onActivePanelChanged = OnActivePanelChanged;

            Register();
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            _uiController.ActivePanel.Value = UIController.Panel.Colors;
        }

        private void OnActivePanelChanged(UIController.Panel panel)
        {
            _selectedImage.enabled = panel == UIController.Panel.Colors;
        }

        private void OnActiveColorChanged(RealityColor color)
        {
            _colorImage.color = color.ToColor32();
        }

        #region Action registration

        private void Register()
        {
            if (_uiController != null)
            {
                _uiController.ActivePanel.Register(_onActivePanelChanged);
            }

            if (_colorsController != null)
            {
                _colorsController.ActiveColor.Register(_onActiveColorChanged);
            }
        }

        private void Unregister()
        {
            if (_uiController != null)
            {
                _uiController.ActivePanel.Unregister(_onActivePanelChanged);
            }

            if (_colorsController != null)
            {
                _colorsController.ActiveColor.Unregister(_onActiveColorChanged);
            }
        }

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void OnDestroy()
        {
            Unregister();
        }

        #endregion
    }
}
