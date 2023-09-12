using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;


namespace ARFoundationReplay
{
    internal sealed class PlaneEncoder : ISubsystemEncoder
    {
        public bool Initialize(XROrigin origin, Material muxMaterial)
        {
            // Not implemented
            return false;
        }

        public void Dispose()
        {
            // Nothing to do
        }

        public void Encode(FrameMetadata metadata)
        {
            // Nothing to do
        }
    }
}
