Shader "UI/URP/GaussianBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlurAmount ("Blur Amount", Range(0, 20)) = 5.0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
            Name "UIURPBlur"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            float4 _Color;
            float _BlurAmount;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 texel = _MainTex_TexelSize.xy * (_BlurAmount * 0.5);

                half4 col = float4(0, 0, 0, 0);

                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-texel.x, -texel.y)) * 0.077847;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0.0,     -texel.y)) * 0.123317;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texel.x,  -texel.y)) * 0.077847;

                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-texel.x,  0.0))     * 0.123317;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0.0,      0.0))     * 0.195346;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texel.x,   0.0))     * 0.123317;

                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-texel.x,  texel.y)) * 0.077847;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0.0,      texel.y)) * 0.123317;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texel.x,   texel.y)) * 0.077847;

                return col * input.color;
            }
            ENDHLSL
        }
    }
}