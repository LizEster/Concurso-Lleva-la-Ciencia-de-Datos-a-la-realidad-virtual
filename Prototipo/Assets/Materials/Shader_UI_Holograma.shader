Shader "Custom/UI_PanelHolograma"
{
    // Pensado para ponerlo en el Material de una Image de un Canvas World Space
    // (el fondo del panel de preguntas). Basado en la misma idea de tu
    // Shader_Agua_Holograma (fresnel + ruido) pero adaptado a un quad de UI:
    // borde brillante + línea de escaneo que sube + parpadeo sutil.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tinte / transparencia base", Color) = (0.3, 0.85, 1.0, 0.35)
        _ColorBorde ("Color del borde y del scan", Color) = (0.4, 0.9, 1.0, 1.0)
        _AnchoBorde ("Ancho del borde brillante", Range(0.01, 0.3)) = 0.08
        _VelocidadScan ("Velocidad de la línea de escaneo", Range(0, 5)) = 1.2
        _GrosorScan ("Grosor de la línea de escaneo", Range(0.01, 0.3)) = 0.05
        _IntensidadParpadeo ("Intensidad del parpadeo", Range(0, 0.5)) = 0.08

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
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
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
            Name "Default"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _ColorBorde;
                float _AnchoBorde;
                float _VelocidadScan;
                float _GrosorScan;
                float _IntensidadParpadeo;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half4 color = tex * i.color;

                // Distancia de este píxel al borde más cercano del panel (en UV, 0..1)
                float distBorde = min(min(i.uv.x, 1.0 - i.uv.x), min(i.uv.y, 1.0 - i.uv.y));
                float brilloBorde = 1.0 - saturate(distBorde / _AnchoBorde);
                color.rgb += _ColorBorde.rgb * brilloBorde * _ColorBorde.a;

                // Línea de escaneo que sube todo el rato (típico "scan" de holograma)
                float scan = frac(i.uv.y - _Time.y * _VelocidadScan);
                float lineaScan = smoothstep(_GrosorScan, 0.0, abs(scan - 0.5));
                color.rgb += _ColorBorde.rgb * lineaScan * 0.6;

                // Parpadeo sutil general, para que se sienta inestable como un holograma real
                float parpadeo = 1.0 - _IntensidadParpadeo * (0.5 + 0.5 * sin(_Time.y * 17.0));
                color.rgb *= parpadeo;

                return color;
            }
            ENDHLSL
        }
    }
}
