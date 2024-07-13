// 
// SerializableDictionary.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Cuboid.Utils
{
    /// <summary>
    /// Because Unity doesn't allow serializing a dictionary
    /// </summary>
    [Serializable]
    public class SerializableDictionary<Key, Value>
    {
        [Serializable]
        public struct KeyValuePair
        {
            public Key Key;
            public Value Value;
        }

        [SerializeField] private KeyValuePair[] _keyValuePairsList = new KeyValuePair[] { };

        /// <summary>
        /// Dictionary of prefabs, can be accessed by a certain enum value
        /// </summary>
        private Dictionary<Key, Value> _dictionary;
        public Dictionary<Key, Value> Dictionary
        {
            get
            {
                if (_dictionary == null)
                {
                    _dictionary = Populate(_keyValuePairsList);
                }
                return _dictionary;
            }
        }

        public Value this[Key key]
        {
            get => Dictionary[key];
        }

        public bool TryGetValue(Key key, out Value value)
        {
            return Dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Creates a dictionary from the list of prefabs, so that they can be easily
        /// accessed through the dictionary instead of iterating through the list every
        /// time. 
        /// </summary>
        /// <param name="prefabsList"></param>
        /// <returns></returns>
        private static Dictionary<Key, Value> Populate(
            KeyValuePair[] keyValuePairs)
        {
            Dictionary<Key, Value> dictionary = new Dictionary<Key, Value>();
            foreach (KeyValuePair keyValuePair in keyValuePairs)
            {
                dictionary.TryAdd(keyValuePair.Key, keyValuePair.Value);
            }

            return dictionary;
        }
    }
}
