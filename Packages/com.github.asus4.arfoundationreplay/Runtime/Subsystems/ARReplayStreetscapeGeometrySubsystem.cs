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

            private readonly TrackableChangesPacketModifier<StreetscapeGeometry> _modifier = new();

            public override void Start() { }

            public override void Stop()
            {
                _modifier.Dispose();
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

                _currentPacket.CorrectTrackable(_modifier);
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
