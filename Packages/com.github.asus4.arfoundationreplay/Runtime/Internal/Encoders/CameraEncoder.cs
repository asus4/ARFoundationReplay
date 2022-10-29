using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

namespace ARFoundationReplay
{
    internal sealed class CameraEncoder : IEncoder
    {

        private Packet _packet;
        private Material _material;
        private ARCameraManager _cameraManager;


        public bool Initialize(XROrigin origin, Packet packet, Material material)
        {
            _packet = packet;
            _material = material;
            _cameraManager = origin.Camera.GetComponent<ARCameraManager>();
            if (_cameraManager == null)
            {
                return false;
            }
            _cameraManager.frameReceived += OnCameraFrameReceived;
            return true;
        }

        public void Dispose()
        {
            if (_cameraManager != null)
            {
                _cameraManager.frameReceived -= OnCameraFrameReceived;
            }
            _cameraManager = null;
            _packet = null;
            _material = null;
        }

        public void Update()
        {
            // Nothing to do
        }

        private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
        {
            if (_packet == null || _material == null)
            {
                return;
            }

            // Set texture
            var count = args.textures.Count;
            for (int i = 0; i < count; i++)
            {
                _material.SetTexture(args.propertyNameIds[i], args.textures[i]);
            }

            _packet.cameraFrame = new Packet.CameraFrameEvent()
            {
                timestampNs = args.timestampNs.Value,
                projectionMatrix = args.projectionMatrix.Value,
                displayMatrix = args.displayMatrix.Value
            };
        }
    }
}
