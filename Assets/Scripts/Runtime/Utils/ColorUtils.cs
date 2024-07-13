using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace Cuboid
{

    /// <summary>
    /// Helper functions for converting between types 
    /// </summary>
    public static class ColorUtils
    {
        /// <summary>
        /// Converts Color32 to RGB, returns rgb in 0 - 255
        /// </summary>
        public static float3 Color32ToRGB_0_255(Color32 color)
        {
            return new float3(color.r, color.g, color.b);
        }

        /// <summary>
        /// Converts RGB To Color32, expects rgb in 0 - 255
        ///
        /// Note: sets alpha to 1
        /// </summary>       
        public static Color32 RGBToColor32_0_255(float3 rgb)
        {
            return new Color32((byte)rgb.x, (byte)rgb.y, (byte)rgb.z, (byte)255f);
        }

        /// <summary>
        /// Converts Color32 to RGB, returns rgb in 0 - 1
        /// </summary>
        public static float3 Color32ToRGB_0_1(Color32 color)
        {
            return new float3(color.r / 255f, color.g / 255f, color.b / 255f);
        }

        /// <summary>
        /// Converts RGB To Color32, expects range 0 - 1
        /// </summary>       
        public static Color32 RGBToColor32_0_1(float3 rgb)
        {
            return new Color32((byte)(rgb.x * 255f), (byte)(rgb.y * 255f), (byte)(rgb.z * 255f), (byte)255f);
        }

        // // HSV to RGB.  Get base_rgb from xxx_to_base_rgb.
        //float3 sv_to_rgb(in float3 base_rgb, float saturation, float value)
        //{
        //    return ((base_rgb - 1) * saturation + 1) * value;
        //}
        //public static Color32 SVToRGB(float3)

        // hue06 is in [0,6]
        public static float3 Hue06ToBaseRGB(float hue06)
        {
            float r = -1 + abs(hue06 - 3);
            float g = 2 - abs(hue06 - 2);
            float b = 2 - abs(hue06 - 4);
            return saturate(float3(r, g, b));
        }

        // hue33 is in [-3,3]
        public static float3 Hue33ToBaseRGB(float hue33)
        {
            float r = -abs(hue33) + 2;
            float g = abs(hue33 + 1) - 1;
            float b = abs(hue33 - 1) - 1;
            return saturate(float3(r, g, b));
        }

        // HSV to RGB.  Get base_rgb from xxx_to_base_rgb.
        public static float3 SVToRGB(float3 base_rgb, float saturation, float value)
        {
            return ((base_rgb - 1) * saturation + 1) * value;
        }

        // Function from https://thebookofshaders.com/glossary/?search=fract
        public static float3 fract(float3 value)
        {
            return value - floor(value);
        }

        /// <summary>
        /// Expects RGB in 0 - 1
        /// Returns HSV in 0 - 1
        /// </summary>
        public static float3 RGBToHSV_0_1(float3 c)
        {
            float4 K = float4(0.0f, -1.0f / 3.0f, 2.0f / 3.0f, -1.0f);
            float4 p = lerp(float4(c.zy, K.wz), float4(c.yz, K.xy), step(c.z, c.y));
            float4 q = lerp(float4(p.xyw, c.x), float4(c.x, p.yzx), step(p.x, c.x));

            float d = q.x - min(q.w, q.y);
            float e = 1.0e-10f;
            return float3(abs(q.z + (q.w - q.y) / (6.0f * d + e)), d / (q.x + e), q.x);
        }

        /// <summary>
        /// Expects HSV in 0 - 1
        /// Returns rgb in 0 - 1
        /// </summary>
        public static float3 HSVToRGB_0_1(float3 c)
        {
            float4 K = float4(1.0f, 2.0f / 3.0f, 1.0f / 3.0f, 3.0f);
            float3 p = abs(fract(c.xxx + K.xyz) * 6.0f - K.www);
            return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0f, 1.0f), c.y);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static Color32 UIntToColor(uint number)
        {
            byte[] bytes = BitConverter.GetBytes(number);
            return new Color32(bytes[0], bytes[1], bytes[2], bytes[3]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static uint ColorToUInt(Color32 color)
        {
            return BitConverter.ToUInt32(color.ToByteArray(), 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static byte[] ToByteArray(this Color32 color)
        {
            return new[] { color.r, color.g, color.b, color.a };
        }
    }
}
