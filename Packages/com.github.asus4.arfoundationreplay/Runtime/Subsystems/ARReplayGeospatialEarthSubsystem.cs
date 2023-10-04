#if ARCORE_EXTENSIONS_ENABLED
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Google.XR.ARCoreExtensions;
using UnityEngine.SubsystemsImplementation;
using UnityEngine.XR.ARSubsystems;
using System;
using UnityEngine.Scripting;

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
        public static bool Register(XRGeospatialEarthSubsystemSubsystemCinfo info)
        {
            var descriptor = new XRGeospatialEarthSubsystemDescriptor(info);
            SubsystemDescriptorStore.RegisterDescriptor(descriptor);
            return true;
        }

        public abstract class Provider : SubsystemProvider<XRGeospatialEarthSubsystem>
        {
            public abstract bool TryGetEarthState(out EarthState earthState);
            public abstract bool TryGetEarthTrackingState(out TrackingState trackingState);
            public abstract bool TryGetCameraGeospatialPose(out GeospatialPose cameraGeospatialPose);
        }
    }

    // Only available when ARCore Extensions package is installed.
    [Preserve]
    public sealed class ARReplayGeospatialEarthSubsystem : XRGeospatialEarthSubsystem
    {
        public const string ID = "ARReplay-GeospatialEarth";
        public static readonly int IDKey = Shader.PropertyToID(ID);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            var info = new XRGeospatialEarthSubsystemSubsystemCinfo()
            {
                id = ID,
                providerType = typeof(ARReplayProvider),
                subsystemTypeOverride = typeof(ARReplayGeospatialEarthSubsystem),
            };
            if (Register(info))
            {
                Debug.Log($"Register {ID} subsystem");
            }
            else
            {
                Debug.LogError($"Cannot register {ID} subsystem");
            }
        }

        class ARReplayProvider : Provider
        {

            public override void Start()
            {

            }

            public override void Stop()
            {

            }

            public override void Destroy()
            {

            }

            public override bool TryGetEarthState(out EarthState earthState)
            {
                earthState = EarthState.Enabled;
                return false;
            }

            public override bool TryGetEarthTrackingState(out TrackingState trackingState)
            {
                trackingState = TrackingState.None;
                return false;
            }

            public override bool TryGetCameraGeospatialPose(out GeospatialPose cameraGeospatialPose)
            {
                cameraGeospatialPose = default;
                return false;
            }
        }
    }
}
#endif // ARCORE_EXTENSIONS_ENABLED
