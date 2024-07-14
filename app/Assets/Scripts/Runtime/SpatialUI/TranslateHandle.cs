//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid
{
    public class TranslateHandleData : AxisHandleData
    {
        public enum HandleType
        {
            Plane,
            Axis
        }

        public HandleType handleType;
    }

    public class TranslateHandle : AxisHandle<TranslateHandleData>
    {
        
    }
}
