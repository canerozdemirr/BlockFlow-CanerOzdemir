Shader "BlockFlow/BlockClipPlane"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _BaseMap("Base Map", 2D) = "white" {}
        _SpecColor("Specular Color", Color) = (0.5, 0.5, 0.5, 0.5)
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5

        // Clip plane: world-space point and normal
        _ClipPlanePos("Clip Plane Position", Vector) = (0,0,0,0)
        _ClipPlaneNormal("Clip Plane Normal", Vector) = (0,0,0,0)
        _ClipEnabled("Clip Enabled", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float fogCoord : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float4 _SpecColor;
                float _Smoothness;
                float4 _ClipPlanePos;
                float4 _ClipPlaneNormal;
                float _ClipEnabled;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogCoord = ComputeFogFactor(posInputs.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                // Clip plane discard
                if (_ClipEnabled > 0.5)
                {
                    float3 planeToFrag = input.positionWS - _ClipPlanePos.xyz;
                    float dist = dot(planeToFrag, _ClipPlaneNormal.xyz);
                    if (dist > 0.0)
                        discard;
                }

                // Simple lit shading
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float3 normalWS = normalize(input.normalWS);

                // Main light
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = baseColor.rgb * mainLight.color * NdotL;
                half3 ambient = baseColor.rgb * half3(0.3, 0.3, 0.35);

                half3 finalColor = diffuse + ambient;
                finalColor = MixFog(finalColor, input.fogCoord);

                return half4(finalColor, baseColor.a);
            }
            ENDHLSL
        }

        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float4 _SpecColor;
                float _Smoothness;
                float4 _ClipPlanePos;
                float4 _ClipPlaneNormal;
                float _ClipEnabled;
            CBUFFER_END

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                if (_ClipEnabled > 0.5)
                {
                    float3 planeToFrag = input.positionWS - _ClipPlanePos.xyz;
                    float dist = dot(planeToFrag, _ClipPlaneNormal.xyz);
                    if (dist > 0.0)
                        discard;
                }
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Simple Lit"
}
