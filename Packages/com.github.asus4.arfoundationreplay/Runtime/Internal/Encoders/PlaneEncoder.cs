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
    /// <summary>
    /// Serializable version of each frame of ARPlanes.
    /// </summary>
    [MemoryPackable]
    internal partial class PlanePacket : TrackableChangesPacket<BoundedPlane>
    {
        public PlaneDetectionMode currentDetectionMode;
        public Dictionary<TrackableId, byte[]> boundaries; // NativeArray<Vector2>

        public PlanePacket() : base()
        {
            boundaries = new();
        }

        public override void Reset()
        {
            base.Reset();
            boundaries.Clear();
        }

    }

    /// <summary>
    /// Encodes planes from ARPlaneManager into Packet.
    /// </summary>
    internal sealed class PlaneEncoder : ISubsystemEncoder
    {
        private ARPlaneManager _planeManager;
        private readonly PlanePacket _packet = new();

        public bool Initialize(XROrigin origin, Material muxMaterial)
        {
            _planeManager = origin.GetComponentInChildren<ARPlaneManager>();
            if (_planeManager == null)
            {
                return false;
            }
            _planeManager.trackablesChanged.AddListener(OnTrackablesChanged);
            return true;
        }

        public void Dispose()
        {
            if (_planeManager != null)
            {
                _planeManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
                _planeManager = null;
            }
        }

        public void Encode(FrameMetadata metadata)
        {
            metadata.plane = _packet.IsAvailable ? _packet : null;
        }

        public void PostEncode()
        {
            _packet.Reset();
        }

        private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            if (_packet.IsAvailable)
            {
                _packet.Reset();
            }

            _packet.currentDetectionMode = _planeManager.currentDetectionMode;

            // Convert ARPlanes into TrackableChanges:
            using var changes = new TrackableChanges<BoundedPlane>(
                args.added.Count, args.updated.Count, args.removed.Count, Allocator.Temp);

            var dstAdded = changes.added;
            var dstUpdated = changes.updated;
            var dstRemoved = changes.removed;

            var dstBoundaries = _packet.boundaries;

            for (int i = 0; i < args.added.Count; i++)
            {
                var arPlane = args.added[i];
                dstAdded[i] = ConvertToBoundedPlane(arPlane);
                dstBoundaries.Add(arPlane.trackableId, arPlane.boundary.ToByteArray());
            }
            for (int i = 0; i < args.updated.Count; i++)
            {
                var arPlane = args.updated[i];
                dstUpdated[i] = ConvertToBoundedPlane(arPlane);
                dstBoundaries.Add(arPlane.trackableId, arPlane.boundary.ToByteArray());
            }
            for (int i = 0; i < args.removed.Count; i++)
            {
                dstRemoved[i] = args.removed[i].Key;
            }
            // Serialize TrackableChanges into Packet:
            _packet.CopyFrom(changes);

            // Debug.Log($"Planes changed: added={args.added.Count}, updated={args.updated.Count}, removed={args.removed.Count}");
        }

        private static BoundedPlane ConvertToBoundedPlane(ARPlane plane)
        {
            return new BoundedPlane(
                trackableId: plane.trackableId,
                subsumedBy: plane.subsumedBy != null
                    ? plane.subsumedBy.trackableId
                    : TrackableId.invalidId,
                pose: plane.transform.GetLocalPose(),
                center: plane.centerInPlaneSpace,
                size: plane.size,
                alignment: plane.alignment,
                trackingState: plane.trackingState,
                nativePtr: IntPtr.Zero,
                classifications: plane.classifications
            );
        }
    }
}
