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
            var cameraSubsystemCinfo = new XRCameraSubsystemDescriptor.Cinfo
            {
                id = ID,
                providerType = typeof(ARReplayProvider),
                subsystemTypeOverride = typeof(ARReplayCameraSubsystem),
                supportsDisplayMatrix = true,
                supportsProjectionMatrix = true,
                supportsTimestamp = true,
                supportsCameraConfigurations = true,
                supportsCameraImage = false,
                // TODO: update more properties
            };

            XRCameraSubsystemDescriptor.Register(cameraSubsystemCinfo);
        }

        class ARReplayProvider : Provider
        {
            static readonly int k_InputTexture = Shader.PropertyToID("_InputTexture");
            static readonly int k_TextureY = Shader.PropertyToID("_textureY");
            static readonly int k_TextureCbCr = Shader.PropertyToID("_textureCbCr");
            static readonly int k_TextureSize = Shader.PropertyToID("_TextureSize");
            static readonly List<string> k_URPEnabledKeywordList = new() { "ARKIT_BACKGROUND_URP" };
            static readonly XRShaderKeywords k_URPShaderKeywords = new(new(k_URPEnabledKeywordList), null);

            Material _beforeOpaquesCameraMaterial;
            Material _afterOpaquesCameraMaterial;
            int _kernel;
            ComputeShader _computeShader;
            RenderTexture _yTexture;
            RenderTexture _cbCrTexture;

            XRCameraConfiguration _cameraConfiguration;
            XRCameraIntrinsics _cameraIntrinsics;

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

                _cameraConfiguration = new XRCameraConfiguration(IntPtr.Zero, size);
                _cameraIntrinsics = new XRCameraIntrinsics();
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

                XRCameraFrame receivedFrame = replay.Metadata.camera;

                // Skip if timestamp is not set
                if (!receivedFrame.TryGetTimestamp(out long timestamp) || timestamp == 0)
                {
                    cameraFrame = default;
                    return false;
                }

                cameraFrame = CopyOnlySafeFrame(receivedFrame);
                // cameraFrame = receivedFrame;
                return true;
            }

            public override bool TryGetIntrinsics(out XRCameraIntrinsics cameraIntrinsics)
            {
                cameraIntrinsics = _cameraIntrinsics;
                return true;
            }

            public override NativeArray<XRCameraConfiguration> GetConfigurations(XRCameraConfiguration defaultCameraConfiguration, Allocator allocator)
            {
                var configs = new NativeArray<XRCameraConfiguration>(1, allocator);
                configs[0] = _cameraConfiguration;
                return configs;
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
                    return base.GetTextureDescriptors(defaultDescriptor, allocator);
                }

                // Decode Y + CbCr texture from video
                var inputTex = replay.Texture;
                var size = new Vector2Int(inputTex.width, inputTex.height);
                _computeShader.SetInts(k_TextureSize, size.x, size.y);
                _computeShader.SetTexture(_kernel, k_InputTexture, inputTex);
                _computeShader.SetTexture(_kernel, k_TextureY, _yTexture);
                _computeShader.SetTexture(_kernel, k_TextureCbCr, _cbCrTexture);
                _computeShader.Dispatch(_kernel, size.x / 8, size.y / 8, 1);

                var arr = new NativeArray<XRTextureDescriptor>(2, allocator);
                arr[0] = _yTexture.ToTextureDescriptor(k_TextureY);
                arr[1] = _cbCrTexture.ToTextureDescriptor(k_TextureCbCr);
                return arr;
            }

            [Obsolete("GetShaderKeywords is deprecated as of AR Foundation 6.1. Use GetShaderKeywords2 instead.")]
            public override void GetMaterialKeywords(out List<string> enabledKeywords, out List<string> disabledKeywords)
            {
                // Only supports URP for now
                if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)
                {
                    enabledKeywords = k_URPEnabledKeywordList;
                    disabledKeywords = null;
                }
                else
                {
                    Debug.LogWarning($"Unsupported render pipeline: {GraphicsSettings.currentRenderPipeline}");
                    enabledKeywords = null;
                    disabledKeywords = null;
                }
            }

            public override XRShaderKeywords GetShaderKeywords2()
            {
                if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)
                {
                    return k_URPShaderKeywords;
                }
                return default;
            }

            public override void OnBeforeBackgroundRender(int id)
            {
                // Do nothing
                // Debug.Log($"OnBeforeBackgroundRender: {id}");
            }

            private static XRCameraFrame CopyOnlySafeFrame(in XRCameraFrame input)
            {
                XRCameraFrameProperties properties = input.properties;
                // Override unsupported properties
                properties &= ~XRCameraFrameProperties.CameraGrain;
                properties &= ~XRCameraFrameProperties.ExifData;


                input.TryGetTimestamp(out var timestamp);
                input.TryGetAverageBrightness(out var averageBrightness);
                input.TryGetAverageColorTemperature(out var averageColorTemperature);
                input.TryGetColorCorrection(out var colorCorrection);
                input.TryGetProjectionMatrix(out var projectionMatrix);
                input.TryGetDisplayMatrix(out var displayMatrix);
                input.TryGetAverageIntensityInLumens(out var averageIntensityInLumens);
                input.TryGetExposureDuration(out double exposureDuration);
                input.TryGetExposureOffset(out var exposureOffset);
                input.TryGetMainLightIntensityLumens(out var mainLightIntensityLumens);
                input.TryGetMainLightColor(out var mainLightColor);
                input.TryGetMainLightDirection(out var mainLightDirection);
                input.TryGetAmbientSphericalHarmonics(out var ambientSphericalHarmonics);
                input.TryGetNoiseIntensity(out var noiseIntensity);

                return new XRCameraFrame(
                    timestamp: timestamp,
                    averageBrightness: averageBrightness,
                    averageColorTemperature: averageColorTemperature,
                    colorCorrection: colorCorrection,
                    projectionMatrix: projectionMatrix,
                    displayMatrix: displayMatrix,
                    trackingState: input.trackingState,
                    nativePtr: IntPtr.Zero,
                    properties: properties,
                    averageIntensityInLumens: averageIntensityInLumens,
                    exposureDuration: exposureDuration,
                    exposureOffset: exposureOffset,
                    mainLightIntensityInLumens: mainLightIntensityLumens,
                    mainLightColor: mainLightColor,
                    mainLightDirection: mainLightDirection,
                    ambientSphericalHarmonics: ambientSphericalHarmonics,
                    cameraGrain: default,
                    noiseIntensity: noiseIntensity,
                    exifData: default
                );
            }
        }
    }
}
