#if ARCORE_EXTENSIONS_ENABLED
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.XR.CoreUtils;
using Google.XR.ARCoreExtensions;

namespace ARFoundationReplay
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct GeospatialEarthPacket
    {
        public EarthState earthState;
        public TrackingState trackingState;
        public GeospatialPose geospatialPose;
    }

    /// <summary>
    /// Encode Geospatial Earth data into the metadata.
    /// Only available when ARCore Extensions package is installed.
    /// </summary>
    internal sealed class GeospatialEarthEncoder : ISubsystemEncoder
    {
        private AREarthManager _earthManager;
        public bool Initialize(XROrigin origin, Material muxMaterial)
        {
            _earthManager = origin.GetComponentInChildren<AREarthManager>();
            if (_earthManager == null)
            {
                return false;
            }
            return true;
        }

        public void Dispose()
        {
            _earthManager = null;
        }

        public void Encode(FrameMetadata metadata)
        {
            var packet = new GeospatialEarthPacket
            {
                earthState = _earthManager.EarthState,
                trackingState = _earthManager.EarthTrackingState,
                geospatialPose = _earthManager.CameraGeospatialPose,
            };
            metadata.extraTracks[(int)ExternalTrackID.ARCoreGeospatialEarth] = packet.ToByteArray();
        }

        public void PostEncode(FrameMetadata metadata)
        {
            // _packet.Reset();
        }
    }
}
#endif // ARCORE_EXTENSIONS_ENABLED
