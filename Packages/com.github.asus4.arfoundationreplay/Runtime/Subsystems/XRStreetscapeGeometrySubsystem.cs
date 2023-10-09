#if ARCORE_EXTENSIONS_ENABLED
using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SubsystemsImplementation;
using UnityEngine.XR.ARSubsystems;

namespace ARFoundationReplay
{
    using NativeTrackableId = UnityEngine.XR.ARSubsystems.TrackableId;

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
        : TrackingSubsystem<StreetscapeGeometry, XRStreetscapeGeometrySubsystem, XRStreetscapeGeometrySubsystemDescriptor, XRStreetscapeGeometrySubsystem.Provider>
    {
        public static bool Register(XRStreetscapeGeometrySubsystemSubsystemCinfo info)
        {
            var descriptor = new XRStreetscapeGeometrySubsystemDescriptor(info);
            SubsystemDescriptorStore.RegisterDescriptor(descriptor);
            return true;
        }

        public override TrackableChanges<StreetscapeGeometry> GetChanges(Allocator allocator)
        {
            return provider.GetChanges(default, allocator);
        }

        public bool TryGetMesh(NativeTrackableId trackableId, out Mesh mesh)
        {
            return provider.TryGetMesh(trackableId, out mesh);
        }

        public abstract class Provider : SubsystemProvider<XRStreetscapeGeometrySubsystem>
        {
            public abstract TrackableChanges<StreetscapeGeometry> GetChanges(
                StreetscapeGeometry defaultGeometry,
                Allocator allocator);

            public abstract bool TryGetMesh(NativeTrackableId trackableId, out Mesh mesh);
        }
    }
}
#endif // ARCORE_EXTENSIONS_ENABLED
