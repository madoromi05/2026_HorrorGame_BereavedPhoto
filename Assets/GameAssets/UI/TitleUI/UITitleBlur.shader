Shader "HorrorGame/UI/UITitleBlur"
{
    Properties
    {
        _MainTex  ("Texture",  2D)             = "white" {}
        // ぼかし強度。テクセル単位のサンプリング半径。0 で無加工（等倍サンプル）。
        _BlurSize ("Blur Size", Range(0, 16))  = 0
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
            Name "UITitleBlur"

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
                float4 _MainTex_TexelSize;
                float  _BlurSize;
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
                // 5x5 の 2 次元ガウス（1D 重みの外積）。単一パスで完結させるため
                // 分離せず 25 タップでサンプリングする。全画面 1 枚のみの用途なので許容。
                const float weights[5] = { 0.06136, 0.24477, 0.38774, 0.24477, 0.06136 };

                float2 step = _MainTex_TexelSize.xy * _BlurSize;

                half4 sum = half4(0, 0, 0, 0);
                [unroll]
                for (int y = 0; y < 5; y++)
                {
                    [unroll]
                    for (int x = 0; x < 5; x++)
                    {
                        float2 offset = float2(x - 2, y - 2) * step;
                        float  w      = weights[x] * weights[y];
                        sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + offset) * w;
                    }
                }

                return sum * IN.color;
            }
            ENDHLSL
        }
    }
}
