#if ARCORE_EXTENSIONS_ENABLED
using System;
using UnityEngine.SubsystemsImplementation;
using UnityEngine.XR.ARSubsystems;
using Google.XR.ARCoreExtensions;

namespace ARFoundationReplay
{
    public struct XRGeospatialEarthSubsystemSubsystemCinfo
    {
        public string id;
        public Type providerType;
        public Type subsystemTypeOverride;
    }

    public class XRGeospatialEarthSubsystemDescriptor
    : SubsystemDescriptorWithProvider<XRGeospatialEarthSubsystem, XRGeospatialEarthSubsystem.Provider>
    {
        public XRGeospatialEarthSubsystemDescriptor(XRGeospatialEarthSubsystemSubsystemCinfo info)
        {
            id = info.id;
            providerType = info.providerType;
            subsystemTypeOverride = info.subsystemTypeOverride;
        }
    }

    public class XRGeospatialEarthSubsystem
        : SubsystemWithProvider<XRGeospatialEarthSubsystem, XRGeospatialEarthSubsystemDescriptor, XRGeospatialEarthSubsystem.Provider>
    {
        public EarthState EarthState => provider.EarthState;
        public TrackingState EarthTrackingState => provider.EarthTrackingState;
        public GeospatialPose CameraGeospatialPose => provider.CameraGeospatialPose;

        public static bool Register(XRGeospatialEarthSubsystemSubsystemCinfo info)
        {
            var descriptor = new XRGeospatialEarthSubsystemDescriptor(info);
            SubsystemDescriptorStore.RegisterDescriptor(descriptor);
            return true;
        }

        public abstract class Provider : SubsystemProvider<XRGeospatialEarthSubsystem>
        {
            public abstract EarthState EarthState { get; }
            public abstract TrackingState EarthTrackingState { get; }
            public abstract GeospatialPose CameraGeospatialPose { get; }
        }
    }
}
#endif // ARCORE_EXTENSIONS_ENABLED
