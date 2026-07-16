Shader "HorrorGame/Enemy/GhostRim"
{
    Properties
    {
        _BaseMap        ("Base Map",   2D)    = "white" {}
        _BaseColor      ("Base Color", Color) = (1, 1, 1, 1)
        // 本体の明るさ。暗所でも一定で見えるが、上げすぎると闇に潜む怖さが消える
        _BodyBrightness ("Body Brightness", Range(0, 1)) = 0.35
        [HDR] _RimColor ("Rim Color",     Color)       = (0.55, 0.75, 1, 1)
        // 大きいほど縁が細く鋭くなる
        _RimPower       ("Rim Power",     Range(0.5, 8)) = 3
        _RimIntensity   ("Rim Intensity", Range(0, 8))   = 2
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline"  = "UniversalPipeline"
        }

        Pass
        {
            Name "GhostRim"
            Tags { "LightMode" = "UniversalForward" }

            Cull   [_Cull]
            ZWrite Off
            Blend  SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _RimColor;
                float  _BodyBrightness;
                float  _RimPower;
                float  _RimIntensity;
                float  _Cull;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);

                OUT.positionHCS = positionInputs.positionCS;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS   = GetWorldSpaceViewDir(positionInputs.positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                float3 normalWS  = normalize(IN.normalWS);
                float3 viewDirWS = normalize(IN.viewDirWS);

                // 視線と法線が直交するシルエット縁ほど 1 に近づく（フレネル）
                float fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                float rim     = pow(fresnel, _RimPower) * _RimIntensity;

                // シーンのライトを一切参照しないため、環境光が暗くても視認性が落ちない
                half3 color = texColor.rgb * _BodyBrightness + _RimColor.rgb * rim;

                // 半透明の体でも輪郭だけは不透明側へ寄せ、闇に溶けるのを防ぐ
                half alpha = saturate(texColor.a + rim);

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
