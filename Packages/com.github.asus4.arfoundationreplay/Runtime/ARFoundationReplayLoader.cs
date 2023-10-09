using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;

namespace ARFoundationReplay
{
    /// <summary>
    /// An entry point of the ARFoundation Replay.
    /// </summary>
    public sealed class ARFoundationReplayLoader : XRLoaderHelper
    {
        public override bool Initialize()
        {
            // Required subsystems
            CreateSubsystem<XRSessionSubsystemDescriptor, XRSessionSubsystem>(new(), ARReplaySessionSubsystem.ID);
            CreateSubsystem<XRCameraSubsystemDescriptor, XRCameraSubsystem>(new(), ARReplayCameraSubsystem.ID);
            CreateSubsystem<XRInputSubsystemDescriptor, XRInputSubsystem>(new(), ARReplayInputSubsystem.ID);

            // Optional subsystems
            CreateSubsystem<XROcclusionSubsystemDescriptor, XROcclusionSubsystem>(new(), ARReplayOcclusionSubsystem.ID);
            CreateSubsystem<XRPlaneSubsystemDescriptor, XRPlaneSubsystem>(new(), ARReplayPlaneSubsystem.ID);

            // Optional ARCore extensions
#if ARCORE_EXTENSIONS_ENABLED
            CreateSubsystem<XRGeospatialEarthSubsystemDescriptor, XRGeospatialEarthSubsystem>(new(), ARReplayGeospatialEarthSubsystem.ID);
            CreateSubsystem<XRStreetscapeGeometrySubsystemDescriptor, XRStreetscapeGeometrySubsystem>(new(), ARReplayStreetscapeGeometrySubsystem.ID);
#endif // ARCORE_EXTENSIONS_ENABLED

            var sessionSubsystem = GetLoadedSubsystem<XRSessionSubsystem>();
            if (sessionSubsystem == null)
            {
                Debug.LogError("Failed to load session subsystem.");
            }
            return sessionSubsystem != null;
        }

        public override bool Start()
        {
            return true;
        }

        public override bool Stop()
        {
            return true;
        }

        public override bool Deinitialize()
        {
            // Is order sensitive?
#if ARCORE_EXTENSIONS_ENABLED
            DestroySubsystem<XRStreetscapeGeometrySubsystem>();
            DestroySubsystem<XRGeospatialEarthSubsystem>();
#endif // ARCORE_EXTENSIONS_ENABLED

            DestroySubsystem<XRPlaneSubsystem>();
            DestroySubsystem<XROcclusionSubsystem>();

            DestroySubsystem<XRInputSubsystem>();
            DestroySubsystem<XRCameraSubsystem>();
            DestroySubsystem<XRSessionSubsystem>();

            return base.Deinitialize();
        }

        public bool EnsureSystemStarted<T>()
            where T : class, ISubsystem
        {
            var system = GetLoadedSubsystem<T>();
            if (system != null && !system.running)
            {
                system.Start();
                return true;
            }
            return false;
        }

        public static bool TryGetLoader(out ARFoundationReplayLoader loader)
        {
            if (XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null)
            {
                var loaders = XRGeneralSettings.Instance.Manager.activeLoaders;
                if (loaders == null)
                {
                    loader = null;
                    return false;
                }
                foreach (var activeLoader in loaders)
                {
                    if (activeLoader is ARFoundationReplayLoader)
                    {
                        loader = activeLoader as ARFoundationReplayLoader;
                        return true;
                    }
                }
            }
            loader = null;
            return false;
        }
    }
}
