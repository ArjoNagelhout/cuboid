// Copyright (c) 2023 Arjo Nagelhout

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.Utils
{
    public static class FloatExtensions
    {
        public const float k_RoughlyEqualsValue = 0.01f;

        public static bool RoughlyEquals(this float value, float otherValue)
        {
            return Mathf.Abs(otherValue - value) < k_RoughlyEqualsValue;
        }
    }
}