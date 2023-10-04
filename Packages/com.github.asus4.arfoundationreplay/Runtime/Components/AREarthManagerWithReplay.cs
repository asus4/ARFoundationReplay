#if ARCORE_EXTENSIONS_ENABLED

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Google.XR.ARCoreExtensions;
using UnityEngine.XR.ARSubsystems;

namespace ARFoundationReplay
{
    public sealed class AREarthManagerWithReplay : AREarthManager
    {
        private bool _useReplay = false;
        private XRGeospatialEarthSubsystem _subsystem;

        private void Awake()
        {
            _useReplay = false;
            if (ARFoundationReplayLoader.TryGetLoader(out var loader))
            {
                _subsystem = loader.GetLoadedSubsystem<XRGeospatialEarthSubsystem>();
                if (_subsystem != null)
                {
                    _useReplay = true;
                }
            }
        }

        public new EarthState EarthState => _useReplay
            ? _subsystem.EarthState
            : base.EarthState;

        public new TrackingState EarthTrackingState => _useReplay
            ? _subsystem.EarthTrackingState
            : base.EarthTrackingState;

        public new GeospatialPose CameraGeospatialPose => _useReplay
            ? _subsystem.CameraGeospatialPose
            : base.CameraGeospatialPose;

        public new FeatureSupported IsGeospatialModeSupported(GeospatialMode mode)
        {
            return _useReplay
                ? FeatureSupported.Supported
                : base.IsGeospatialModeSupported(mode);
        }
    }
}
#endif // ARCORE_EXTENSIONS_ENABLED
