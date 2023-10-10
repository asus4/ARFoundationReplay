using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Profiling;
using Unity.XR.CoreUtils;
using System.Collections.Generic;

namespace ARFoundationReplay
{
    /// <summary>
    /// Records video file with timeline metadata.
    /// </summary>
    public sealed class ARRecorder : MonoBehaviour
    {
        [System.Serializable]
        public struct Options
        {
            [Min(640)]
            public int width;
            [Min(480)]
            public int height;
            [Range(10, 60)]
            public int targetFrameRate;
        }

        public Options options;

        static readonly ProfilerMarker kSerializeMarker = new("ARRecorder.Serialize");

        [SerializeField]
        private XROrigin _origin = null;
        private VideoRecorder _videoRecorder;
        private RenderTexture _muxTexture;
        private Material _muxMaterial;

        private FrameMetadata _metadata;
        private ISubsystemEncoder[] _encoders;
        private int _maxMetadataBytes = 0;
        public bool IsRecording => _videoRecorder.IsRecording;

        private static bool _needWarmedUp = Application.platform == RuntimePlatform.IPhonePlayer;

        private void Awake()
        {
            if (_origin == null)
            {
                _origin = FindFirstObjectByType<XROrigin>();
            }
            if (_origin == null)
            {
                Debug.LogError("ARRecorder requires ARSessionOrigin in the scene");
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            // Nullable
            _muxTexture = new RenderTexture(options.width, options.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var shader = Shader.Find("Hidden/ARFoundationReplay/ARKitEncoder");
            Assert.IsNotNull(shader);
            _muxMaterial = new Material(shader);
            _videoRecorder = new VideoRecorder(_muxTexture, options.targetFrameRate);
            _maxMetadataBytes = 0;
            if (_needWarmedUp)
            {
                _videoRecorder.WarmUp();
                _needWarmedUp = false;
            }
        }

        private void OnDestroy()
        {
            if (IsRecording)
            {
                StopRecording();
            }
            DisposeUtil.Dispose(_muxMaterial);
            DisposeUtil.Dispose(_muxTexture);
            _videoRecorder?.Dispose();
        }

        private void LateUpdate()
        {
            if (!IsRecording) { return; }

            foreach (var encoder in _encoders)
            {
                encoder.Encode(_metadata);
            }

            // Multiplexing RGB and depth textures into a texture
            Graphics.Blit(null, _muxTexture, _muxMaterial);

            kSerializeMarker.Begin();
            // TODO: Consider using faster serializer
            // instead of using BinaryFormatter?
            // https://github.com/Cysharp/MemoryPack
            // https://docs.unity3d.com/Packages/com.unity.serialization@3.1/manual/index.html
            var metadata = _metadata.Serialize();
            kSerializeMarker.End();

            _videoRecorder.Update(metadata);
            _maxMetadataBytes = Mathf.Max(_maxMetadataBytes, metadata.Length);
        }

        public void StartRecording()
        {
            if (IsRecording) { return; }

            _metadata = new FrameMetadata();

            // Initialize encoders and filter unavailable out
            _encoders = CreateAllEncoders()
                .Where(encoder =>
                {
                    bool available = encoder.Initialize(_origin, _muxMaterial);
                    if (!available)
                    {
                        encoder.Dispose();
                    }
                    return available;
                })
                .ToArray();

            _videoRecorder.StartRecording(MakeFileMetadata());

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("ARRecorder.StartRecording - active encoders:");
            foreach (var encoder in _encoders)
            {
                sb.AppendLine($"  {encoder}");
            }
            Debug.Log(sb.ToString());
        }

        public void StopRecording()
        {
            if (!IsRecording) { return; }
            Debug.Log("ARRecorder.StopRecording");

            _videoRecorder.EndRecording();

            // Dispose encoders
            if (_encoders != null)
            {
                foreach (var encoder in _encoders)
                {
                    encoder.Dispose();
                }
                _encoders = null;
            }
        }

        private Dictionary<string, string> MakeFileMetadata()
        {
            var metadata = new FileMetadata()
            {
                version = Config.VERSION,
                modelName = SystemInfo.deviceModel,
                screenWidth = Screen.width,
                screenHeight = Screen.height,
                encoders = _encoders.Select(encoder => encoder.GetType().Name).ToArray(),
            };
            return new Dictionary<string, string>()
            {
                { FileMetadata.KEY,  metadata.Serialize() },
            };
        }

        private static ISubsystemEncoder[] CreateAllEncoders()
            => new ISubsystemEncoder[]
            {
                new CameraEncoder(),
                new InputEncoder(),
                new OcclusionEncoder(),
                new PlaneEncoder(),
                // Working in progress
                // new MeshEncoder(),
#if ARCORE_EXTENSIONS_ENABLED
                // Optional encoders for ARCore Geospatial
                new GeospatialEarthEncoder(),
                new StreetscapeGeometryEncoder(),
#endif // ARCORE_EXTENSIONS_ENABLED
            };
    }
}
