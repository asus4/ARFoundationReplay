using UnityEngine;
using Unity.XR.CoreUtils;

namespace ARFoundationReplay
{
    internal sealed class InputEncoder : ISubsystemEncoder
    {
        private Transform _target;

        public bool Initialize(XROrigin origin, Material muxMaterial)
        {
            _target = origin.Camera.transform;
            return true;
        }

        public void Dispose()
        {
            _target = null;
        }

        public void Encode(FrameMetadata metadata)
        {
            metadata.input = new Pose
            {
                position = _target.localPosition,
                rotation = _target.localRotation,
            };
        }
    }
}
