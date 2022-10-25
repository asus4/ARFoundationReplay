Shader "Unlit/WebcamBackground"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            // ZTest GEqual
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct appdata
            {
                float3 position : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                float2 depthTexCoord : TEXCOORD1;
            };

            struct fragment_output
            {
                real4 color : SV_Target;
                float depth : SV_Depth;
            };

            CBUFFER_START(UnityARFoundationPerFrame)
                float4x4 _UnityDisplayTransform;
                float _UnityCameraForwardScale;
            CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
                o.position = TransformObjectToHClip(v.position);

                float2 texcoord = mul(float3(v.texcoord, 1.0f), _UnityDisplayTransform).xy;
                o.texcoord = texcoord + float2(_UnityDisplayTransform[0].w, _UnityDisplayTransform[1].w);
                o.depthTexCoord = v.texcoord;
                return o;
            }

            TEXTURE2D(_MainTex) ;
            SAMPLER(sampler_MainTex);

            fragment_output frag (v2f i)
            {
                // sample the texture
                real4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);

                float cameraDepthValue = SampleSceneDepth(i.depthTexCoord);
                if (cameraDepthValue >= depthValue)
                {
                    // discard;
                }

                fragment_output o;
                o.color = color;
                o.depth = depthValue;
                return o;
            }
            ENDHLSL
        }
    }
}
