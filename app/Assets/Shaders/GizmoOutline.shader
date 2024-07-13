Shader "ShapeReality/GizmoOutline"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (1, 1, 1, 1)
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width", float) = 1
    }
    SubShader
    {
        HLSLINCLUDE
        
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes 
        {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
        };

        struct Varyings
        {
            float4 vertex : SV_POSITION;
        };

        ENDHLSL

        LOD 100

        Tags
        {
            // https://stackoverflow.com/questions/72533739/unity-change-color-in-mesh-overlap-using-urp
            // Multipass shaders don't normally work in the URP, so you need to add "LightMode" tags
            // in the first pass "SRPDefaultUnlit" and second pass "UniversalForward".
            // this is stupid and hacky, but easier to read / maintain than having to rely on a
            // renderer feature for multiple passes.
            //
            // Also, the outline shader by vertex offset can't be converted to a single pass shader.
            "RenderType"="Opaque"
            "LightMode" = "SRPDefaultUnlit"
        }
        // Main color pass
        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            half4 _MainColor;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.vertex = TransformObjectToHClip(IN.vertex.xyz);
                return OUT;
            }

            half4 frag() : SV_Target
            {
                // sample the texture
                return _MainColor;
            }
            
            ENDHLSL
        }


        // Outline pass
        Tags
        {
            "LightMode" = "UniversalForward"
        }
        Pass
        {
            Cull Front

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            half4 _OutlineColor;
            half _OutlineWidth;
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // naive implementation
                // float3 position = IN.vertex.xyz;
                // position += IN.normal * _OutlineWidth;
                // OUT.vertex = TransformObjectToHClip(position);

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
