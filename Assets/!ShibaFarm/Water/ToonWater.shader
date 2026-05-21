Shader "Custom/ToonWater"
{
    Properties
    {
        //Depth Color
        _DepthGradientShallow("Depth Gradient Shallow", Color) = (0.325, 0.807, 0.971, 0.725)
        _DepthGradientDeep("Depth Gradient Deep", Color) = (0.086, 0.407, 1, 0.749)
        _DepthMaxDistance("Depth Maximum Distance", Float) = 1

        //Foam
        _FoamColor("Foam Color", Color) = (1, 1, 1, 1)
        _FoamMaxDistance("Foam Maximum Distance", Float) = 0.4
        _FoamMinDistance("Foam Minimum Distance", Float) = 0.04

        //Surface Noise and Wave Pattern
        _SurfaceNoise("Surface Noise", 2D) = "white" {}
        _SurfaceNoiseCutoff("Surface Noise Cutoff", Range(0, 1)) = 0.777
        _SurfaceNoiseScroll("Surface Noise Scroll", Vector) = (0.03, 0.03, 0, 0)

        //Distortion
        _SurfaceDistortion("Surface Distortion", 2D) = "white" {}
        _SurfaceDistortionAmount("Surface Distortion Amount", Range(0, 1)) = 0.27

        //Vertex Waves Configuration
        _WaveSpeed("Wave Speed", Range(0, 5)) = 2.0
        _WaveAmount("Wave Amount", Range(0, 5)) = 0.5
        _WaveHeight("Wave Height", Range(0, 2)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS    : SV_POSITION;
                float4 screenPosition : TEXCOORD0;
                float2 noiseUV        : TEXCOORD1;
                float2 distortUV      : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _DepthGradientShallow;
                float4 _DepthGradientDeep;
                float  _DepthMaxDistance;

                float4 _FoamColor;
                float  _FoamMaxDistance;
                float  _FoamMinDistance;

                sampler2D _SurfaceNoise;
                float4    _SurfaceNoise_ST;
                float     _SurfaceNoiseCutoff;
                float2    _SurfaceNoiseScroll;

                sampler2D _SurfaceDistortion;
                float4    _SurfaceDistortion_ST;
                float     _SurfaceDistortionAmount;

                float _WaveSpeed;
                float _WaveAmount;
                float _WaveHeight;
            CBUFFER_END

            //Standard alpha blending function
            half4 alphaBlend(half4 top, half4 bottom)
            {
                half3 color = (top.rgb * top.a) + (bottom.rgb * (1 - top.a));
                half  alpha = top.a + bottom.a * (1 - top.a);
                return half4(color, alpha);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                //Calculate vertex wave displacement using sine wave and time
                float wave = sin(_Time.y * _WaveSpeed + (IN.positionOS.x * IN.positionOS.z * _WaveAmount)) * _WaveHeight;
                
                //Apply the displacement to the Y axis
                IN.positionOS.y += wave;

                OUT.positionHCS    = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.screenPosition = ComputeScreenPos(OUT.positionHCS);
                OUT.noiseUV        = TRANSFORM_TEX(IN.uv, _SurfaceNoise);
                OUT.distortUV      = TRANSFORM_TEX(IN.uv, _SurfaceDistortion);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                //Calculate depth difference between water surface and underwater objects
                float2 screenUV   = IN.screenPosition.xy / IN.screenPosition.w;
                float sceneDepth  = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float surfaceDepth = IN.screenPosition.w;
                float depthDiff   = sceneDepth - surfaceDepth;

                //Determine base water color using depth gradient
                float waterDepth01 = saturate(depthDiff / _DepthMaxDistance);
                half4 waterColor   = lerp(_DepthGradientShallow, _DepthGradientDeep, waterDepth01);

                //Calculate UV distortion by remapping texture from 0-1 to -1-1
                float2 distortSample = (tex2D(_SurfaceDistortion, IN.distortUV).xy * 2 - 1) * _SurfaceDistortionAmount;

                //Apply scrolling over time and add distortion to noise UVs
                float2 noiseUV = float2(
                    IN.noiseUV.x + _Time.y * _SurfaceNoiseScroll.x + distortSample.x,
                    IN.noiseUV.y + _Time.y * _SurfaceNoiseScroll.y + distortSample.y
                );
                float noiseSample = tex2D(_SurfaceNoise, noiseUV).r;

                //Calculate foam intensity based on depth to make it thicker in shallow areas
                float foamDepth01    = saturate(depthDiff / _FoamMaxDistance);
                float noiseCutoff    = foamDepth01 * _SurfaceNoiseCutoff;

                //Use smoothstep to soften foam edges and prevent pixelation
                #define SMOOTHSTEP_AA 0.01
                float surfaceNoise = smoothstep(
                    noiseCutoff - SMOOTHSTEP_AA,
                    noiseCutoff + SMOOTHSTEP_AA,
                    noiseSample
                );

                //Blend final foam color with the base water color
                half4 foamColor = _FoamColor;
                foamColor.a    *= surfaceNoise;

                return alphaBlend(foamColor, waterColor);
            }
            ENDHLSL
        }
    }
}