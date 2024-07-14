//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Cuboid
{
    public class Selection
    {
        public HashSet<RealityObjectData> SelectedRealityObjects = new HashSet<RealityObjectData>();

        public bool CanSelectAll => (SelectedRealityObjects.Count <
            RealitySceneController.Instance.InstantiatedRealityObjects.Value.Count)
            && (RealitySceneController.Instance.InstantiatedRealityObjects.Value.Count > 0);

        public bool ContainsObjects => SelectedRealityObjects.Count > 0;

        public string GetString(string defaultStringIfNoObjects)
        {
            int count = SelectedRealityObjects.Count;
            if (count == 0)
            {
                // no objects, just use "Properties"
                return defaultStringIfNoObjects;
            }

            if (count == 1)
            {
                return SelectedRealityObjects.First().Name.Value;
            }
            
            // just display the amount of objects
            return $"{count} objects";
        }
    }
}

