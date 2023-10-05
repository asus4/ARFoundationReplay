using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Profiling;
using Unity.XR.CoreUtils;

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
                if (encoder.TryEncode(out object data))
                {
                    _metadata.tracks[encoder.ID] = data;
                }
                else
                {
                    _metadata.tracks.Remove(encoder.ID);
                }
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
        }

        public void StartRecording()
        {
            if (IsRecording) { return; }
            Debug.Log($"ARRecorder.StartRecording");

            _metadata = new FrameMetadata();
            _videoRecorder.StartRecording();

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
#endif // ARCORE_EXTENSIONS_ENABLED
            };
    }
}
