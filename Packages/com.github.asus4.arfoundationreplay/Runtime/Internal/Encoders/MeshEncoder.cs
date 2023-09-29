using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.XR.CoreUtils;
using UnityEngine.XR;
using Unity.Mathematics;

namespace ARFoundationReplay
{
    using NativeTrackableId = UnityEngine.XR.ARSubsystems.TrackableId;

    internal struct TrackableMesh : ITrackable
    {
        public TrackableId id;
        public Pose poseMesh;
        public Vector3 scale;

        public NativeTrackableId trackableId => id;
        public UnityEngine.Pose pose { get; set; }
        public TrackingState trackingState { get; set; }
        public IntPtr nativePtr { get; set; }

    }


    /// <summary>
    /// Serializable version of each frame of ARMeshes.
    /// </summary>
    [Serializable]
    internal class MeshPacket : TrackableChangesPacket<BoundedPlane>
    {
        public MeshId id;
    }

    /// <summary>
    /// Encodes mesh from ARMeshManager into Packet.
    /// </summary>
    internal sealed class MeshEncoder : ISubsystemEncoder
    {
        private ARMeshManager _meshManager;
        private readonly MeshPacket _packet = new();

        public bool Initialize(XROrigin origin, Material muxMaterial)
        {
            _meshManager = origin.GetComponentInChildren<ARMeshManager>();
            if (_meshManager == null)
            {
                Debug.LogError("ARMeshManager is not found");
                return false;
            }
            _meshManager.meshesChanged += OnMeshesChanged;
            return true;
        }

        public void Dispose()
        {
            _meshManager.meshesChanged -= OnMeshesChanged;
            _meshManager = null;
        }

        public void Encode(FrameMetadata metadata)
        {
            // metadata.mesh = _packet.IsAvailable ? _packet : null;
        }

        public void PostEncode(FrameMetadata metadata)
        {
            _packet.Reset();
        }

        private void OnMeshesChanged(ARMeshesChangedEventArgs args)
        {
            Debug.Log($"OnMeshesChanged added={args.added.Count} updated={args.updated.Count} removed={args.removed.Count}");
            _packet.Reset();

            // _packet.Added = args.added;
            // _packet.Updated = args.updated;
            // _packet.Removed = args.removed;
        }
    }
}
