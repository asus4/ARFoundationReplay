#if ARCORE_EXTENSIONS_ENABLED

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Google.XR.ARCoreExtensions;
using System;
using System.Reflection;

namespace ARFoundationReplay
{
    using NativeTrackableId = UnityEngine.XR.ARSubsystems.TrackableId;

    public sealed class ARStreetscapeGeometryManagerWithReplay : ARStreetscapeGeometryManager
    {
        private bool _useReplay = false;
        private XRStreetscapeGeometrySubsystem _subsystem;

        private readonly Dictionary<NativeTrackableId, ARStreetscapeGeometryWithReplay> _geometries = new();

        private readonly List<ARStreetscapeGeometry> _added = new();
        private readonly List<ARStreetscapeGeometry> _updated = new();
        private readonly List<ARStreetscapeGeometry> _removed = new();



        private void Start()
        {
            _useReplay = false;
            if (ARFoundationReplayLoader.TryGetLoader(out var loader))
            {
                _subsystem = loader.GetLoadedSubsystem<XRStreetscapeGeometrySubsystem>();
                if (_subsystem != null)
                {
                    _useReplay = true;
                }
            }
        }

        public new void Update()
        {
            if (!_useReplay)
            {
                base.Update();
                return;
            }
            // Use replay
            using var changes = _subsystem.GetChanges(Allocator.Temp);

            if (changes.added.Length == 0 && changes.updated.Length == 0 && changes.removed.Length == 0)
            {
                return;
            }

            foreach (var added in changes.added)
            {
                _added.Add(Convert(added));
            }
            foreach (var updated in changes.updated)
            {
                var arGeometry = Convert(updated);
                _updated.Add(arGeometry);
            }
            foreach (var removedId in changes.removed)
            {
                if (_geometries.TryGetValue(removedId, out var arGeometry))
                {
                    _removed.Add(arGeometry);
                    _geometries.Remove(removedId);
                }
            }

            InvokeChangedEvent(new ARStreetscapeGeometriesChangedEventArgs(_added, _updated, _removed));
        }

        private ARStreetscapeGeometry Convert(StreetscapeGeometry geometry)
        {
            if (_geometries.TryGetValue(geometry.trackableId, out var arGeometry))
            {
                return arGeometry;
            }

            // Call private constructor using Activator
            arGeometry = new ARStreetscapeGeometryWithReplay(geometry);
            // Set private field _mesh
            if (_subsystem.TryGetMesh(geometry.trackableId, out Mesh mesh))
            {
                arGeometry.Mesh = mesh;
            }
            return arGeometry;
        }

        private void InvokeChangedEvent(ARStreetscapeGeometriesChangedEventArgs args)
        {
            // TODO: cache to speed up
            var field = typeof(ARStreetscapeGeometryManager)
                .GetField("StreetscapeGeometriesChanged", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var eventDelegate = (MulticastDelegate)field.GetValue(this);
            if (eventDelegate != null)
            {
                foreach (var handler in eventDelegate.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, new object[] { args });
                }
            }
        }
    }
}
#endif // ARCORE_EXTENSIONS_ENABLED
