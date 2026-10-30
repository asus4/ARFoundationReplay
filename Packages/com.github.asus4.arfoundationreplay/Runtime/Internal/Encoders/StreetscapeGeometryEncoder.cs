#if ARCORE_EXTENSIONS_ENABLED
using System;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;
using Google.XR.ARCoreExtensions;
using System.Collections.Generic;
using MemoryPack;

namespace ARFoundationReplay
{
    public struct StreetscapeGeometry : ITrackable
    {
        private TrackableId _trackableId;
        public TrackableId trackableId
        {
            readonly get => _trackableId;
            set => _trackableId = value;
        }

        private Pose _pose;
        public Pose pose
        {
            readonly get => _pose;
            set => _pose = value;
        }

        private TrackingState _trackingState;
        public TrackingState trackingState
        {
            readonly get => _trackingState;
            set => _trackingState = value;
        }
        public readonly IntPtr nativePtr => new((long)_trackableId.subId2);

        public StreetscapeGeometryType streetscapeGeometryType;
        public StreetscapeGeometryQuality quality;
    }

    [MemoryPackable]
    internal sealed partial class StreetscapeGeometryPacket : TrackableChangesPacket<StreetscapeGeometry>
    {
        public Dictionary<TrackableId, Mesh> meshes = new();

        public override void Reset()
        {
            base.Reset();
            meshes.Clear();
        }

        public void MergeEarlier(StreetscapeGeometryPacket earlier)
        {
            MergeEarlierChanges(earlier);
            foreach (var (id, mesh) in earlier.meshes)
            {
                meshes.TryAdd(id, mesh);
            }
        }
    }

    internal sealed class StreetscapeGeometryEncoder : ISubsystemEncoder
    {
        private ARStreetscapeGeometryManager _geometryManager;
        private readonly StreetscapeGeometryPacket _packet = new();
        private readonly TrackableChangesAccumulator<StreetscapeGeometry> _changes = new();
        private readonly HashSet<TrackableId> _sentMeshes = new();

        public bool Initialize(XROrigin origin, Material muxMaterial)
        {
            _geometryManager = origin.GetComponentInChildren<ARStreetscapeGeometryManager>();
            if (_geometryManager == null)
            {
                return false;
            }
            _geometryManager.StreetscapeGeometriesChanged += OnGeometriesChanged;
            return true;
        }

        public void Dispose()
        {
            _sentMeshes.Clear();

            if (_geometryManager != null)
            {
                _geometryManager.StreetscapeGeometriesChanged -= OnGeometriesChanged;
                _geometryManager = null;
            }
        }

        public void Encode(FrameMetadata metadata)
        {
            if (_changes.IsEmpty)
            {
                metadata.streetscapeGeometry = null;
                return;
            }
            _changes.CopyTo(_packet);
            metadata.streetscapeGeometry = _packet;
        }

        public void PostEncode()
        {
            _changes.Clear();
            _packet.Reset();
        }

        private void OnGeometriesChanged(ARStreetscapeGeometriesChangedEventArgs args)
        {
            for (int i = 0; i < args.Added.Count; i++)
            {
                var geometry = args.Added[i];
                _changes.Add(ConvertToSerializable(geometry));
                AddMeshIfNeeded(geometry);
            }
            for (int i = 0; i < args.Updated.Count; i++)
            {
                var geometry = args.Updated[i];
                _changes.Update(ConvertToSerializable(geometry));
                AddMeshIfNeeded(geometry);
            }
            for (int i = 0; i < args.Removed.Count; i++)
            {
                var id = args.Removed[i].trackableId;
                _changes.Remove(id);
                // Sent again if the same ID comes back
                _packet.meshes.Remove(id);
                _sentMeshes.Remove(id);
            }

            // Debug.Log($"StreetscapeGeometryEncoder: added: {changes.added.Length}, updated: {changes.updated.Length}, removed: {changes.removed.Length}");
        }

        private void AddMeshIfNeeded(ARStreetscapeGeometry arGeometry)
        {
            if (_sentMeshes.Contains(arGeometry.trackableId))
            {
                return;
            }
            var meshes = _packet.meshes;
            if (meshes.ContainsKey(arGeometry.trackableId))
            {
                return;
            }
            meshes.Add(arGeometry.trackableId, arGeometry.mesh);
            _sentMeshes.Add(arGeometry.trackableId);
        }

        private StreetscapeGeometry ConvertToSerializable(ARStreetscapeGeometry geometry)
        {
            return new StreetscapeGeometry
            {
                trackableId = geometry.trackableId,
                pose = geometry.pose,
                trackingState = geometry.trackingState,
                streetscapeGeometryType = geometry.streetscapeGeometryType,
                quality = geometry.quality
            };
        }
    }
}
#endif // ARCORE_EXTENSIONS_ENABLED
