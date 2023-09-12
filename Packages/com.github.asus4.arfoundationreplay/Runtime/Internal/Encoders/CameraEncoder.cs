using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.Mathematics;
using Unity.XR.CoreUtils;

namespace ARFoundationReplay
{
    [Serializable]
    public struct CameraPacket
    {
        // public ARLightEstimationData lightEstimation;
        public long timestampNs;
        public float4x4 projectionMatrix;
        public float4x4 displayMatrix;
        // public double? exposureDuration;
        // public float? exposureOffset;

        public override readonly string ToString()
        {
            return $"[time: {timestampNs}, projection: {projectionMatrix}, display: {displayMatrix}]";
        }
    }

    internal sealed class CameraEncoder : ISubsystemEncoder
    {
        private Material _muxMaterial;
        private ARCameraManager _cameraManager;
        private CameraPacket _cameraPacket;

        public bool Initialize(XROrigin origin, Material muxMaterial)
        {
            _muxMaterial = muxMaterial;
            if (!origin.Camera.TryGetComponent(out _cameraManager))
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
            _muxMaterial = null;
        }

        public void Encode(FrameMetadata metadata)
        {
            metadata.camera = _cameraPacket;
        }

        private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
        {
            // Set texture
            var count = args.textures.Count;
            for (int i = 0; i < count; i++)
            {
                _muxMaterial.SetTexture(args.propertyNameIds[i], args.textures[i]);
            }

            _cameraPacket = new CameraPacket()
            {
                timestampNs = args.timestampNs.Value,
                projectionMatrix = args.projectionMatrix.Value,
                displayMatrix = args.displayMatrix.Value
            };
        }
    }
}
