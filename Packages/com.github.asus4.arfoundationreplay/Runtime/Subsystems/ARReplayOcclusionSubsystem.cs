using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;

namespace ARFoundationReplay
{
    /// <summary>
    /// The occlusion subsystem which decode depth and human segmentation textures from a replay video
    /// </summary>
    [Preserve]
    internal sealed class ARReplayOcclusionSubsystem : XROcclusionSubsystem
    {
        public const string ID = "ARReplay-Occlusion";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            var cinfo = new XROcclusionSubsystemCinfo()
            {
                id = ID,
                providerType = typeof(ARReplayProvider),
                subsystemTypeOverride = typeof(ARReplayOcclusionSubsystem),
                humanSegmentationStencilImageSupportedDelegate = DummySupported,
                humanSegmentationDepthImageSupportedDelegate = DummySupported,
                environmentDepthImageSupportedDelegate = DummySupported,
                environmentDepthConfidenceImageSupportedDelegate = DummySupported,
                environmentDepthTemporalSmoothingSupportedDelegate = DummySupported,
            };
            if (Register(cinfo))
            {
                Debug.LogFormat("Registered the {0} subsystem", ID);
            }
            else
            {
                Debug.LogErrorFormat("Cannot register the {0} subsystem", ID);
            }
        }

        // TODO: Encode supported info into the packet?
        static Supported DummySupported() => Supported.Supported;

        class ARReplayProvider : Provider
        {
            #region Keywords
            static readonly int k_InputTexture = Shader.PropertyToID("_InputTexture");
            static readonly int k_TextureSize = Shader.PropertyToID("_TextureSize");
            static readonly int k_HumanStencil = Shader.PropertyToID("_HumanStencil");
            static readonly int k_HumanDepth = Shader.PropertyToID("_HumanDepth");
            static readonly int k_EnvironmentDepth = Shader.PropertyToID("_EnvironmentDepth");
            static readonly int k_DepthRange = Shader.PropertyToID("_DepthRange");
            // Not used
            // static readonly int k_EnvironmentDepthConfidence = Shader.PropertyToID("_EnvironmentDepthConfidence");


            const string k_HumanEnabledMaterialKeyword = "ARKIT_HUMAN_SEGMENTATION_ENABLED";
            const string k_EnvironmentDepthEnabledMaterialKeyword = "ARKIT_ENVIRONMENT_DEPTH_ENABLED";
            static readonly List<string> m_AllDisabledMaterialKeywords = new()
            {
                k_HumanEnabledMaterialKeyword,
                k_EnvironmentDepthEnabledMaterialKeyword,
            };
            static readonly List<string> m_HumanEnabledMaterialKeywords = new()
            {
                k_HumanEnabledMaterialKeyword,
            };
            static readonly List<string> m_EnvironmentDepthEnabledMaterialKeywords = new()
            {
                k_EnvironmentDepthEnabledMaterialKeyword,
            };
            #endregion // Keywords

            ComputeShader _computeShader;
            int _kernel;
            RenderTexture _humanStencilTexture;
            RenderTexture _humanDepthTexture;
            RenderTexture _environmentDepthTexture;
            Vector2 _depthRange;

            public ARReplayProvider()
            {
            }

            public override void Start()
            {
                Debug.Log("Start ARReplayOcclusionSubsystem");

                _computeShader = Resources.Load<ComputeShader>("Shaders/ARKitDecoder");
                Assert.IsNotNull(_computeShader);
                _kernel = _computeShader.FindKernel("DecodeOcclusion");
                Vector2Int size = Config.RecordResolution;
                _computeShader.SetInts(k_TextureSize, size.x, size.y);
                _humanStencilTexture = TextureUtils.CreateRWTexture2D(size, RenderTextureFormat.R8);
                _humanDepthTexture = TextureUtils.CreateRWTexture2D(size, RenderTextureFormat.RFloat);
                _environmentDepthTexture = TextureUtils.CreateRWTexture2D(size, RenderTextureFormat.RFloat);
            }

            public override void Stop() { }

            public override void Destroy()
            {
                DisposeUtil.Dispose(_humanStencilTexture);
                DisposeUtil.Dispose(_humanDepthTexture);
                DisposeUtil.Dispose(_environmentDepthTexture);
            }

            public override HumanSegmentationStencilMode requestedHumanStencilMode { get; set; }
            public override HumanSegmentationStencilMode currentHumanStencilMode => requestedHumanStencilMode;
            public override HumanSegmentationDepthMode requestedHumanDepthMode { get; set; }
            public override HumanSegmentationDepthMode currentHumanDepthMode => requestedHumanDepthMode;

            public override EnvironmentDepthMode requestedEnvironmentDepthMode { get; set; }
            public override EnvironmentDepthMode currentEnvironmentDepthMode => requestedEnvironmentDepthMode;
            public override bool environmentDepthTemporalSmoothingRequested { get; set; }
            public override bool environmentDepthTemporalSmoothingEnabled => environmentDepthTemporalSmoothingRequested;

