#if ARCORE_EXTENSIONS_ENABLED
using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting;
using UnityEngine.SubsystemsImplementation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections.LowLevel.Unsafe;
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

    // Only available when ARCore Extensions package is installed.
    [Preserve]
    public sealed class ARReplayGeospatialEarthSubsystem : XRGeospatialEarthSubsystem
    {
        public const string ID = "ARReplay-GeospatialEarth";

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
            private static readonly int PacketSize = UnsafeUtility.SizeOf<GeospatialEarthPacket>();

            private int updatedFrameCount = -1;
            private GeospatialEarthPacket latest = new()
            {
                earthState = EarthState.ErrorEarthNotReady,
                trackingState = TrackingState.None,
                geospatialPose = new GeospatialPose(),
            };

            public override void Start()
            {
            }

            public override void Stop()
            {
            }

            public override void Destroy()
            {
            }

            public override EarthState EarthState
            {
                get
                {
                    UpdatePacket();
                    return latest.earthState;
                }
            }

            public override TrackingState EarthTrackingState
            {
                get
                {
                    UpdatePacket();
                    return latest.trackingState;
                }
            }

            public override GeospatialPose CameraGeospatialPose
            {
                get
                {
                    UpdatePacket();
                    return latest.geospatialPose;
                }
            }

            private GeospatialEarthPacket UpdatePacket()
            {
                if (updatedFrameCount == Time.frameCount)
                {
                    // Updated already at this frame.
                    return latest;
                }

                if (!ARReplay.TryGetReplay(out var replay))
                {
                    // Assuming not ready
                    return latest;
                }
                if (!replay.DidUpdateThisFrame)
                {
                    return latest;
                }

                var metadata = replay.Metadata;

                updatedFrameCount = Time.frameCount;

                if (metadata.TryGetTrack(TrackID.ARCoreGeospatialEarth, out byte[] bytes))
                {
                    if (bytes == null)
                    {
                        Debug.LogWarning($"Unexpected null bytes");
                        return latest;
                    }
                    Assert.AreEqual(PacketSize, bytes.Length);
                    latest = bytes.ToStruct<GeospatialEarthPacket>();
                }
                return latest;
            }
        }
    }
}
#endif // ARCORE_EXTENSIONS_ENABLED
