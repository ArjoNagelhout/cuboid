// 
// SortUtils.cs
// Cuboid
// 
// Copyright 2023 ShapeReality
// 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.Input
{
    /// <summary>
    /// Utility functions related to sorting.
    /// </summary>
    public static class SortUtils
    {
        public static void Sort<T>(IList<T> hits, IComparer<T> comparer) where T : struct
        {
            bool fullPass;
            do
            {
                fullPass = true;
                for (var i = 1; i < hits.Count; ++i)
                {
                    var result = comparer.Compare(hits[i - 1], hits[i]);
                    if (result > 0)
                    {
                        var temp = hits[i - 1];
                        hits[i - 1] = hits[i];
                        hits[i] = temp;
                        fullPass = false;
                    }
                }
            } while (fullPass == false);
        }
    }
}
