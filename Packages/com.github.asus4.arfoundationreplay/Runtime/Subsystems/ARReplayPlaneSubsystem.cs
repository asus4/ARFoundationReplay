using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;

namespace ARFoundationReplay
{
    [Preserve]
    internal sealed class ARReplayPlaneSubsystem : XRPlaneSubsystem
    {
        public const string ID = "ARReplay-Plane";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterDescriptor()
        {
            var cinfo = new XRPlaneSubsystemDescriptor.Cinfo
            {
                id = ID,
                providerType = typeof(ARReplayProvider),
                subsystemTypeOverride = typeof(ARReplayPlaneSubsystem),
                supportsHorizontalPlaneDetection = true,
                supportsVerticalPlaneDetection = true,
                supportsArbitraryPlaneDetection = false,
                supportsBoundaryVertices = true,
                supportsClassification = true,
            };
            XRPlaneSubsystemDescriptor.Register(cinfo);
        }

        class ARReplayProvider : Provider
        {
            private PlanePacket _currentPacket;
            private PlaneDetectionMode _requestedPlaneDetectionMode;

            private readonly HashSet<TrackableId> _activeIds = new();
            private readonly List<BoundedPlane> _added = new();
            private readonly List<BoundedPlane> _updated = new();
            private readonly List<TrackableId> _removed = new();

            public override void Start() { }

            public override void Stop()
            {
                _activeIds.Clear();
                _added.Clear();
                _updated.Clear();
                _removed.Clear();
                _currentPacket = null;
            }

            public override void Destroy() { }

            public override PlaneDetectionMode currentPlaneDetectionMode
            {
                get
                {
                    if (requestedPlaneDetectionMode != PlaneDetectionMode.None)
                    {
                        return requestedPlaneDetectionMode;
                    }
                    return _currentPacket?.currentDetectionMode ?? requestedPlaneDetectionMode;
                }
            }

            public override PlaneDetectionMode requestedPlaneDetectionMode
            {
                get => _requestedPlaneDetectionMode;
                set => _requestedPlaneDetectionMode = value;
            }

            public override unsafe void GetBoundary(
                TrackableId trackableId, Allocator allocator, ref NativeArray<Vector2> boundary)
            {
                if (_currentPacket == null)
                {
                    return;
                }

                if (_currentPacket.boundaries.TryGetValue(trackableId, out var bytes))
                {
                    int length = bytes.Length / UnsafeUtility.SizeOf<Vector2>();
                    CreateOrResizeNativeArrayIfNecessary(length, allocator, ref boundary);
                    fixed (byte* bytesPtr = bytes)
                    {
                        UnsafeUtility.MemCpy(boundary.GetUnsafePtr(), bytesPtr, bytes.Length);
                    }
                }
                else
                {
                    CreateOrResizeNativeArrayIfNecessary(0, allocator, ref boundary);
                }
            }

            public override unsafe TrackableChanges<BoundedPlane> GetChanges(
                BoundedPlane defaultPlane,
                Allocator allocator)
            {
                if (!ARReplay.TryGetReplay(out var replay))
                {
                    return default;
                }

                _currentPacket = replay.Metadata.plane;
                if (_currentPacket == null || !_currentPacket.IsAvailable)
                {
                    return default;
                }


                if (_currentPacket.currentDetectionMode == requestedPlaneDetectionMode)
                {
                    _currentPacket.CorrectTrackable(_activeIds, _added, _updated, _removed);
                }
                else
                {
                    // Filter out trackable
                    _currentPacket.CorrectTrackable(_activeIds, _added, _updated, _removed,
                        TrackableFilter);
                }

                return _currentPacket.AsTrackableChanges(allocator);
            }

            private bool TrackableFilter(ref BoundedPlane plane)
            {
                return plane.alignment switch
                {
                    PlaneAlignment.HorizontalUp or PlaneAlignment.HorizontalDown
                        => requestedPlaneDetectionMode.HasFlag(PlaneDetectionMode.Horizontal),
                    PlaneAlignment.Vertical
                        => requestedPlaneDetectionMode.HasFlag(PlaneDetectionMode.Vertical),
                    _ => true,
                };
            }

        }
    }
}
