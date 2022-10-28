Shader "Hidden/ARFoundationReplay/ARKitEncoder"
{
    Properties
    {
        _textureY ("TextureY", 2D) = "white" {}
        _textureCbCr ("TextureCbCr", 2D) = "black" {}
        _HumanStencil ("HumanStencil", 2D) = "black" {}
        _HumanDepth ("HumanDepth", 2D) = "black" {}
        _EnvironmentDepth ("EnvironmentDepth", 2D) = "black" {}
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "ForceNoShadowCasting" = "True"
        }

        Pass
        {
            Cull Off
            ZTest Always
            ZWrite On
            Lighting Off
            LOD 100
            Tags
            {
                "LightMode" = "Always"
            }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.github.asus4.arfoundationreplay/Resources/Shaders/Common.hlsl"

            struct appdata
            {
                float3 position : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct fragment_output
            {
                real4 color : SV_Target;
                float depth : SV_Depth;
            };


            CBUFFER_START(UnityARFoundationPerFrame)
            // Device display transform is provided by the AR Foundation camera background renderer.
            float4x4 _UnityDisplayTransform;
            float _UnityCameraForwardScale;
            CBUFFER_END


            v2f vert (appdata v)
            {
                v2f o;
                o.position = TransformObjectToHClip(v.position);;
                o.texcoord = v.texcoord;
                return o;
            }

            inline float ConvertDistanceToDepth(float d)
            {
                // Account for scale
                d = _UnityCameraForwardScale > 0.0 ? _UnityCameraForwardScale * d : d;

                // Clip any distances smaller than the near clip plane, and compute the depth value from the distance.
                return (d < _ProjectionParams.y) ? 0.0f : ((1.0f / _ZBufferParams.z) * ((1.0f / d) - _ZBufferParams.w));
            }

            TEXTURE2D(_textureY);
            SAMPLER(sampler_textureY);
            TEXTURE2D(_textureCbCr);
            SAMPLER(sampler_textureCbCr);
            TEXTURE2D(_EnvironmentDepth);
            SAMPLER(sampler_EnvironmentDepth);
            TEXTURE2D(_HumanStencil);
            SAMPLER(sampler_HumanStencil);
            TEXTURE2D(_HumanDepth);
            SAMPLER(sampler_HumanDepth);

            float4 frag (v2f i): SV_Target
            {
                // Sample in YCbCr then convert to sRGB.
                float2 uv_c = UV_FullToColor(i.texcoord);
                float3 c = YCbCrToSRGB(
                    SAMPLE_TEXTURE2D(_textureY, sampler_textureY, uv_c).x,
                    SAMPLE_TEXTURE2D(_textureCbCr, sampler_textureCbCr, uv_c).xy
                );
                // Hue-encoded depth
                float depth = SAMPLE_TEXTURE2D(_EnvironmentDepth, sampler_EnvironmentDepth, UV_FullToDepth(i.texcoord)).x;
                float2 _DepthRange = float2(0.5, 10);
                float3 z = EncodeDepth(depth, _DepthRange);

                // Human stencil
                float s = SAMPLE_TEXTURE2D(_HumanStencil, sampler_HumanStencil, UV_FullToStencil(i.texcoord)).x;

                // Multiplexing
                float3 rgb = BibcamMux(i.texcoord, 0.0, c, z, s);

                // Linear color support
                #ifndef UNITY_NO_LINEAR_COLORSPACE
                rgb = FastSRGBToLinear(rgb);
                #endif

                // Output
                return float4(rgb, 1);
            }

            ENDHLSL
        }
    }
}
