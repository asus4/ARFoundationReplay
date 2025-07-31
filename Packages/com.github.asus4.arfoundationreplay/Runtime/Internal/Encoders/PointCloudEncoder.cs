using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using Unity.XR.CoreUtils;
using MemoryPack;

namespace ARFoundationReplay
{
    [MemoryPackable]
    internal partial class PointCloudData
    {
        public byte[] positions; // NativeArray<Vector3>
        public float[] confidenceValues; // NativeArray<float>
        public ulong[] identifiers; // NativeArray<ulong>

        public XRPointCloudData ToXRPointCloudData(Allocator allocator)
        {
            return new XRPointCloudData()
            {
                positions = positions.AsNativeArray<Vector3>(allocator),
                confidenceValues = new(confidenceValues, allocator),
                identifiers = new(identifiers, allocator)
            };
        }
    }

    [MemoryPackable]
    internal partial class PointCloudPacket : TrackableChangesPacket<XRPointCloud>
    {
        public Dictionary<TrackableId, PointCloudData> data; // NativeArray<ulong>

        public PointCloudPacket() : base()
        {
            data = new();
        }

        public override void Reset()
        {
            base.Reset();
            data.Clear();
        }

        public override bool IsAvailable => base.IsAvailable || data.Count > 0;
    }

    internal sealed class PointCloudEncoder : ISubsystemEncoder
    {
        private ARPointCloudManager _pointCloudManager;
        private readonly PointCloudPacket _packet = new();

        public bool Initialize(XROrigin origin, Material muxMaterial)
        {
            _pointCloudManager = origin.GetComponentInChildren<ARPointCloudManager>();
            if (_pointCloudManager == null)
            {
                return false;
            }
            _pointCloudManager.trackablesChanged.AddListener(OnTrackablesChanged);
            return true;
        }

        public void Dispose()
        {
            if (_pointCloudManager != null)
            {
                _pointCloudManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
                _pointCloudManager = null;
            }
            _packet.Reset();
        }

        public void Encode(FrameMetadata metadata)
        {
            metadata.pointCloud = _packet.IsAvailable ? _packet : null;
        }

        public void PostEncode()
        {
            _packet.Reset();
        }

        private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARPointCloud> args)
        {
            if (_packet.IsAvailable)
            {
                _packet.Reset();
            }

            using var changes = new TrackableChanges<XRPointCloud>(
                args.added.Count, args.updated.Count, args.removed.Count, Allocator.Temp);

            var dstAdded = changes.added;
            var dstUpdated = changes.updated;
            var dstRemoved = changes.removed;

            var data = _packet.data;

            for (int i = 0; i < args.added.Count; i++)
            {
                var arPointCloud = args.added[i];
                dstAdded[i] = ToXRPointCloud(arPointCloud);
                data[arPointCloud.trackableId] = ToPointCloudData(arPointCloud);
            }
            for (int i = 0; i < args.updated.Count; i++)
            {
                var arPointCloud = args.updated[i];
                dstUpdated[i] = ToXRPointCloud(arPointCloud);
                data[arPointCloud.trackableId] = ToPointCloudData(arPointCloud);
            }
            for (int i = 0; i < args.removed.Count; i++)
            {
                dstRemoved[i] = args.removed[i].Key;
            }
            // Serialize TrackableChanges into Packet:
            _packet.CopyFrom(changes);

            Debug.Log($"Point Cloud changed: added={args.added.Count}, updated={args.updated.Count}, removed={args.removed.Count}");
        }

        private static XRPointCloud ToXRPointCloud(ARPointCloud arPointCloud)
        {
            return new XRPointCloud(
                arPointCloud.trackableId,
                arPointCloud.pose,
                arPointCloud.trackingState,
                IntPtr.Zero);
        }

        private static PointCloudData ToPointCloudData(ARPointCloud arPointCloud)
        {
            return new PointCloudData()
            {
                positions = arPointCloud.positions.HasValue
                    ? arPointCloud.positions.Value.ToByteArray()
                    : Array.Empty<byte>(),
                confidenceValues = arPointCloud.confidenceValues.HasValue
                    ? arPointCloud.confidenceValues.Value.ToArray()
                    : Array.Empty<float>(),
                identifiers = arPointCloud.identifiers.HasValue
                    ? arPointCloud.identifiers.Value.ToArray()
                    : Array.Empty<ulong>()
            };
        }
    }
}
