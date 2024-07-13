Shader "ShapeReality/ColorPicker"
{
    Properties
    {
        _Hue ("Hue", Float) = 0

        [KeywordEnum(HSV, RGB)] _Type("Type", Float) = 0

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
            // 0 hsv (saturation on x axis, value on y axis)
            // 1 rgb (hue on x axis, value on y axis)
            #pragma shader_feature_local _TYPE_HSV _TYPE_RGB

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
                float y = IN.texcoord.y;

                float3 color = float3(1, 1, 1);
                
                #if _TYPE_HSV
                // 0 hsv (saturation on x axis, value on y axis)
                // get the hue

                float3 base_rgb = hue06_to_base_rgb(_Hue * 6);
                color = sv_to_rgb(base_rgb, x, y);

                #elif _TYPE_RGB
                // 1 rgb (hue on x axis, value on y axis)

                float3 base_rgb = hue06_to_base_rgb(x * 6);
                color = sv_to_rgb(base_rgb, 1, y);

                #endif

                return fixed4(color, 1);
            }
            
            ENDCG
        }
    }
}
