using UnityEngine;
using Unity.XR.CoreUtils;

namespace ARFoundationReplay
{
    internal sealed class InputEncoder : ISubsystemEncoder
    {
        private Transform _target;

        public TrackID ID => TrackID.Input;

        public bool Initialize(XROrigin origin, Material muxMaterial)
        {
            _target = origin.Camera.transform;
            return true;
        }

        public void Dispose()
        {
            _target = null;
        }

        public bool TryEncode(out object data)
        {
            var pose = new Pose(_target.localPosition, _target.localRotation);
            data = pose.ToByteArray();
            return true;
        }

        public void PostEncode()
        {
            // Nothing to do
        }
    }
}
