//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.Utils
{
    /// <summary>
    /// Initially I made the PrefabDictionary create its own GameObject + MonoBehaviour
    /// for starting and stopping coroutines, and store them in a static variable.
    /// However, because PrefabDictionary is generic, this static variable will be
    /// different for each type and thus create a separate GameObject for each
    /// type.
    /// This only requires one :)
    /// </summary>
    public class PrefabDictionaryHelper : MonoBehaviour
    {
        private static PrefabDictionaryHelper _instance;
        public static PrefabDictionaryHelper Instance => _instance;

        private void Awake()
        {
            // Singleton implemention
            if (_instance != null && _instance != this) { Destroy(this); } else { _instance = this; }
        }
    }
}
