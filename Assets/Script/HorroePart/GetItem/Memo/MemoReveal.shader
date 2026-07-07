Shader "HorrorGame/UI/MemoReveal"
{
    Properties
    {
        _MainTex    ("Texture",    2D)    = "white" {}
        // 円形マスクの半径（0=完全非表示, 1=全体表示）
        _Radius     ("Radius",     Range(0, 1.5)) = 0
        // 境界のグラデーション幅（大きいほどぼんやり）
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.5)) = 0.15
        // 周辺の暗さ（ビネット強度）
        _VignetteStrength ("Vignette Strength", Range(0, 1)) = 0.85
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline"    = "UniversalPipeline"
        }

        Stencil
        {
            Ref   [_Stencil]
            Comp  [_StencilComp]
            Pass  [_StencilOp]
            ReadMask  [_StencilReadMask]
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
            Name "MemoReveal"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float  _Radius;
                float  _EdgeSoftness;
                float  _VignetteStrength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // UV中心からの距離
                float2 centered = IN.uv - 0.5;
                float dist = length(centered);

                // 境界をsmoothstepでグラデーション化して円形マスクを作る
                float mask = smoothstep(_Radius, _Radius - _EdgeSoftness, dist);

                // マスク内でも端は少し暗くなるビネット
                // _VignetteStrengthが高いほど周辺が暗くなる
                float vignette = 1.0 - saturate(dist * _VignetteStrength * 2.0);
                vignette = lerp(1.0, vignette, _VignetteStrength);

                half4 result = texColor * IN.color;
                result.rgb *= vignette;
                result.a   *= mask;

                return result;
            }
            ENDHLSL
        }
    }
}