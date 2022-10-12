using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ARFoundationRecorder
{
    public sealed class ARRecorder : System.IDisposable
    {
        private readonly ARSessionOrigin _origin;
        private readonly ARCameraManager _cameraManager;
        private readonly VideoRecorder _videoRecorder;
        private readonly RenderTexture _renderTexture;

        public bool IsRecording => _videoRecorder.IsRecording;

        public ARRecorder(ARSessionOrigin origin)
        {
            _origin = origin;
            _cameraManager = _origin.camera.GetComponent<ARCameraManager>();
            _renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
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
            _cameraManager.frameReceived += OnFrameReceived;
        }

        public void StopRecording()
        {
            if (!IsRecording) { return; }
            Debug.Log("StopRecording");
            _cameraManager.frameReceived -= OnFrameReceived;
            _videoRecorder.EndRecording();
        }

        private void OnFrameReceived(ARCameraFrameEventArgs args)
        {
            if (!IsRecording) { return; }

            Graphics.Blit(null, _renderTexture, _cameraManager.cameraMaterial);
            var packet = new Packet()
            {
                cameraFrame = new Packet.CameraFrameEvent()
                {
                    timestampNs = args.timestampNs.Value,
                    projectionMatrix = args.projectionMatrix.Value,
                    displayMatrix = args.displayMatrix.Value
                }
            };
            using var binary = new NativeArray<byte>(packet.Serialize(), Allocator.Temp);
            _videoRecorder.Update(binary);
        }
    }
}