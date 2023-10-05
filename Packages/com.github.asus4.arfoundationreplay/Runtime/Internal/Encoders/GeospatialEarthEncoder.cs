#if ARCORE_EXTENSIONS_ENABLED
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
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

        public TrackID ID => TrackID.ARCoreGeospatialEarth;

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

        public bool TryEncode(out object data)
        {
            var packet = new GeospatialEarthPacket
            {
                earthState = _earthManager.EarthState,
                trackingState = _earthManager.EarthTrackingState,
                geospatialPose = _earthManager.CameraGeospatialPose,
            };
            data = packet.ToByteArray();
            return true;
        }

        public void PostEncode()
        {
            // Nothing to do
        }
    }
}
#endif // ARCORE_EXTENSIONS_ENABLED
