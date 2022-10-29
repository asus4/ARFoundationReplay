using UnityEngine;
using Unity.XR.CoreUtils;

namespace ARFoundationReplay
{
    internal sealed class TrackedPoseEncoder : IEncoder
    {
        private Packet _packet;
        private Transform _target;

        public bool Initialize(XROrigin origin, Packet packet, Material material)
        {
            _packet = packet;
            _target = origin.Camera.transform;
            return true;
        }

        public void Dispose()
        {
            _packet = null;
            _target = null;
        }

        public void Update()
        {
            _packet.trackedPose = new Pose
            {
                position = _target.localPosition,
                rotation = _target.localRotation,
            };
        }
    }
}
