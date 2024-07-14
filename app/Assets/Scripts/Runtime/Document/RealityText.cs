//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid
{
    public class RealityTextData : RealityObjectData
    {
        public override IEnumerator InstantiateAsync(Action<RealityObject> completed)
        {
            throw new NotImplementedException();
        }
    }

    public class RealityText : RealityObject
    {
        
    }
}

