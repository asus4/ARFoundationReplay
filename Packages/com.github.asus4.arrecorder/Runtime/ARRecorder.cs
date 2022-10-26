using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR.ARFoundation;
using Unity.Collections;
using Unity.XR.CoreUtils;

namespace ARRecorder
{
    public sealed class ARRecorder : System.IDisposable
    {
        private readonly XROrigin _origin;
        private readonly ARCameraManager _cameraManager;
        private readonly AROcclusionManager _occlusionManager;
        private readonly VideoRecorder _videoRecorder;
        private readonly RenderTexture _renderTexture;
        private readonly Material _bufferMaterial;

        private int _updatedFrame;
        private Packet _packet;
        public bool IsRecording => _videoRecorder.IsRecording;

        public ARRecorder(XROrigin origin)
        {
            _origin = origin;
            _cameraManager = _origin.Camera.GetComponent<ARCameraManager>();
            // Nullable
            _occlusionManager = _origin.GetComponentInChildren<AROcclusionManager>();
            _renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var shader = Shader.Find("Hidden/ARRecorder/ARKitEncoder");
            Assert.IsNotNull(shader);
            _bufferMaterial = new Material(shader);
            _videoRecorder = new VideoRecorder(_renderTexture);
        }

        public void Dispose()
        {
            if (IsRecording)
            {
                StopRecording();
            }
            Object.Destroy(_renderTexture);
            _videoRecorder.Dispose();
        }

        public void StartRecording()
        {
            if (IsRecording) { return; }
            Debug.Log("StartRecording");
            _videoRecorder.StartRecording();
            _cameraManager.frameReceived += OnCameraFrameReceived;
            if (_occlusionManager != null)
            {
                _occlusionManager.frameReceived += OnOcclusionFrameReceived;
            }
        }

        public void StopRecording()
        {
            if (!IsRecording) { return; }
            Debug.Log("StopRecording");
            _cameraManager.frameReceived -= OnCameraFrameReceived;
            if (_occlusionManager != null)
            {
                _occlusionManager.frameReceived -= OnOcclusionFrameReceived;
            }
            _videoRecorder.EndRecording();
        }

        private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
        {
            if (!IsRecording) { return; }

            // Set texture
            var count = args.textures.Count;
            for (int i = 0; i < count; i++)
            {
                _bufferMaterial.SetTexture(args.propertyNameIds[i], args.textures[i]);
            }

            _packet = new Packet()
            {
                cameraFrame = new Packet.CameraFrameEvent()
                {
                    timestampNs = args.timestampNs.Value,
                    projectionMatrix = args.projectionMatrix.Value,
                    displayMatrix = args.displayMatrix.Value
                }
            };

            // Update if occlusion is not available
            if (_occlusionManager == null)
            {
                UpdateRecorder();
            }
        }

        private void OnOcclusionFrameReceived(AROcclusionFrameEventArgs args)
        {
            if (!IsRecording) { return; }

            // Set texture
            var count = args.textures.Count;
            for (int i = 0; i < count; i++)
            {
                _bufferMaterial.SetTexture(args.propertyNameIds[i], args.textures[i]);
            }

            UpdateRecorder();
        }

        private void UpdateRecorder()
        {
            Graphics.Blit(null, _renderTexture, _bufferMaterial);
            var metadata = _packet.Serialize();
            _videoRecorder.Update(metadata);
        }
    }
}
