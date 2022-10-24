using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Video;
using Object = UnityEngine.Object;

namespace ARFoundationRecorder
{
    /// <summary>
    /// The camera subsystem for ARRecorder.
    /// </summary>
    [Preserve]
    internal sealed class ARRecorderCameraSubsystem : XRCameraSubsystem
    {
        public const string ID = "ARRecorder-Camera";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterDescriptor()
        {
            XRCameraSubsystemCinfo cameraSubsystemCinfo = new XRCameraSubsystemCinfo
            {
                id = ID,
                providerType = typeof(ARRecorderProvider),
                subsystemTypeOverride = typeof(ARRecorderCameraSubsystem),
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
        }

        class ARRecorderProvider : Provider
        {
            private static readonly int _TEXTURE_MAIN = Shader.PropertyToID("_MainTex");
            private Material _material;
            private VideoPlayer _player;
            
            public override Material cameraMaterial => _material;


            public override void Start()
            {
                base.Start();
                if (_material == null)
                {
                    _material = CreateCameraMaterial("Unlit/WebcamBackground");
                }
#if UNITY_EDITOR
                var setting = ARRecorderSettings.currentSettings;
                string path = setting.GetRecordPath();
#else
                string path = "";
#endif
                Debug.Log($"Start {ID}: {path}");
                _player = CreateVideoPlayer(path);
            }

            public override void Stop()
            {
                if (_player != null)
                {
                    _player.Stop();
                }
                base.Stop();
            }

            public override void Destroy()
            {
                if (_player != null)
                {
                    _player.Stop();
                    if (Application.isEditor)
                    {
                        Object.DestroyImmediate(_player);
                    }
                    else
                    {
                        Object.Destroy(_player);
                    }
                }
                base.Destroy();
            }

            public override Feature currentCamera => Feature.AnyCamera;
            public override Feature requestedCamera
            {
                get => Feature.AnyCamera;
                set
                {
                    // Debug.Log($"requestedCamera: {value}")
                }
            }

            public override bool TryGetFrame(XRCameraParams cameraParams, out XRCameraFrame cameraFrame)
            {
                if (!Application.isPlaying || !_player.isPrepared)
                {
                    cameraFrame = default(XRCameraFrame);
                    return false;
                }

                const XRCameraFrameProperties properties =
                    XRCameraFrameProperties.Timestamp
                    // | XRCameraFrameProperties.ProjectionMatrix
                    | XRCameraFrameProperties.DisplayMatrix;

                cameraFrame = (XRCameraFrame)new CameraFrame()
                {
                    timestampNs = DateTime.Now.Ticks,
                    averageBrightness = 0,
                    averageColorTemperature = 0,
                    colorCorrection = default,
                    projectionMatrix = Matrix4x4.identity,
                    displayMatrix = Matrix4x4.identity,
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
                if (!Application.isPlaying || !_player.isPrepared)
                {
                    return new NativeArray<XRTextureDescriptor>(0, allocator);
                }

                var arr = new NativeArray<XRTextureDescriptor>(1, allocator);
                arr[0] = new TextureDescriptor(_player.texture, _TEXTURE_MAIN);

                // var tex = _player.texture;
                // Debug.Log($"{_player.isPlaying}: {tex.width}x{tex.height}");
                return arr;
            }

            private static VideoPlayer CreateVideoPlayer(string path)
            {
                var gameObject = new GameObject(typeof(ARRecorderCameraSubsystem).ToString());
                Object.DontDestroyOnLoad(gameObject);

                var player = gameObject.AddComponent<VideoPlayer>();
                player.source = VideoSource.Url;
                player.url = "file://" + path;
                player.playOnAwake = true;
                player.isLooping = true;
                player.skipOnDrop = true;
                player.renderMode = VideoRenderMode.APIOnly;
                player.audioOutputMode = VideoAudioOutputMode.None;
                player.SetDirectAudioMute(0, true);
                player.playbackSpeed = 1;

                return player;
            }
        }
    }
}
