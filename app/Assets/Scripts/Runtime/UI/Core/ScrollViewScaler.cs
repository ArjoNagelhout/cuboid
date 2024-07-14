// Copyright (c) 2023 Arjo Nagelhout

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid
{
    public class ScrollViewScaler : MonoBehaviour
    {
        public RectTransform RectTransform;

        public void SetScale(float scale, float yOffset)
        {
            transform.localPosition = transform.localPosition.SetY(yOffset);
            transform.localScale = Vector3.one * scale;
        }
    }
}
