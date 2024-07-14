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
    /// <summary>
    /// Should't be subclassed, but a property should be added next to it and simply
    /// call the method on this class Property.
    ///
    /// This is to make sure that the stored data (_textMesh and _propertyContentRectTransform)
    /// doesn't have to be reassigned for each prefab variant that subclasses from property. 
    /// </summary>
    public sealed class PropertyContainer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _textMesh;
        [SerializeField] private RectTransform _propertyContentRectTransform;

        public RectTransform PropertyContentRectTransform => _propertyContentRectTransform;
        
        public string PropertyName
        {
            set => _textMesh.text = value;
        }
    }
}
