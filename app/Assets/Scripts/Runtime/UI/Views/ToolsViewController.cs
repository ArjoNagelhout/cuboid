//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cuboid.Input;
using UnityEngine.UI;
using TMPro;

namespace Cuboid.UI
{
    /// <summary>
    /// The tools view controller is responsible for displaying all tools
    ///
    /// It instantiates a view controller for the currently instantiated tool,
    /// so that the user can change settings for the given tool, such as
    /// for the Draw Shape Tool the corner radius or corner quality, and which
    /// shape to draw (e.g. Rectangle, Cube)
    /// </summary>
    public class ToolsViewController : MonoBehaviour
    {
        private InputController _inputController;
        private ToolController _toolController;

        private Action<Handedness.Hand> _onDominantHandChanged;
        private Action<GameObject> _onInstantiatedToolChanged;

        [SerializeField] private HorizontalLayoutGroup _horizontalLayoutGroup;

        [SerializeField] private RectTransform _toolPropertiesView;

        [SerializeField] private TextMeshProUGUI _headerText; // used for the name of the tool
        [SerializeField] private GameObject _propertiesContent; // used for instantiating the properties inside

        [SerializeField] private List<GameObject> _gameObjectsToDisableWhenNoProperties = new List<GameObject>();

        private List<GameObject> _instantiatedPropertyGameObjects = new List<GameObject>();

        private void Start()
        {
            _inputController = InputController.Instance;
            _toolController = ToolController.Instance;

            _onDominantHandChanged = OnDominantHandChanged;
            _onInstantiatedToolChanged = OnInstantiatedToolChanged;

            Register();
        }

        private void OnDominantHandChanged(Handedness.Hand hand)
        {
            // set the offset of the tool bar and tool options view.
            _horizontalLayoutGroup.reverseArrangement = hand == Handedness.Hand.RightHand;
        }

        private void OnInstantiatedToolChanged(GameObject go)
        {
            if (go == null) { return; }

            foreach (GameObject instantiatedPropertyGameObject in _instantiatedPropertyGameObjects)
            {
                Destroy(instantiatedPropertyGameObject);
            }
            _instantiatedPropertyGameObjects.Clear();

            bool hasProperties = go.TryGetComponent<IToolHasProperties>(out IToolHasProperties tool);

            // THIS IS IMPORTANT TO DO BEFORE INSTANTIATING THE PROPERTIES,
            // because otherwise the Awake method in the properties won't get called, because the parent
            // game object is inactive. 
            foreach (GameObject gameObjectToDisable in _gameObjectsToDisableWhenNoProperties)
            {
                gameObjectToDisable.SetActive(hasProperties);
            }

            if (hasProperties)
            {
                Type type = tool.GetType();
                PropertiesController.RuntimeSerializedPropertiesData propertiesData = PropertiesController.GetPropertiesData(type);

                foreach (PropertiesController.RuntimeSerializedPropertyData propertyData in propertiesData.Properties)
                {
                    propertyData._bindings = new List<object>();
                    object binding = propertyData.FieldInfo.GetValue(tool);
                    Debug.Assert(binding != null, $"Binding {propertyData.FieldInfo.Name} in {type.Name} has not been assigned, please do so.");
                    propertyData._bindings.Add(binding);
                }

                _headerText.text = propertiesData.TypeName;
                hasProperties = true;

                foreach (PropertiesController.RuntimeSerializedPropertyData propertyData in propertiesData.Properties)
                {
                    GameObject newPropertyGameObject = PropertiesController.InstantiateProperty(propertyData, _propertiesContent.transform, false);
                    if (newPropertyGameObject != null)
                    {
                        _instantiatedPropertyGameObjects.Add(newPropertyGameObject);
                    }
                }
            }
        }

        #region Action registration

        private void Register()
        {
            if (_inputController != null)
            {
                _inputController.Handedness.DominantHand.Register(_onDominantHandChanged);
            }

            if (_toolController != null)
            {
                _toolController.InstantiatedTool.Register(_onInstantiatedToolChanged);
            }
        }

        private void Unregister()
        {
            if (_inputController != null)
            {
                _inputController.Handedness.DominantHand.Unregister(_onDominantHandChanged);
            }

            if (_toolController != null)
            {
                _toolController.InstantiatedTool.Unregister(_onInstantiatedToolChanged);
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

