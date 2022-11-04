using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
            Register(cinfo);
        }

        static Supported DummySupported() => Supported.Supported;

        class ARReplayProvider : Provider
        {
            #region Keywords
            static readonly int _HumanStencil = Shader.PropertyToID("_HumanStencil");
            static readonly int _HumanDepth = Shader.PropertyToID("_HumanDepth");
            static readonly int _EnvironmentDepth = Shader.PropertyToID("_EnvironmentDepth");
            static readonly int _EnvironmentDepthConfidence = Shader.PropertyToID("_EnvironmentDepthConfidence");


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

            public ARReplayProvider()
            {
            }

            public override void Start()
            {
                Debug.Log("Start ARReplayOcclusionSubsystem");
            }

            public override void Stop()
            {
                Debug.Log("Stop ARReplayOcclusionSubsystem");
            }

            public override void Destroy()
            {
                Debug.Log("Destroy ARReplayOcclusionSubsystem");
            }

            public override HumanSegmentationStencilMode requestedHumanStencilMode { get; set; }
            public override HumanSegmentationStencilMode currentHumanStencilMode => requestedHumanStencilMode;
            public override HumanSegmentationDepthMode requestedHumanDepthMode { get; set; }
            public override HumanSegmentationDepthMode currentHumanDepthMode => requestedHumanDepthMode;

            public override EnvironmentDepthMode requestedEnvironmentDepthMode { get; set; }
            public override EnvironmentDepthMode currentEnvironmentDepthMode => requestedEnvironmentDepthMode;
            public override bool environmentDepthTemporalSmoothingRequested { get; set; }
            public override bool environmentDepthTemporalSmoothingEnabled => environmentDepthTemporalSmoothingRequested;

            public override OcclusionPreferenceMode requestedOcclusionPreferenceMode { get; set; }
            public override OcclusionPreferenceMode currentOcclusionPreferenceMode => requestedOcclusionPreferenceMode;

            public override bool TryGetHumanStencil(out XRTextureDescriptor humanStencilDescriptor)
            {
                // TODO
                humanStencilDescriptor = default;
                return false;
            }

            public override bool TryGetHumanDepth(out XRTextureDescriptor humanDepthDescriptor)
            {
                // TODO
                humanDepthDescriptor = default;
                return false;
            }

            public override bool TryGetEnvironmentDepth(out XRTextureDescriptor environmentDepthDescriptor)
            {
                // TODO
                environmentDepthDescriptor = default;
                return false;
            }

            public override bool TryGetEnvironmentDepthConfidence(out XRTextureDescriptor environmentDepthConfidenceDescriptor)
            {
                // Not implemented yet
                environmentDepthConfidenceDescriptor = default;
                return false;
            }

            public NativeArray<XRTextureDescriptor> GetTextureDescriptors(Allocator allocator)
            {
                var descriptors = new List<XRTextureDescriptor>();

                // TODO
                return new NativeArray<XRTextureDescriptor>(0, allocator);
            }



            public override void GetMaterialKeywords(out List<string> enabledKeywords, out List<string> disabledKeywords)
            {
                bool isEnvDepthEnabled = currentEnvironmentDepthMode != EnvironmentDepthMode.Disabled;
                bool isHumanDepthEnabled = currentHumanDepthMode != HumanSegmentationDepthMode.Disabled;

                if ((currentOcclusionPreferenceMode == OcclusionPreferenceMode.NoOcclusion) || (!isEnvDepthEnabled && !isHumanDepthEnabled))
                {
                    enabledKeywords = null;
                    disabledKeywords = m_AllDisabledMaterialKeywords;
                }
                else if (isEnvDepthEnabled && (!isHumanDepthEnabled || (currentOcclusionPreferenceMode == OcclusionPreferenceMode.PreferEnvironmentOcclusion)))
                {
                    enabledKeywords = m_EnvironmentDepthEnabledMaterialKeywords;
                    disabledKeywords = m_HumanEnabledMaterialKeywords;
                }
                else
                {
                    enabledKeywords = m_HumanEnabledMaterialKeywords;
                    disabledKeywords = m_EnvironmentDepthEnabledMaterialKeywords;
                }
            }

        }
    }
}
