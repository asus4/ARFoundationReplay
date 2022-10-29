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
            static readonly int k_InputTextureID = Shader.PropertyToID("_InputTexture");
            static readonly int k_TextureYID = Shader.PropertyToID("_textureY");
            static readonly int k_TextureCbCrID = Shader.PropertyToID("_textureCbCr");

            static readonly List<string> k_URPEnabledMaterialKeywords = new() { "ARKIT_BACKGROUND_URP" };

            Material _beforeOpaquesCameraMaterial;
            Material _afterOpaquesCameraMaterial;
            int _kernel;
            ComputeShader _computeShader;
            RenderTexture _yTexture;
            RenderTexture _cbCrTexture;
            bool IsReplayAvailable => Application.isPlaying && ARReplay.Current != null;

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
                = XRSupportedCameraBackgroundRenderingMode.AfterOpaques;

            public override XRSupportedCameraBackgroundRenderingMode supportedBackgroundRenderingMode
                => XRSupportedCameraBackgroundRenderingMode.AfterOpaques;

            public override void Start()
            {
                _computeShader = Resources.Load<ComputeShader>("Shaders/ARKitDecoder");
                var size = Config.RecordResolution;
                _computeShader.SetInts("_TextureSize", size.x, size.y);
                _kernel = _computeShader.FindKernel("DecodeYCbCr");
                _yTexture = CreateRenderTexture(Config.RecordResolution, RenderTextureFormat.R8);
                _cbCrTexture = CreateRenderTexture(Config.RecordResolution, RenderTextureFormat.RG16);
                Debug.Log($"shader {_computeShader}");
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
                var replay = ARReplay.Current;
                if (!IsReplayAvailable || !replay.DidUpdateThisFrame)
                {
                    cameraFrame = default;
                    return false;
                }

                const XRCameraFrameProperties properties =
                    XRCameraFrameProperties.Timestamp
                    | XRCameraFrameProperties.ProjectionMatrix
                    | XRCameraFrameProperties.DisplayMatrix;

                var received = replay.Packet.cameraFrame;

                cameraFrame = (XRCameraFrame)new CameraFrame()
                {
                    timestampNs = received.timestampNs,
                    averageBrightness = 0,
                    averageColorTemperature = 0,
                    colorCorrection = default,
                    projectionMatrix = received.projectionMatrix,
                    displayMatrix = received.displayMatrix,
                    trackingState = TrackingState.Tracking,
                    nativePtr = IntPtr.Zero,
                    properties = properties,
                    averageIntensityInLumens = 0,
                    exposureDuration = 0,
                    exposureOffset = 0,
                    mainLightIntensityLumens = 0,
                    mainLightColor = default,
                    ambientSphericalHarmonics = default,
                    cameraGrain = default,
                    noiseIntensity = 0,
                };

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
                var replay = ARReplay.Current;
                if (!IsReplayAvailable || !replay.DidUpdateThisFrame)
                {
                    return new NativeArray<XRTextureDescriptor>(0, allocator);
                }

                // Decode Y + CbCr texture from video
                _computeShader.SetTexture(_kernel, k_InputTextureID, replay.Texture);
                _computeShader.SetTexture(_kernel, k_TextureYID, _yTexture);
                _computeShader.SetTexture(_kernel, k_TextureCbCrID, _cbCrTexture);
                _computeShader.Dispatch(_kernel, Config.RecordResolution.x / 8, Config.RecordResolution.y / 8, 1);

                var arr = new NativeArray<XRTextureDescriptor>(2, allocator);
                arr[0] = new TextureDescriptor(_yTexture, k_TextureYID);
                arr[1] = new TextureDescriptor(_cbCrTexture, k_TextureCbCrID);
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

            static RenderTexture CreateRenderTexture(Vector2Int size, RenderTextureFormat format)
            {
                var rt = new RenderTexture(new RenderTextureDescriptor(size.x, size.y, format)
                {
                    enableRandomWrite = true,
                    useMipMap = false,
                });
                rt.Create();
                return rt;
            }
        }
    }
}
