using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

namespace ARFoundationRecorder
{
    public sealed class ARRecorder : System.IDisposable
    {
        private readonly XROrigin _origin;
        private readonly ARCameraManager _cameraManager;
        private readonly VideoRecorder _videoRecorder;
        private readonly RenderTexture _renderTexture;
        private readonly Material _bufferMaterial;


        public bool IsRecording => _videoRecorder.IsRecording;

        public ARRecorder(XROrigin origin)
        {
            _origin = origin;
            _cameraManager = _origin.Camera.GetComponent<ARCameraManager>();
            _renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var shader = Shader.Find("Hidden/ARRecording/ARKitEncoder");
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

            // Set texture
            var count = args.textures.Count;
            for (int i = 0; i < count; i++)
            {
                _bufferMaterial.SetTexture(args.propertyNameIds[i], args.textures[i]);
            }

            Graphics.Blit(null, _renderTexture, _bufferMaterial);
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
