#if ARCORE_EXTENSIONS_ENABLED
using System;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;
using Unity.Collections;
using Google.XR.ARCoreExtensions;
using System.Collections.Generic;

namespace ARFoundationReplay
{
    using NativeTrackableId = UnityEngine.XR.ARSubsystems.TrackableId;

    public struct StreetscapeGeometry : ITrackable
    {
        private NativeTrackableId _trackableId;
        public NativeTrackableId trackableId
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

    [Serializable]
    internal sealed class StreetscapeGeometryPacket : TrackableChangesPacket<StreetscapeGeometry>
    {
        public Dictionary<TrackableId, byte[]> meshes = new();

        public override void Reset()
        {
            base.Reset();
            meshes.Clear();
        }
    }

    internal sealed class StreetscapeGeometryEncoder : ISubsystemEncoder
    {
        private ARStreetscapeGeometryManager _geometryManager;
        private readonly StreetscapeGeometryPacket _packet = new();

        public TrackID ID => TrackID.ARCoreStreetscapeGeometry;

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
            if (_geometryManager != null)
            {
                _geometryManager.StreetscapeGeometriesChanged -= OnGeometriesChanged;
                _geometryManager = null;
            }
        }

        public bool TryEncode(out object data)
        {
            if (_packet.IsAvailable)
            {
                data = _packet;
                return true;
            }
            else
            {
                data = null;
                return false;
            }
        }

        public void PostEncode()
        {
            _packet.Reset();
        }

        private void OnGeometriesChanged(ARStreetscapeGeometriesChangedEventArgs args)
        {
            if (_packet.IsAvailable)
            {
                _packet.Reset();
            }

            using var changes = new TrackableChanges<StreetscapeGeometry>(
                args.Added.Count, args.Updated.Count, args.Removed.Count, Allocator.Temp);

            var dstAdded = changes.added;
            var dstUpdated = changes.updated;
            var dstRemoved = changes.removed;

            var meshes = _packet.meshes;

            for (int i = 0; i < args.Added.Count; i++)
            {
                var geometry = args.Added[i];
                dstAdded[i] = ConvertToSerializable(geometry);
                if (!meshes.ContainsKey(geometry.trackableId))
                {
                    meshes.Add(geometry.trackableId, geometry.mesh.ToByteArray());
                }
            }
            for (int i = 0; i < args.Updated.Count; i++)
            {
                var geometry = args.Updated[i];
                dstUpdated[i] = ConvertToSerializable(geometry);
                if (!meshes.ContainsKey(geometry.trackableId))
                {
                    meshes.Add(geometry.trackableId, geometry.mesh.ToByteArray());
                }
            }
            for (int i = 0; i < args.Removed.Count; i++)
            {
                dstRemoved[i] = args.Removed[i].trackableId;
            }

            // Serialize TrackableChanges into Packet:
            _packet.CopyFrom(changes);

            // Debug.Log($"StreetscapeGeometryEncoder: added: {changes.added.Length}, updated: {changes.updated.Length}, removed: {changes.removed.Length}");
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
