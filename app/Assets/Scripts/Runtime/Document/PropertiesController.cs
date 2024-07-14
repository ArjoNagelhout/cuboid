// Copyright (c) 2023 Arjo Nagelhout

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cuboid.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cuboid
{
    public class PrettyTypeNameAttribute : Attribute
    {
        public string Name;
    }

    /// <summary>
    /// Base property attribute to subclass from
    /// </summary>
    public class RuntimeSerializedProperty : Attribute
    {
        public string Label = null;

        /// <summary>
        /// Used for sorting
        /// </summary>
        public int Weight = 0;
    }

    public class RuntimeSerializedPropertyFloat : RuntimeSerializedProperty
    {
        public float Min = 0.0f;
        public float Max = 1.0f;
        public bool Slider = true;
        public bool InputField = true;
    }

    public class RuntimeSerializedPropertyInt : RuntimeSerializedProperty
    {
        public int Min = 0;
        public int Max = 1;
        public bool Slider = true;
        public bool InputField = true;
    }

    public class RuntimeSerializedPropertyBoolean : RuntimeSerializedProperty
    {

    }

    public class RuntimeSerializedPropertyColor : RuntimeSerializedProperty
    {

    }

    public class RuntimeSerializedPropertyVector2 : RuntimeSerializedProperty
    {

    }

    public class RuntimeSerializedPropertyVector3 : RuntimeSerializedProperty
    {

    }

    public class RuntimeSerializedPropertyEnum : RuntimeSerializedProperty
    {
        
    }

    public class RuntimeSerializedPropertyString : RuntimeSerializedProperty
    {

    }

    /// <summary>
    /// The properties controller contains a cache of the different RealityObjects and their properties
    /// that were created by using Reflection. 
    /// </summary>
    public sealed class PropertiesController : MonoBehaviour
    {
        private static PropertiesController _instance;
        public static PropertiesController Instance => _instance;

        private SelectionController _selectionController;
        private Action<Selection> _onSelectionChanged;

        private Dictionary<Type, RuntimeSerializedPropertiesData> _propertiesCache = new();

        public Binding<RuntimeSerializedPropertiesData> ActivePropertiesData;

        private void Awake()
        {
            // Singleton implementation
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }
        }

        private void Start()
        {
            _selectionController = SelectionController.Instance;

            _onSelectionChanged = OnSelectionChanged;

            Register();
        }

        public class RuntimeSerializedPropertiesData
        {
            public string TypeName;
            public List<RuntimeSerializedPropertyData> Properties = new List<RuntimeSerializedPropertyData>();
        }

        public class RuntimeSerializedPropertyData
        {
            public RuntimeSerializedProperty PropertyAttribute;
            public FieldInfo FieldInfo;

            internal List<object> _bindings;

            public List<IBinding<T>> GetBindings<T>(out bool differentValues, out T value)
            {
                List<IBinding<T>> targetBindings = new List<IBinding<T>>();

                differentValues = false;
                bool firstValue = true;
                value = default(T);

                foreach (object binding in _bindings)
                {
                    IBinding<T> targetBinding = binding as IBinding<T>;
                    if (targetBinding == null)
                    {
                        continue;
                    }

                    T newValue = targetBinding.Value;

                    if (!firstValue && !EqualityComparer<T>.Default.Equals(value, newValue))
                    {
                        differentValues = true;
                    }

                    value = newValue;
                    firstValue = false;

                    targetBindings.Add(targetBinding);
                }

                return targetBindings;
            }
        }

        private void OnSelectionChanged(Selection selection)
        {
            bool setType = false;
            bool renderProperties = true;
            Type lastType = null;
            foreach (RealityObjectData realityObjectData in selection.SelectedRealityObjects)
            {
                // if one of the reality objects has a different type, don't render the properties editor. Only when of the same type
                // e.g. only Text objects, or Primitive shapes, or RealityAssets. 
                Type type = realityObjectData.GetType();

                if (setType && type != lastType)
                {
                    // types are different, so we should not render the properties
                    renderProperties = false;
                    break;
                }

                lastType = type;
                setType = true;
            }

            if (setType && renderProperties)
            {
                RuntimeSerializedPropertiesData newData = GetPropertiesData(lastType);

                // populate the data with list of bindings
                foreach (RuntimeSerializedPropertyData propertyData in newData.Properties)
                {
                    propertyData._bindings = new List<object>();
                    foreach (RealityObjectData realityObjectData in selection.SelectedRealityObjects)
                    {
                        object binding = propertyData.FieldInfo.GetValue(realityObjectData);
                        Debug.Assert(binding != null, $"Binding {propertyData.FieldInfo.Name} in {lastType.Name} has not been assigned, please do so.");
                        propertyData._bindings.Add(binding);
                    }
                }

                ActivePropertiesData.Value = newData;
            }
            else
            {
                ActivePropertiesData.Value = null;
            }
        }

        /// <summary>
        /// Gets the properties data, if already cached it returns that value.
        /// This because reflection is slow. 
        /// </summary>
        public static RuntimeSerializedPropertiesData GetPropertiesData(Type type)
        {
            // first try to get the properties out of the cache, instead of having to iterate over them each time
            // the selection changes
            if (_instance._propertiesCache.TryGetValue(type, out RuntimeSerializedPropertiesData cachedData))
            {
                return cachedData;
            }

            // get RealityObjectPrettyTypeName attribute
            PrettyTypeNameAttribute prettyTypeNameAttribute = type.GetCustomAttribute<PrettyTypeNameAttribute>();

            // create a new propertiesdata
            RuntimeSerializedPropertiesData data = new RuntimeSerializedPropertiesData()
            {
                // either use the default type name or if defined the pretty type name
                TypeName = prettyTypeNameAttribute != null ? prettyTypeNameAttribute.Name : type.Name
            };

            // we can assume all properties are fields. They should be of type Binding so
            FieldInfo[] fields = type.GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];

                if (!IsValidField(field))
                {
                    continue;
                }

                object[] attributes = field.GetCustomAttributes(true);
                for (int j = 0; j < attributes.Length; j++)
                {
                    object attribute = attributes[j];
                    RuntimeSerializedProperty propertyAttribute = attribute as RuntimeSerializedProperty;

                    if (propertyAttribute != null)
                    {
                        // valid attribute, so add a new entry
                        data.Properties.Add(new RuntimeSerializedPropertyData()
                        {
                            FieldInfo = field,
                            PropertyAttribute = propertyAttribute
                        });

                        // should only expect one attribute per field
                        break;
                    }
                }
            }

            // cache the result
            _instance._propertiesCache.TryAdd(type, data);

            return data;
        }

        private static bool IsValidField(FieldInfo field)
        {
            // https://stackoverflow.com/questions/982487/testing-if-object-is-of-generic-type-in-c-sharp
            // answer by Wiebe Tijsma could be used for more robustness, but for now this is simple enough.

            Type type = field.FieldType;
            if (!type.IsGenericType)
            {
                return false;
            }

            Type[] constraints = type.GetInterfaces();
            foreach (Type constraint in constraints)
            {
                if (constraint.GetGenericTypeDefinition() == typeof(IBinding<>))
                {
                    return true;
                }
            }

            return false;
        }

        private static object GetValue(MemberInfo memberInfo, object forObject)
        {
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)memberInfo).GetValue(forObject);
                case MemberTypes.Property:
                    return ((PropertyInfo)memberInfo).GetValue(forObject);
                default:
                    return null;
            }
        }

        #region Property instantiation

        [Header("Property instantiation")]
        [SerializeField] private GameObject _propertyContainerPrefab;
        [SerializeField] private GameObject _intPropertyPrefab;
        [SerializeField] private GameObject _floatPropertyPrefab;
        [SerializeField] private GameObject _booleanPropertyPrefab;
        [SerializeField] private GameObject _vector2PropertyPrefab;
        [SerializeField] private GameObject _vector3PropertyPrefab;
        [SerializeField] private GameObject _colorPropertyPrefab;
        [SerializeField] private GameObject _enumPropertyPrefab;
        [SerializeField] private GameObject _stringPropertyPrefab;

        public static GameObject InstantiateProperty(RuntimeSerializedPropertyData propertyData, Transform parent, bool canUndo = true)
        {
            return propertyData.PropertyAttribute switch
            {
                RuntimeSerializedPropertyInt => InstantiatePropertyInternal<int>(propertyData, _instance._intPropertyPrefab, parent, canUndo),
                RuntimeSerializedPropertyFloat => InstantiatePropertyInternal<float>(propertyData, _instance._floatPropertyPrefab, parent, canUndo),
                RuntimeSerializedPropertyBoolean => InstantiatePropertyInternal<bool>(propertyData, _instance._booleanPropertyPrefab, parent, canUndo),
                RuntimeSerializedPropertyVector2 => InstantiatePropertyInternal<Vector2>(propertyData, _instance._vector2PropertyPrefab, parent, canUndo),
                RuntimeSerializedPropertyVector3 => InstantiatePropertyInternal<Vector3>(propertyData, _instance._vector3PropertyPrefab, parent, canUndo),
                RuntimeSerializedPropertyColor => InstantiatePropertyInternal<RealityColor>(propertyData, _instance._colorPropertyPrefab, parent, canUndo),
                RuntimeSerializedPropertyString => InstantiatePropertyInternal<string>(propertyData, _instance._stringPropertyPrefab, parent, canUndo),
                RuntimeSerializedPropertyEnum => InstantiatePropertyGeneric(propertyData, _instance._enumPropertyPrefab, parent, canUndo),
                _ => null,
            };
        }

        private static GameObject InstantiatePropertyGeneric(
            RuntimeSerializedPropertyData propertyData,
            GameObject prefab,
            Transform parent,
            bool canUndo = true)
        {
            Type fieldType = propertyData.FieldInfo.FieldType;
            if (!fieldType.IsGenericType) { return null; }

            Type[] typeArguments = fieldType.GenericTypeArguments;
            if (typeArguments.Length == 0) { return null; }

            Type enumType = typeArguments[0];

            var method = typeof(PropertiesController).GetMethod("InstantiatePropertyInternal", BindingFlags.Static | BindingFlags.NonPublic);
            var genericMethod = method.MakeGenericMethod(enumType);
            object resultObject = genericMethod.Invoke(null, new object[] { propertyData, prefab, parent, canUndo});
            GameObject result = (GameObject)Convert.ChangeType(resultObject, typeof(GameObject));

            return result;
        }

        /// <summary>
        /// parent is for example _propertiesContent.transform
        ///
        /// canUndo is used for ToolProperties vs object properties. ToolProperties shouldn't get registerd. 
        /// </summary>
        private static GameObject InstantiatePropertyInternal<T>(
            RuntimeSerializedPropertyData propertyData,
            GameObject prefab,
            Transform parent,
            bool canUndo = true)
        {
            // first instantiate the container, this is the same for each property
            GameObject newPropertyContainerGameObject = Instantiate(_instance._propertyContainerPrefab, parent, false);
            PropertyContainer newPropertyContainer = newPropertyContainerGameObject.GetComponent<PropertyContainer>();

            // set property name based on Label in attribute or Field name as default. 
            newPropertyContainer.PropertyName = propertyData.PropertyAttribute.Label != null ?
                propertyData.PropertyAttribute.Label : propertyData.FieldInfo.Name;

            Transform propertyContainerTransform = newPropertyContainer.PropertyContentRectTransform;
            GameObject gameObject = Instantiate(prefab, propertyContainerTransform, false);

            if (gameObject.TryGetComponent<IGenericProperty>(out IGenericProperty genericPropertyInterface))
            {
                Property<T> genericProperty = genericPropertyInterface.CreateProperty<T>();

                genericProperty.CanUndo = canUndo;
                genericProperty.Data = propertyData;

                return newPropertyContainerGameObject;
            }

            if (!gameObject.TryGetComponent<IProperty<T>>(out IProperty<T> propertyInterface))
            {
                Debug.Log("Instantiated GameObject does not implement IProperty interface");
                return newPropertyContainerGameObject;
            }

            Property<T> property = propertyInterface.Property;
            if (property == null)
            {
                Debug.Log("Object doesn't contain Property");
                return newPropertyContainerGameObject;
            }

            property.CanUndo = canUndo;
            property.Data = propertyData;

            return newPropertyContainerGameObject;
        }

        #endregion

        #region Action registration

        private void Register()
        {
            if (_selectionController != null)
            {
                _selectionController.Selection.Register(_onSelectionChanged);
            }
        }

        private void Unregister()
        {
            if (_selectionController != null)
            {
                _selectionController.Selection.Unregister(_onSelectionChanged);
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
