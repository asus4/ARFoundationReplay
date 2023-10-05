using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;

namespace ARFoundationReplay
{
    using NativeTrackableId = UnityEngine.XR.ARSubsystems.TrackableId;

    [Preserve]
    internal sealed class ARReplayPlaneSubsystem : XRPlaneSubsystem
    {
        public const string ID = "ARReplay-Plane";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterDescriptor()
        {
            Debug.Log($"Register {ID} subsystem");

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

            XRPlaneSubsystemDescriptor.Create(cinfo);
        }

        class ARReplayProvider : Provider
        {
            private PlanePacket _currentPacket;
            private PlaneDetectionMode _requestedPlaneDetectionMode;

            private readonly HashSet<NativeTrackableId> _activeIds = new();
            private readonly List<BoundedPlane> _added = new();
            private readonly List<BoundedPlane> _updated = new();
            private readonly List<NativeTrackableId> _removed = new();


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
                NativeTrackableId trackableId, Allocator allocator, ref NativeArray<Vector2> boundary)
            {
                if (_currentPacket == null)
                {
                    return;
                }
                _currentPacket.GetBoundary(trackableId, allocator, ref boundary);
            }

            public override unsafe TrackableChanges<BoundedPlane> GetChanges(
                BoundedPlane defaultPlane,
                Allocator allocator)
            {

                if (!ARReplay.TryGetReplay(out var replay))
                {
                    return default;
                }

                if (!replay.Metadata.TryGetTrack(TrackID.Plane, out _currentPacket))
                {
                    return default;
                }
                if (_currentPacket == null)
                {
                    return default;
                }
                if (!_currentPacket.IsAvailable)
                {
                    return default;
                }

                // Need to correct inconsistencies of tracked IDs
                // since the recording will start in the middle of the session,
                // and the video is looped.
                CorrectTrackable(_currentPacket);

                return _currentPacket.AsTrackableChanges(allocator);
            }

            private void CorrectTrackable(PlanePacket packet)
            {
                _added.Clear();
                _updated.Clear();
                _removed.Clear();
                using var rawChanges = packet.AsTrackableChanges(Allocator.Temp);
                // Added
                for (int i = 0; i < rawChanges.added.Length; i++)
                {
                    BoundedPlane plane = rawChanges.added[i];
                    if (!_activeIds.Contains(plane.trackableId))
                    {
                        _activeIds.Add(plane.trackableId);
                        _added.Add(plane);
                    }
                }
                // Updated
                for (int i = 0; i < rawChanges.updated.Length; i++)
                {
                    BoundedPlane plane = rawChanges.updated[i];
                    if (_activeIds.Contains(plane.trackableId))
                    {
                        _updated.Add(plane);
                    }
                    else
                    {
                        _activeIds.Add(plane.trackableId);
                        _added.Add(plane);
                    }
                }
                // Removed
                for (int i = 0; i < rawChanges.removed.Length; i++)
                {
                    NativeTrackableId id = rawChanges.removed[i];
                    if (_activeIds.Contains(id))
                    {
                        _activeIds.Remove(id);
                        _removed.Add(id);
                    }
                }
                packet.CopyFrom(_added, _updated, _removed);
            }
        }
    }
}
