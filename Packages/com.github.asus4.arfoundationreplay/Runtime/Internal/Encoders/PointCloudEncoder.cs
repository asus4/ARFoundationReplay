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
        public Dictionary<TrackableId, PointCloudData> data;

        public PointCloudPacket() : base()
        {
            data = new();
        }

        public override void Reset()
        {
            base.Reset();
            data.Clear();
        }

        public override bool IsAvailable
        {
            get
            {
                if (data == null)
                {
                    return false;
                }
                return base.IsAvailable || data.Count > 0;
            }
        }

        public void MergeEarlier(PointCloudPacket earlier)
        {
            MergeEarlierChanges(earlier);
            foreach (var (id, pointCloudData) in earlier.data)
            {
                data.TryAdd(id, pointCloudData);
            }
        }
    }

    internal sealed class PointCloudEncoder : ISubsystemEncoder
    {
        private ARPointCloudManager _pointCloudManager;
        private readonly PointCloudPacket _packet = new();
        private readonly TrackableChangesAccumulator<XRPointCloud> _changes = new();

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
            if (_changes.IsEmpty)
            {
                metadata.pointCloud = null;
                return;
            }
            _changes.CopyTo(_packet);
            metadata.pointCloud = _packet;
        }

        public void PostEncode()
        {
            _changes.Clear();
            _packet.Reset();
        }

        private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARPointCloud> args)
        {
            var data = _packet.data;

            for (int i = 0; i < args.added.Count; i++)
            {
                var arPointCloud = args.added[i];
                _changes.Add(ToXRPointCloud(arPointCloud));
                data[arPointCloud.trackableId] = ToPointCloudData(arPointCloud);
            }
            for (int i = 0; i < args.updated.Count; i++)
            {
                var arPointCloud = args.updated[i];
                _changes.Update(ToXRPointCloud(arPointCloud));
                data[arPointCloud.trackableId] = ToPointCloudData(arPointCloud);
            }
            for (int i = 0; i < args.removed.Count; i++)
            {
                var id = args.removed[i].Key;
                _changes.Remove(id);
                data.Remove(id);
            }

            // Debug.Log($"Point Cloud changed: added={args.added.Count}, updated={args.updated.Count}, removed={args.removed.Count}");
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
