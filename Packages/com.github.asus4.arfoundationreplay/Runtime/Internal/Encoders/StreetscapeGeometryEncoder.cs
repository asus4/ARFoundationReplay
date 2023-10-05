#if ARCORE_EXTENSIONS_ENABLED
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;
using Google.XR.ARCoreExtensions;
using System.Collections.Generic;

namespace ARFoundationReplay
{
    using NativeTrackableId = UnityEngine.XR.ARSubsystems.TrackableId;
    using NativePose = UnityEngine.Pose;

    internal struct StreetscapeGeometry : ITrackable
    {
        private NativeTrackableId _trackableId;
        public NativeTrackableId trackableId
        {
            readonly get => _trackableId;
            set => _trackableId = value;
        }

        private NativePose _pose;
        public NativePose pose
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
        public readonly IntPtr nativePtr => IntPtr.Zero;

        public StreetscapeGeometryType streetscapeGeometryType;
        public StreetscapeGeometryQuality quality;
    }

    [Serializable]
    internal class StreetscapeGeometryPacket : TrackableChangesPacket<StreetscapeGeometry>
    {
        public Dictionary<TrackableId, byte[]> meshes = new();

        public override void Reset()
        {
            base.Reset();
            meshes.Clear();
        }

        public byte[] ToByteArray()
        {
            return null;
        }
    }

    internal class StreetscapeGeometryEncoder : ISubsystemEncoder
    {
        private ARStreetscapeGeometryManager _geometryManager;
        private readonly StreetscapeGeometryPacket _packet = new();

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

        public void Encode(FrameMetadata metadata)
        {
            TrackID id = TrackID.ARCoreStreetscapeGeometry;
            if (_packet.IsAvailable)
            {
                metadata.tracks[id] = _packet.ToByteArray();
            }
            else
            {
                metadata.tracks.Remove(id);
            }
        }

        public void PostEncode(FrameMetadata metadata)
        {
            _packet.Reset();
        }

        private void OnGeometriesChanged(ARStreetscapeGeometriesChangedEventArgs args)
        {

        }
    }
}
#endif // ARCORE_EXTENSIONS_ENABLED
