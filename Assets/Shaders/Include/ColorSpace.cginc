// Copyright 2020 The Tilt Brush Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

// -*- c -*-

// hue06 is in [0,6]
float3 hue06_to_base_rgb(in float hue06) {
  float r = -1 + abs(hue06 - 3);
  float g =  2 - abs(hue06 - 2);
  float b =  2 - abs(hue06 - 4);
  return saturate(float3(r, g, b));
}

// hue33 is in [-3,3]
float3 hue33_to_base_rgb(in float hue33) {
  float r = -abs(hue33  ) + 2;
  float g =  abs(hue33+1) - 1;
  float b =  abs(hue33-1) - 1;
  return saturate(float3(r, g, b));
}

// HSV to RGB.  Get base_rgb from xxx_to_base_rgb.
float3 sv_to_rgb(in float3 base_rgb, float saturation, float value) {
  return ((base_rgb - 1) * saturation + 1) * value;
}

// Function from https://thebookofshaders.com/glossary/?search=fract
float3 fract(in float3 value)
{
    return value - floor(value);
}

// RGB to HSV
// from https://gamedev.stackexchange.com/questions/59797/glsl-shader-change-hue-saturation-brightness
// initially I used the RGBtoHSV functions from the TiltBrush source code but these were
// horribly unoptimized.
float3 rgb_to_hsv(in float3 c)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float3 hsv_to_rgb(in float3 c)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}
