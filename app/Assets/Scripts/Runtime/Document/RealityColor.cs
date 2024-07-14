// Copyright (c) 2023 Arjo Nagelhout

using System.Collections;
using System.Collections.Generic;
using Cuboid.UI;
using Unity.Mathematics;
using UnityEngine;

namespace Cuboid
{
    public enum RealityColorMode
    {
        RGB,
        HSV
    }

    public struct RealityColor
    {
        /// <summary>
        /// whether the color was set using RGB or HSV.  
        /// </summary>
        public RealityColorMode Mode;

        public byte r; // 0 - 255
        public byte g; // 0 - 255
        public byte b; // 0 - 255

        public float hue; // 0 - 1
        public float saturation; // 0 - 1
        public float value; // 0 - 1

        public Color32 ToColor32()
        {
            switch (Mode)
            {
                case RealityColorMode.RGB:
                    return new Color32(r, g, b, (byte)255f);
                default:
                case RealityColorMode.HSV:
                    float3 hsv = new float3(hue, saturation, value);
                    float3 rgb = ColorUtils.HSVToRGB_0_1(hsv);
                    return ColorUtils.RGBToColor32_0_1(rgb);
            }
        }

        public RealityColor(RealityColorMode mode, byte r, byte g, byte b, float hue, float saturation, float value)
        {
            Mode = mode;
            this.r = r;
            this.g = g;
            this.b = b;
            this.hue = hue;
            this.saturation = saturation;
            this.value = value;
        }

        public static RealityColor FromColor32(Color32 color)
        {
            return RGB(color.r, color.g, color.b);
        }

        // these static methods are done to ensure that which is meant (RGB or HSV) is explicit
        // instead of implicit by using different constructors. 

        public static RealityColor RGB(byte r, byte g, byte b)
        {
            // set hsv as well
            float3 rgb = new float3((float)r / 255f, (float)g / 255f, (float)b / 255f);
            float3 hsv = ColorUtils.RGBToHSV_0_1(rgb);
            return new RealityColor(RealityColorMode.RGB, r, g, b, hsv.x, hsv.y, hsv.z);
        }

        public static RealityColor HSV(float hue, float saturation, float value)
        {
            // set rgb as well
            float3 rgb = ColorUtils.HSVToRGB_0_1(new float3(hue, saturation, value));
            byte r = (byte)(rgb.x * 255f);
            byte g = (byte)(rgb.y * 255f);
            byte b = (byte)(rgb.z * 255f);
            return new RealityColor(RealityColorMode.HSV, r, g, b, hue, saturation, value);
        }
    }
}
