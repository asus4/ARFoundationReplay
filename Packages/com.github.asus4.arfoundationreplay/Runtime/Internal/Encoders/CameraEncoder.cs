using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;
using UnityEngine.XR.ARSubsystems;

namespace ARFoundationReplay
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct CameraPacket
    {
        // [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.I1, SizeConst = 448)]
        public byte[] cameraFrame;

        public readonly XRCameraFrame CameraFrame
            => cameraFrame == null ? default : cameraFrame.ToStruct<XRCameraFrame>();

        public override readonly string ToString()
        {
            return CameraFrame.ToString();
        }
    }

    internal sealed class CameraEncoder : ISubsystemEncoder
    {
        private Material _muxMaterial;
        private ARCameraManager _cameraManager;
        private Camera _camera;
        private XRCameraFrame _cameraFrame;

        public bool Initialize(XROrigin origin, Material muxMaterial)
        {
            _muxMaterial = muxMaterial;
            _camera = origin.Camera;
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
            metadata.camera = new CameraPacket
            {
                cameraFrame = _cameraFrame.ToByteArray(),
            };
        }

        private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
        {
            // Set texture
            var count = args.textures.Count;
            for (int i = 0; i < count; i++)
            {
                _muxMaterial.SetTexture(args.propertyNameIds[i], args.textures[i]);
            }

            // Get XR Camera Frame
            var cameraParams = new XRCameraParams
            {
                zNear = _camera.nearClipPlane,
                zFar = _camera.farClipPlane,
                screenWidth = Screen.width,
                screenHeight = Screen.height,
                screenOrientation = Screen.orientation
            };
            if (_cameraManager.subsystem.TryGetLatestFrame(cameraParams, out XRCameraFrame frame))
            {
                _cameraFrame = frame;
            }
        }
    }
}
