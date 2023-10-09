#if ARCORE_EXTENSIONS_ENABLED
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections.LowLevel.Unsafe;
using Google.XR.ARCoreExtensions;

namespace ARFoundationReplay
{
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
            Register(info);
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
                if (metadata.TryGetByteStruct(TrackID.ARCoreGeospatialEarth, out GeospatialEarthPacket earthPacket))
                {
                    latest = earthPacket;
                }
                return latest;
            }
        }
    }
}
#endif // ARCORE_EXTENSIONS_ENABLED
