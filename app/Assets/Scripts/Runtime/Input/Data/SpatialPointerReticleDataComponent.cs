using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.Input
{
    /// <summary>
    /// Allows simply adding this to any GameObject to make it support a custom reticle,
    /// without having to implement the interface in a component each time. 
    /// </summary>
    public class SpatialPointerReticleDataComponent : MonoBehaviour,
        ISpatialPointerCustomReticle
    {
        [SerializeField] private SpatialPointerReticleData _reticleData;
        public SpatialPointerReticleData ReticleData
        {
            get => _reticleData;
            set
            {
                _reticleData = value;
            }
        }
    }
}
