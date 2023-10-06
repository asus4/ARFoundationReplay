#if ARCORE_EXTENSIONS_ENABLED
using System;
using UnityEngine.SubsystemsImplementation;

namespace ARFoundationReplay
{
    public struct XRStreetscapeGeometrySubsystemSubsystemCinfo
    {
        public string id;
        public Type providerType;
        public Type subsystemTypeOverride;
    }

    public class XRStreetscapeGeometrySubsystemDescriptor
        : SubsystemDescriptorWithProvider<XRStreetscapeGeometrySubsystem, XRStreetscapeGeometrySubsystem.Provider>
    {
        public XRStreetscapeGeometrySubsystemDescriptor(XRStreetscapeGeometrySubsystemSubsystemCinfo info)
        {
            id = info.id;
            providerType = info.providerType;
            subsystemTypeOverride = info.subsystemTypeOverride;
        }
    }

    public class XRStreetscapeGeometrySubsystem
        : SubsystemWithProvider<XRStreetscapeGeometrySubsystem, XRStreetscapeGeometrySubsystemDescriptor, XRStreetscapeGeometrySubsystem.Provider>
    {
        public static bool Register(XRStreetscapeGeometrySubsystemSubsystemCinfo info)
        {
            var descriptor = new XRStreetscapeGeometrySubsystemDescriptor(info);
            SubsystemDescriptorStore.RegisterDescriptor(descriptor);
            return true;
        }

        public abstract class Provider : SubsystemProvider<XRStreetscapeGeometrySubsystem>
        {
        }
    }
}
#endif // ARCORE_EXTENSIONS_ENABLED
