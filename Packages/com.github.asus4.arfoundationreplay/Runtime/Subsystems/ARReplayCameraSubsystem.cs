using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARSubsystems;

namespace ARFoundationReplay
{
    /// <summary>
    /// The camera subsystem which decodes recorded video into ARKit textures (Y + CbCr).
    /// TODO: Support only Apple ARKit for now. ARCore is planned.
    /// </summary>
    [Preserve]
    internal sealed class ARReplayCameraSubsystem : XRCameraSubsystem
    {
        public const string ID = "ARReplay-Camera";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterDescriptor()
        {
            var cameraSubsystemCinfo = new XRCameraSubsystemCinfo
            {
                id = ID,
                providerType = typeof(ARReplayProvider),
                subsystemTypeOverride = typeof(ARReplayCameraSubsystem),
                supportsAverageBrightness = false,
                supportsAverageColorTemperature = true,
                supportsColorCorrection = false,
                supportsDisplayMatrix = true,
                supportsProjectionMatrix = true,
                supportsTimestamp = true,
                supportsCameraConfigurations = true,
                supportsCameraImage = true,
                supportsAverageIntensityInLumens = true,
                supportsFocusModes = true,
                supportsFaceTrackingAmbientIntensityLightEstimation = true,
                supportsFaceTrackingHDRLightEstimation = true,
                supportsWorldTrackingAmbientIntensityLightEstimation = true,
                supportsWorldTrackingHDRLightEstimation = false,
                supportsCameraGrain = false,
            };

            if (!Register(cameraSubsystemCinfo))
            {
                Debug.LogErrorFormat("Cannot register the {0} subsystem", ID);
            }
            else
            {
                Debug.Log($"Register {ID} subsystem");
            }
        }

        class ARReplayProvider : Provider
        {
            static readonly int k_InputTexture = Shader.PropertyToID("_InputTexture");
            static readonly int k_TextureY = Shader.PropertyToID("_textureY");
            static readonly int k_TextureCbCr = Shader.PropertyToID("_textureCbCr");
            static readonly int k_TextureSize = Shader.PropertyToID("_TextureSize");
            static readonly List<string> k_URPEnabledMaterialKeywords = new() { "ARKIT_BACKGROUND_URP" };

            Material _beforeOpaquesCameraMaterial;
            Material _afterOpaquesCameraMaterial;
            int _kernel;
            ComputeShader _computeShader;
            RenderTexture _yTexture;
            RenderTexture _cbCrTexture;


            public override Material cameraMaterial
            {
                get
                {
                    return currentBackgroundRenderingMode switch
                    {
                        XRCameraBackgroundRenderingMode.BeforeOpaques
                            => _beforeOpaquesCameraMaterial ??= CreateCameraMaterial("Unlit/ARKitBackground"),
                        XRCameraBackgroundRenderingMode.AfterOpaques
                            => _afterOpaquesCameraMaterial ??= CreateCameraMaterial("Unlit/ARKitBackground/AfterOpaques"),
                        _ => null,
                    };
                }
            }

            public override bool permissionGranted => true;

            public override Feature currentCamera => Feature.AnyCamera;
            public override Feature requestedCamera
            {
                get => Feature.AnyCamera;
                set
                {
                    // Debug.Log($"requestedCamera: {value}")
                }
            }

            public override XRCameraBackgroundRenderingMode currentBackgroundRenderingMode
                => requestedBackgroundRenderingMode switch
                {
                    XRSupportedCameraBackgroundRenderingMode.Any => XRCameraBackgroundRenderingMode.AfterOpaques,
                    // Don't support BeforeOpaques at the moment
                    XRSupportedCameraBackgroundRenderingMode.BeforeOpaques => XRCameraBackgroundRenderingMode.BeforeOpaques,
                    XRSupportedCameraBackgroundRenderingMode.AfterOpaques => XRCameraBackgroundRenderingMode.AfterOpaques,
                    XRSupportedCameraBackgroundRenderingMode.None => XRCameraBackgroundRenderingMode.None,
                    _ => throw new ArgumentOutOfRangeException(),
                };

            public override XRSupportedCameraBackgroundRenderingMode requestedBackgroundRenderingMode { get; set; }
                = XRSupportedCameraBackgroundRenderingMode.Any;

            public override XRSupportedCameraBackgroundRenderingMode supportedBackgroundRenderingMode
                => XRSupportedCameraBackgroundRenderingMode.Any;

