using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.Mathematics;
using Unity.XR.CoreUtils;

namespace ARFoundationReplay
{
    [Serializable]
    public struct CameraPacket : IEquatable<CameraPacket>
    {
        // public ARLightEstimationData lightEstimation;
        public long timestampNs;
        public float4x4 projectionMatrix;
        public float4x4 displayMatrix;
        // public double? exposureDuration;
        // public float? exposureOffset;

        public bool Equals(CameraPacket o)
        {
            return timestampNs.Equals(o.timestampNs)
                && projectionMatrix.Equals(o.projectionMatrix)
                && displayMatrix.Equals(o.displayMatrix);
        }

        public override string ToString()
        {
            return $"[time: {timestampNs}, projection: {projectionMatrix}, display: {displayMatrix}]";
        }
    }

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
            // Set texture
            var count = args.textures.Count;
            for (int i = 0; i < count; i++)
            {
                _material.SetTexture(args.propertyNameIds[i], args.textures[i]);
            }

            _packet.camera = new CameraPacket()
            {
                timestampNs = args.timestampNs.Value,
                projectionMatrix = args.projectionMatrix.Value,
                displayMatrix = args.displayMatrix.Value
            };
        }
    }
}
