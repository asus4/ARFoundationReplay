#if ARCORE_EXTENSIONS_ENABLED
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using Unity.Collections;

namespace ARFoundationReplay
{
    using NativeTrackableId = UnityEngine.XR.ARSubsystems.TrackableId;

    [Preserve]
    public sealed class ARReplayStreetscapeGeometrySubsystem : XRStreetscapeGeometrySubsystem
    {
        public const string ID = "ARReplay-StreetscapeGeometry";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            var info = new XRStreetscapeGeometrySubsystemSubsystemCinfo()
            {
                id = ID,
                providerType = typeof(ARReplayProvider),
                subsystemTypeOverride = typeof(ARReplayStreetscapeGeometrySubsystem)
            };
            Register(info);
        }

        class ARReplayProvider : Provider
        {
            private StreetscapeGeometryPacket _currentPacket;

            private readonly HashSet<NativeTrackableId> _activeIds = new();
            private readonly List<StreetscapeGeometry> _added = new();
            private readonly List<StreetscapeGeometry> _updated = new();
            private readonly List<NativeTrackableId> _removed = new();

            public override void Start() { }
            public override void Stop() { }
            public override void Destroy() { }

            public override unsafe TrackableChanges<StreetscapeGeometry> GetChanges(
                StreetscapeGeometry defaultGeometry,
                Allocator allocator)
            {
                if (!ARReplay.TryGetReplay(out var replay))
                {
                    return default;
                }

                if (!replay.Metadata.TryGetObject(TrackID.ARCoreStreetscapeGeometry, out _currentPacket))
                {
                    return default;
                }
                if (!_currentPacket.IsAvailable)
                {
                    return default;
                }

                _currentPacket.CorrectTrackable(_activeIds, _added, _updated, _removed);
                return _currentPacket.AsTrackableChanges(allocator);
            }

            public override bool TryGetMesh(NativeTrackableId trackableId, out Mesh mesh)
            {
                if (_currentPacket == null ||
                    !_currentPacket.meshes.TryGetValue(trackableId, out byte[] meshBytes))
                {
                    mesh = default;
                    return false;
                }

                var newMesh = new Mesh();
                if (newMesh.UpdateFromBytes(meshBytes))
                {
                    mesh = newMesh;
                    return true;
                }
                else
                {
                    Object.Destroy(newMesh);
                    mesh = default;
                    return false;
                }
            }
        }
    }
}
#endif // ARCORE_EXTENSIONS_ENABLED
