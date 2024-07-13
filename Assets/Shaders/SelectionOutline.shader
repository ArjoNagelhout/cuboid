Shader "ShapeReality/SelectionOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width", Float) = 1
    }
    SubShader
    {
        // Outline pass
        Tags
        {
            "RenderType"="Opaque"
        }
        Pass
        {
            Cull Front

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            struct Attributes 
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct Varyings
            {
                float4 vertex : SV_POSITION;
            };

            half4 _OutlineColor;
            half _OutlineWidth;
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // clip space
                // https://alexanderameye.github.io/notes/rendering-outlines/

                OUT.vertex = TransformObjectToHClip(IN.vertex.xyz);
                float3 normalHCS = mul((float3x3)UNITY_MATRIX_VP, mul((float3x3)UNITY_MATRIX_M, IN.normal));

                OUT.vertex.xy += normalize(normalHCS.xy) / _ScreenParams.xy * OUT.vertex.w * _OutlineWidth * 2;

                return OUT;
            }

            half4 frag() : SV_Target
            {
                return _OutlineColor;
            }

            ENDHLSL
        }
    }
}