            public override void Start()
            {
                _computeShader = Resources.Load<ComputeShader>("Shaders/ARKitDecoder");
                Assert.IsNotNull(_computeShader);
                _kernel = _computeShader.FindKernel("DecodeYCbCr");

                Vector2Int size = Config.RecordResolution;
                _computeShader.SetInts(k_TextureSize, size.x, size.y);
                _yTexture = TextureUtils.CreateRWTexture2D(size, RenderTextureFormat.R8);
                _cbCrTexture = TextureUtils.CreateRWTexture2D(size, RenderTextureFormat.RG16);
            }

            public override void Destroy()
            {
                DisposeUtil.Dispose(_beforeOpaquesCameraMaterial);
                DisposeUtil.Dispose(_afterOpaquesCameraMaterial);
                DisposeUtil.Dispose(_yTexture);
                DisposeUtil.Dispose(_cbCrTexture);
            }

            public override bool TryGetFrame(XRCameraParams cameraParams, out XRCameraFrame cameraFrame)
            {
                if (!ARReplay.TryGetReplay(out var replay))
                {
                    cameraFrame = default;
                    return false;
                }

                const XRCameraFrameProperties properties =
                    XRCameraFrameProperties.Timestamp
                    | XRCameraFrameProperties.ProjectionMatrix
                    | XRCameraFrameProperties.DisplayMatrix;

                var received = replay.Packet.cameraFrame;

                // Skip if timestamp is not set
                if (received.timestampNs == 0)
                {
                    cameraFrame = default;
                    return false;
                }

                cameraFrame = new XRCameraFrame(
                    timestamp: received.timestampNs,
                    averageBrightness: 0,
                    averageColorTemperature: 0,
                    colorCorrection: default,
                    projectionMatrix: received.projectionMatrix,
                    displayMatrix: received.displayMatrix,
                    trackingState: TrackingState.Tracking,
                    nativePtr: IntPtr.Zero,
                    properties: properties,
                    averageIntensityInLumens: 0,
                    exposureDuration: 0,
                    exposureOffset: 0,
                    mainLightIntensityInLumens: 0,
                    mainLightColor: default,
                    mainLightDirection: default,
                    ambientSphericalHarmonics: default,
                    cameraGrain: default,
                    noiseIntensity: 0
                );

                return true;
            }

            public override bool autoFocusEnabled => true;

            public override bool autoFocusRequested
            {
                get => true;
                set
                {
                    Debug.Log($"autoFocusRequested: {value}");
                }
            }

            public override Feature currentLightEstimation => Feature.AnyLightEstimation;
            public override Feature requestedLightEstimation
            {
                get => Feature.AnyLightEstimation;
                set
                {
                    Debug.Log($"requestedLightEstimation: {value}");
                }
            }

            public override NativeArray<XRTextureDescriptor> GetTextureDescriptors(XRTextureDescriptor defaultDescriptor, Allocator allocator)
            {
                if (!ARReplay.TryGetReplay(out var replay))
                {
                    return new NativeArray<XRTextureDescriptor>(0, allocator);
                }

                // Decode Y + CbCr texture from video
                Vector2Int size = Config.RecordResolution;
                _computeShader.SetTexture(_kernel, k_InputTexture, replay.Texture);
                _computeShader.SetTexture(_kernel, k_TextureY, _yTexture);
                _computeShader.SetTexture(_kernel, k_TextureCbCr, _cbCrTexture);
                _computeShader.Dispatch(_kernel, size.x / 8, size.y / 8, 1);

                var arr = new NativeArray<XRTextureDescriptor>(2, allocator);
                arr[0] = _yTexture.ToTextureDescriptor(k_TextureY);
                arr[1] = _cbCrTexture.ToTextureDescriptor(k_TextureCbCr);
                return arr;
            }

            public override void GetMaterialKeywords(out List<string> enabledKeywords, out List<string> disabledKeywords)
            {
                // Only supports URP for now
                if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)
                {
                    enabledKeywords = k_URPEnabledMaterialKeywords;
                    disabledKeywords = null;
                }
                else
                {
                    Debug.LogWarning($"Unsupported render pipeline: {GraphicsSettings.currentRenderPipeline}");
                    enabledKeywords = null;
                    disabledKeywords = null;
                }
            }

            public override void OnBeforeBackgroundRender(int id)
            {
                // Do nothing
                // Debug.Log($"OnBeforeBackgroundRender: {id}");
            }
        }
    }
}
