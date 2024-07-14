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
    /// 
    /// </summary>
    [DisallowMultipleComponent]
    public class ConstantScaleOnScreenComponent : MonoBehaviour
    {
        [SerializeField] private float _referenceScaleAtOneMeterDistance;

        private void LateUpdate()
        {
            transform.localScale = ConstantScaleOnScreen.GetConstantScale(transform.position, _referenceScaleAtOneMeterDistance);
        }
    }
}
