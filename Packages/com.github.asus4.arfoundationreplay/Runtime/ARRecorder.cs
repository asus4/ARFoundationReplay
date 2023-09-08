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

        private XROrigin _origin;
        private VideoRecorder _videoRecorder;
        private RenderTexture _renderTexture;
        private Material _bufferMaterial;

        private Packet _packet;
        private IEncoder[] _encoders;
        public bool IsRecording => _videoRecorder.IsRecording;

        private void Awake()
        {
            _origin = FindFirstObjectByType<XROrigin>();
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
            _renderTexture = new RenderTexture(options.width, options.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var shader = Shader.Find("Hidden/ARFoundationReplay/ARKitEncoder");
            Assert.IsNotNull(shader);
            _bufferMaterial = new Material(shader);
            _videoRecorder = new VideoRecorder(_renderTexture, options.targetFrameRate);
        }

        private void OnDestroy()
        {
            if (IsRecording)
            {
                StopRecording();
            }
            DisposeUtil.Dispose(_bufferMaterial);
            DisposeUtil.Dispose(_renderTexture);
            _videoRecorder?.Dispose();
        }

        private void Update()
        {
            if (!IsRecording) { return; }

            foreach (var encoder in _encoders)
            {
                encoder.Update();
            }

            Graphics.Blit(null, _renderTexture, _bufferMaterial);

            kSerializeMarker.Begin();
            var metadata = _packet.Serialize();
            kSerializeMarker.End();
            _videoRecorder.Update(metadata);
        }

        public void StartRecording()
        {
            if (IsRecording) { return; }
            Debug.Log($"ARRecorder.StartRecording");

            _packet = new Packet();
            _videoRecorder.StartRecording();

            // Initialize encoders and filter unavailable out
            _encoders = CreateAllEncoders()
                .Where(encoder =>
                {
                    bool available = encoder.Initialize(_origin, _packet, _bufferMaterial);
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

        private static IEncoder[] CreateAllEncoders()
            => new IEncoder[]
            {
                new CameraEncoder(),
                new TrackedPoseEncoder(),
                new OcclusionEncoder(),
            };
    }
}
