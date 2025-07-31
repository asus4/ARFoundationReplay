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
            private readonly TrackableChangesPacketModifier<XRPointCloud> _modifier = new();

            public override void Start() { }

            public override void Stop()
            {
                _modifier.Dispose();
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

                _currentPacket.CorrectTrackable(_modifier);
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
