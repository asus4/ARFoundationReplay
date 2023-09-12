using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;


namespace ARFoundationReplay
{
    internal sealed class PlaneEncoder : IEncoder
    {
        public bool Initialize(XROrigin origin, Packet packet, Material material)
        {
            return false;
        }

        public void Dispose()
        {
            // Nothing to do
        }

        public void Update()
        {
            // Nothing to do
        }
    }
}