            public override OcclusionPreferenceMode requestedOcclusionPreferenceMode { get; set; } = OcclusionPreferenceMode.PreferEnvironmentOcclusion;
            public override OcclusionPreferenceMode currentOcclusionPreferenceMode => requestedOcclusionPreferenceMode;

            public override bool TryGetHumanStencil(out XRTextureDescriptor humanStencilDescriptor)
            {
                if (currentHumanStencilMode == HumanSegmentationStencilMode.Disabled)
                {
                    humanStencilDescriptor = default;
                    return false;
                }
                else
                {
                    humanStencilDescriptor = _humanStencilTexture.ToTextureDescriptor(k_HumanStencil);
                    return true;
                }
            }

            public override bool TryGetHumanDepth(out XRTextureDescriptor humanDepthDescriptor)
            {
                if (currentHumanDepthMode == HumanSegmentationDepthMode.Disabled)
                {
                    humanDepthDescriptor = default;
                    return false;
                }
                else
                {
                    humanDepthDescriptor = _humanDepthTexture.ToTextureDescriptor(k_HumanDepth);
                    return true;
                }
            }

            public override bool TryGetEnvironmentDepth(out XRTextureDescriptor environmentDepthDescriptor)
            {
                if (currentEnvironmentDepthMode == EnvironmentDepthMode.Disabled)
                {
                    environmentDepthDescriptor = default;
                    return false;
                }
                else
                {
                    environmentDepthDescriptor = _environmentDepthTexture.ToTextureDescriptor(k_EnvironmentDepth);
                    return true;
                }
            }

            public override bool TryGetEnvironmentDepthConfidence(out XRTextureDescriptor environmentDepthConfidenceDescriptor)
            {
                // Not implemented yet
                Debug.Log("TryGetEnvironmentDepthConfidence");
                environmentDepthConfidenceDescriptor = default;
                return false;
            }

            private static readonly List<XRTextureDescriptor> _descriptors = new();
            public override NativeArray<XRTextureDescriptor> GetTextureDescriptors(XRTextureDescriptor defaultDescriptor, Allocator allocator)
            {
                if (!ARReplay.TryGetReplay(out var replay))
                {
                    return new NativeArray<XRTextureDescriptor>(0, allocator);
                }

                // Decode the occlusion textures from video
                Vector2Int size = Config.RecordResolution;
                _computeShader.SetTexture(_kernel, k_InputTexture, replay.Texture);
                _computeShader.SetTexture(_kernel, k_HumanStencil, _humanStencilTexture);
                _computeShader.SetTexture(_kernel, k_HumanDepth, _humanDepthTexture);
                _computeShader.SetTexture(_kernel, k_EnvironmentDepth, _environmentDepthTexture);
                _computeShader.SetFloats(k_DepthRange, _depthRange.x, _depthRange.y);
                _computeShader.Dispatch(_kernel, size.x / 8, size.y / 8, 1);

                _descriptors.Clear();
                if (TryGetHumanStencil(out var humanStencilDescriptor))
                {
                    _descriptors.Add(humanStencilDescriptor);
                }
                if (TryGetHumanDepth(out var humanDepthDescriptor))
                {
                    _descriptors.Add(humanDepthDescriptor);
                }
                if (TryGetEnvironmentDepth(out var environmentDepthDescriptor))
                {
                    _descriptors.Add(environmentDepthDescriptor);
                }
                return new NativeArray<XRTextureDescriptor>(_descriptors.ToArray(), allocator);
            }


            public override void GetMaterialKeywords(out List<string> enabledKeywords, out List<string> disabledKeywords)
            {
                bool isEnvDepthEnabled = currentEnvironmentDepthMode != EnvironmentDepthMode.Disabled;
                bool isHumanDepthEnabled = currentHumanDepthMode != HumanSegmentationDepthMode.Disabled;

                if ((currentOcclusionPreferenceMode == OcclusionPreferenceMode.NoOcclusion) || (!isEnvDepthEnabled && !isHumanDepthEnabled))
                {
                    // No occlusion
                    enabledKeywords = null;
                    disabledKeywords = m_AllDisabledMaterialKeywords;
                }
                else if (isEnvDepthEnabled && (!isHumanDepthEnabled || (currentOcclusionPreferenceMode == OcclusionPreferenceMode.PreferEnvironmentOcclusion)))
                {
                    // Environment depth only
                    enabledKeywords = m_EnvironmentDepthEnabledMaterialKeywords;
                    disabledKeywords = m_HumanEnabledMaterialKeywords;
                }
                else
                {
                    // Human depth only
                    enabledKeywords = m_HumanEnabledMaterialKeywords;
                    disabledKeywords = m_EnvironmentDepthEnabledMaterialKeywords;
                }
            }
        }
    }
}
