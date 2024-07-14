//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using Cuboid.Models;
using UnityEngine;
using Cuboid.Utils;
using TMPro;
using System.Linq;

namespace Cuboid.UI
{
    /// <summary>
    /// Enable the user to edit properties of the selected reality objects.
    /// </summary>
    public class PropertiesViewController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _headerText;
        [SerializeField] private TextMeshProUGUI _propertiesHeaderText;

        [SerializeField] private GameObject _propertiesHeader;
        [SerializeField] private GameObject _propertiesContent;

        [SerializeField] private GameObject _noObjectsSelectedNotice;

        private SelectionController _selectionController;
        private PropertiesController _propertiesController;

        private Action<Selection> _onSelectionChanged;
        private Action<PropertiesController.RuntimeSerializedPropertiesData> _onActivePropertiesDataChanged;

        private List<GameObject> _instantiatedPropertyGameObjects = new List<GameObject>();


        private void Start()
        {
            _selectionController = SelectionController.Instance;
            _propertiesController = PropertiesController.Instance;

            _setHeaderText = (value) => _headerText.text = value;

            _onActivePropertiesDataChanged = OnActivePropertiesDataChanged;
            _onSelectionChanged = OnSelectionChanged;

            Register();
        }

        private void SetVisible(bool visible)
        {
            _propertiesHeader.SetActive(visible);
        }

        private RealityObjectData _singleSelectedObject;
        private Action<string> _setHeaderText;

        private void OnSelectionChanged(Selection selection)
        {
            if (_singleSelectedObject != null)
            {
                _singleSelectedObject.Name.OnValueChanged -= _setHeaderText;
                _singleSelectedObject = null;
            }

            bool hasObjects = selection.ContainsObjects;

            _noObjectsSelectedNotice.SetActive(!hasObjects);
            _headerText.text = selection.GetString("Properties");

            // This is a bit of an hack, abusing the 
            if (selection.SelectedRealityObjects.Count == 1)
            {
                // register for when the name of that one object changes
                _singleSelectedObject = selection.SelectedRealityObjects.First();
                _singleSelectedObject.Name.OnValueChanged += _setHeaderText;
            }
        }

        private void OnActivePropertiesDataChanged(PropertiesController.RuntimeSerializedPropertiesData propertiesData)
        {
            // first, destroy the previously instantiated properties
            foreach (GameObject instantiatedPropertyGameObject in _instantiatedPropertyGameObjects)
            {
                Destroy(instantiatedPropertyGameObject);
            }
            _instantiatedPropertyGameObjects.Clear();

            bool hasProperties = propertiesData != null && propertiesData.Properties.Count > 0;
            SetVisible(hasProperties);
            if (!hasProperties) { return; }

            _propertiesHeaderText.text = $"{propertiesData.TypeName} properties";

            foreach (PropertiesController.RuntimeSerializedPropertyData propertyData in propertiesData.Properties)
            {
                GameObject newPropertyGameObject = PropertiesController.InstantiateProperty(propertyData, _propertiesContent.transform);
                if (newPropertyGameObject != null)
                {
                    _instantiatedPropertyGameObjects.Add(newPropertyGameObject);
                }
            }
        }

        #region Action registration

        private void Register()
        {
            if (_selectionController != null)
            {
                _selectionController.Selection.Register(_onSelectionChanged);
            }

            if (_propertiesController != null)
            {
                _propertiesController.ActivePropertiesData.Register(_onActivePropertiesDataChanged);
            }
        }

        private void Unregister()
        {
            if (_selectionController != null)
            {
                _selectionController.Selection.Unregister(_onSelectionChanged);
            }

            if (_propertiesController != null)
            {
                _propertiesController.ActivePropertiesData.Unregister(_onActivePropertiesDataChanged);
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
