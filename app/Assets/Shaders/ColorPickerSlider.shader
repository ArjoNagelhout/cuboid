Shader "ShapeReality/ColorPickerSlider"
{
    Properties
    {
        _MainColor ("Main color", Color) = (1, 1, 1, 1)
        _Hue ("Hue", Float) = 0
        _Saturation ("Saturation", Float) = 0

        [KeywordEnum(Hue, Saturation, Value, Red, Green, Blue)] _Type("Type", Float) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "Assets/Shaders/Include/ColorSpace.cginc"

            // types:
            // 0 Hue
            // 1 Saturation
            // 2 Value
            // 
            // 3 Red
            // 4 Green
            // 5 Blue
            #pragma shader_feature_local _TYPE_HUE _TYPE_SATURATION _TYPE_VALUE _TYPE_RED _TYPE_GREEN _TYPE_BLUE

            struct Attributes
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
                float2 texcoord  : TEXCOORD0;
            };

            float4 _MainColor;
            float _Hue;
            float _Saturation;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                return OUT;
            }

            fixed4 frag(Varyings IN) : SV_Target
            {
                float x = IN.texcoord.x;

                float3 color = float3(1, 1, 1);
                #if _TYPE_HUE

                float hue = x * 6; // texcoord is from 0, 1, this is the hue
                float3 base_rgb = hue06_to_base_rgb(hue);
                color = sv_to_rgb(base_rgb, 1, 1);

                #elif _TYPE_SATURATION

                // the saturation is the x
                // 1. so first go from rgb to hsv
                float3 hsv = rgb_to_hsv(_MainColor.xyz);

                hsv.x = _Hue == -1 ? hsv.x : _Hue;

                // 2. then set the s to x
                hsv.y = x;

                // 3. then hsv to rgb
                color = hsv_to_rgb(hsv);

                #elif _TYPE_VALUE

                // the value is the x
                // 1. so first go from rgb to hsv
                float3 hsv = rgb_to_hsv(_MainColor.xyz);

                // correct for when the color is black. 
                hsv.x = _Hue == -1 ? hsv.x : _Hue;
                hsv.y = _Saturation == -1 ? hsv.y : _Saturation;

                // 2. then set the v to x
                hsv.z = x;

                // 3. then hsv to rgb
                color = hsv_to_rgb(hsv);

                #elif _TYPE_RED

                float2 gb = _MainColor.yz;
                color = float3(x, gb);

                #elif _TYPE_GREEN

                float r = _MainColor.x;
                float b = _MainColor.z;
                color = float3(r, x, b);

                #elif _TYPE_BLUE

                float2 rg = _MainColor.xy;
                color = float3(rg, x);

                #endif

                return fixed4(color, 1);
            }
            
            ENDCG
        }
    }
}
