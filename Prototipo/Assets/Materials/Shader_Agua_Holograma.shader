Shader "Custom/Shader_Agua_Holograma"
{
    Properties
    {
        _Color ("Color del agua", Color) = (0.3, 0.7, 1.0, 0.5)
        _EmissionColor ("Color de brillo", Color) = (0.3, 0.7, 1.0, 1.0)
        _EmissionIntensity ("Intensidad del brillo", Range(0, 3)) = 1.0
        _NoiseScale ("Escala del patron", Range(1, 30)) = 10.0
        _Speed ("Velocidad del movimiento", Range(0, 3)) = 0.5
        _Distortion ("Distorsion", Range(0, 0.3)) = 0.1
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _EmissionColor;
                float _EmissionIntensity;
                float _NoiseScale;
                float _Speed;
                float _Distortion;
            CBUFFER_END

            // Funcion de ruido simple
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Ruido con varias capas para mas detalle
            float waterNoise(float2 uv, float time)
            {
                float n = 0.0;
                n += 0.5 * noise(uv * _NoiseScale + time * _Speed);
                n += 0.25 * noise(uv * _NoiseScale * 2.0 - time * _Speed * 1.3);
                n += 0.125 * noise(uv * _NoiseScale * 4.0 + time * _Speed * 0.7);
                return n;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Distorsionar los vertices para dar movimiento a la superficie
                float time = _Time.y;
                float n = waterNoise(input.uv, time);
                float3 displaced = input.positionOS.xyz + input.normalOS * n * _Distortion;
                
                output.positionCS = TransformObjectToHClip(displaced);
                output.uv = input.uv;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(TransformObjectToWorld(input.positionOS.xyz));
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y;
                
                // Generar patron de agua
                float n = waterNoise(input.uv, time);
                
                // Efecto Fresnel para brillo en los bordes (tipico de hologramas)
                float fresnel = pow(1.0 - saturate(dot(input.normalWS, input.viewDirWS)), 2.0);
                
                // Color base con variacion del ruido
                half4 color = _Color;
                color.rgb *= (0.8 + 0.4 * n);
                
                // Agregar brillo de emision y fresnel
                color.rgb += _EmissionColor.rgb * _EmissionIntensity * (0.5 + 0.5 * n);
                color.rgb += _EmissionColor.rgb * fresnel * 0.5;
                
                // Transparencia con variacion
                color.a = _Color.a * (0.7 + 0.3 * n);
                color.a = saturate(color.a + fresnel * 0.3);
                
                return color;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
