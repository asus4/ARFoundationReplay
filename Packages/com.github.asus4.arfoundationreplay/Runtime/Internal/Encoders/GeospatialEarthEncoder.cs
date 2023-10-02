#if ARCORE_EXTENSIONS_ENABLED
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.XR.CoreUtils;

namespace ARFoundationReplay
{
    internal sealed class GeospatialEarthEncoder : ISubsystemEncoder
    {
        public bool Initialize(XROrigin origin, Material muxMaterial)
        {
            return false;
        }

        public void Dispose()
        {
        }

        public void Encode(FrameMetadata metadata)
        {
            // metadata.plane = _packet.IsAvailable ? _packet : null;
        }

        public void PostEncode(FrameMetadata metadata)
        {
            // _packet.Reset();
        }
    }
}
#endif // ARCORE_EXTENSIONS_ENABLED
