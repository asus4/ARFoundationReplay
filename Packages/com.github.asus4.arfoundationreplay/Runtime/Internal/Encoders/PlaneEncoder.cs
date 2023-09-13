using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using Unity.XR.CoreUtils;


namespace ARFoundationReplay
{
    [Serializable]
    public class PlanePacket : TrackableChangesPacket<BoundedPlane>
    {
    }

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
            _planeManager.planesChanged += OnPlanesChanged;
            return true;
        }

        public void Dispose()
        {
            _planeManager = null;
        }

        public void Encode(FrameMetadata metadata)
        {
            metadata.plane = _packet.IsAvailable ? _packet : null;
        }

        private void OnPlanesChanged(ARPlanesChangedEventArgs args)
        {
            // Convert ARPlanes into TrackableChanges:
            using var changes = new TrackableChanges<BoundedPlane>(
                args.added.Count, args.updated.Count, args.removed.Count, Allocator.Temp);
            var dstAdded = changes.added;
            var dstUpdated = changes.updated;
            var dstRemoved = changes.removed;

            for (int i = 0; i < args.added.Count; i++)
            {
                dstAdded[i] = ConvertToBoundedPlane(args.added[i]);
            }
            for (int i = 0; i < args.updated.Count; i++)
            {
                dstUpdated[i] = ConvertToBoundedPlane(args.updated[i]);
            }
            for (int i = 0; i < args.removed.Count; i++)
            {
                dstRemoved[i] = args.removed[i].trackableId;
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
                    : NativeTrackableId.invalidId,
                pose: plane.transform.GetLocalPose(),
                center: plane.centerInPlaneSpace,
                size: plane.size,
                alignment: plane.alignment,
                trackingState: plane.trackingState,
                nativePtr: IntPtr.Zero,
                classification: plane.classification
            );
        }
    }
}
