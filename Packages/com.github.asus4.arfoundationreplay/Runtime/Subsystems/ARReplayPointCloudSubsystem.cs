using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace ARFoundationReplay
{
    internal sealed class ARReplayPointCloudSubsystem : XRPointCloudSubsystem
    {
        public const string ID = "ARReplay-PointCloud";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterDescriptor()
        {
            var cinfo = new XRPointCloudSubsystemDescriptor.Cinfo
            {
                id = ID,
                providerType = typeof(ARReplayProvider),
                subsystemTypeOverride = typeof(ARReplayPointCloudSubsystem),
                supportsFeaturePoints = true,
                supportsConfidence = true,
                supportsUniqueIds = true,
            };
            XRPointCloudSubsystemDescriptor.Register(cinfo);
        }

        class ARReplayProvider : Provider
        {
            private PointCloudPacket _currentPacket;

            private readonly HashSet<TrackableId> _activeIds = new();
            private readonly List<XRPointCloud> _added = new();
            private readonly List<XRPointCloud> _updated = new();
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

            public override TrackableChanges<XRPointCloud> GetChanges(XRPointCloud defaultPointCloud, Allocator allocator)
            {
                if (!ARReplay.TryGetReplay(out var replay))
                {
                    return default;
                }

                _currentPacket = replay.Metadata.pointCloud;
                if (_currentPacket == null || !_currentPacket.IsAvailable)
                {
                    return default;
                }

                _currentPacket.CorrectTrackable(_activeIds, _added, _updated, _removed);
                return _currentPacket.AsTrackableChanges(allocator);
            }

            public override XRPointCloudData GetPointCloudData(TrackableId trackableId, Allocator allocator)
            {
                if (_currentPacket == null || !_currentPacket.IsAvailable)
                {
                    return default;
                }

                if (_currentPacket.data.TryGetValue(trackableId, out var pointCloudData))
                {
                    return pointCloudData.ToXRPointCloudData(allocator);
                }

                return default;
            }
        }
    }
}
