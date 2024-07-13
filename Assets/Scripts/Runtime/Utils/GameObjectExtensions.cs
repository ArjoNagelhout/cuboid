using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid
{
    public static class GameObjectExtensions
    {
        public static void SetLayerRecursively(this GameObject self, int layer)
        {
            Transform[] children = self.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                child.gameObject.layer = layer;
            }
        }
    }
}
