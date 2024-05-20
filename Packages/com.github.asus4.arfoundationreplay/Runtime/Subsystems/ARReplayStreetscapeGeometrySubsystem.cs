#if ARCORE_EXTENSIONS_ENABLED
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using Unity.Collections;

namespace ARFoundationReplay
{
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
            private readonly Dictionary<TrackableId, Mesh> _cachedMeshes = new();

            private readonly HashSet<TrackableId> _activeIds = new();
            private readonly List<StreetscapeGeometry> _added = new();
            private readonly List<StreetscapeGeometry> _updated = new();
            private readonly List<TrackableId> _removed = new();

            public override void Start() { }
            public override void Stop()
            {
                _activeIds.Clear();
                _added.Clear();
                _updated.Clear();
                _removed.Clear();
                _currentPacket = null;
            }

            public override void Destroy()
            {
                foreach (var mesh in _cachedMeshes.Values)
                {
                    Object.Destroy(mesh);
                }
                _cachedMeshes.Clear();
            }

            public override unsafe TrackableChanges<StreetscapeGeometry> GetChanges(
                StreetscapeGeometry defaultGeometry,
                Allocator allocator)
            {
                if (!ARReplay.TryGetReplay(out var replay))
                {
                    return default;
                }

                _currentPacket = replay.Metadata.streetscapeGeometry;
                if (_currentPacket == null || !_currentPacket.IsAvailable)
                {
                    return default;
                }

                // Cache meshes
                foreach (var kv in _currentPacket.meshes)
                {
                    _cachedMeshes[kv.Key] = kv.Value;
                }

                _currentPacket.CorrectTrackable(_activeIds, _added, _updated, _removed);
                return _currentPacket.AsTrackableChanges(allocator);
            }

            public override bool TryGetMesh(TrackableId trackableId, out Mesh mesh)
            {
                if (_cachedMeshes.TryGetValue(trackableId, out mesh))
                {
                    return true;
                }

                if (!_currentPacket.meshes.TryGetValue(trackableId, out mesh))
                {
                    mesh = default;
                    return false;
                }

                _cachedMeshes.Add(trackableId, mesh);

                return true;
            }
        }
    }
}
#endif // ARCORE_EXTENSIONS_ENABLED
