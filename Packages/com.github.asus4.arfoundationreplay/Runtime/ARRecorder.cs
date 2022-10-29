using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.XR.CoreUtils;

namespace ARFoundationReplay
{
    public sealed class ARRecorder : MonoBehaviour
    {
        private XROrigin _origin;
        private VideoRecorder _videoRecorder;
        private RenderTexture _renderTexture;
        private Material _bufferMaterial;

        private Packet _packet;
        private IEncoder[] _encoders;
        public bool IsRecording => _videoRecorder.IsRecording;

        private void Awake()
        {
            _origin = FindObjectOfType<XROrigin>();
            if (_origin == null)
            {
                Debug.LogError("ARRecorder requires ARSessionOrigin in the scene");
                enabled = false;
                return;
            }

            // Nullable
            _renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var shader = Shader.Find("Hidden/ARFoundationReplay/ARKitEncoder");
            Assert.IsNotNull(shader);
            _bufferMaterial = new Material(shader);
            _videoRecorder = new VideoRecorder(_renderTexture);
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
            var metadata = _packet.Serialize();
            _videoRecorder.Update(metadata);
        }

        public void StartRecording()
        {
            if (IsRecording) { return; }
            Debug.Log("StartRecording");
            _packet = new Packet();
            _videoRecorder.StartRecording();

            // Initialize available encoders
            _encoders = new IEncoder[]
                {
                    new CameraEncoder(),
                    new TrackedPoseEncoder(),
                    new OcclusionEncoder(),
                }
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
            Debug.Log("StopRecording");

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

    }
}
