//
// Cuboid (http://github.com/arjonagelhout/cuboid)
// Copyright (c) 2023 Arjo Nagelhout
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid
{
    public static class RectTransformExtensions
    {
        /// <summary>
        /// Fills the entire parent rect transform
        /// </summary>
        public static void Fill(this RectTransform rectTransform)
        {
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        /// <summary>
        /// Convenience method for setting certain properties of a rect transform
        /// </summary>
        public static void Set(this RectTransform rectTransform,
            float? x = null, float? y = null, float? width = null, float? height = null)
        {
            float _x = x.HasValue ? x.Value : rectTransform.anchoredPosition.x;
            float _y = y.HasValue ? y.Value : rectTransform.anchoredPosition.y;
            rectTransform.anchoredPosition = new Vector2(_x, _y);

            float _width = width.HasValue ? width.Value : rectTransform.sizeDelta.x;
            float _height = height.HasValue ? height.Value : rectTransform.sizeDelta.y;
            rectTransform.sizeDelta = new Vector2(_width, _height);
        }
    }

}
