#if ARCORE_EXTENSIONS_ENABLED

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using Google.XR.ARCoreExtensions;

namespace ARFoundationReplay
{

    public sealed class ARStreetscapeGeometryManagerWithReplay : ARStreetscapeGeometryManager
    {
        private bool _useReplay = false;
        private XRStreetscapeGeometrySubsystem _subsystem;
        private MulticastDelegate _updateDelegates;

        private readonly Dictionary<TrackableId, ARStreetscapeGeometryWithReplay> _geometries = new();

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

        public override void Update()
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

            // Clear all
            _added.Clear();
            _updated.Clear();
            _removed.Clear();

            // Create ARStreetscapeGeometriesChangedEventArgs
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

            // Debug.Log($"Invoke [{Time.frameCount}] = added={_added.Count}, updated={_updated.Count}, removed={_removed.Count}");
            InvokeChangedEvent(new ARStreetscapeGeometriesChangedEventArgs(_added, _updated, _removed));
        }

        private ARStreetscapeGeometry Convert(StreetscapeGeometry geometry)
        {
            if (_geometries.TryGetValue(geometry.trackableId, out var arGeometry))
            {
                arGeometry.UpdateGeometry(geometry);
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
            if (_updateDelegates == null)
            {
                const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var field = typeof(ARStreetscapeGeometryManager).GetField("StreetscapeGeometriesChanged", bindingFlags)
                    ?? throw new InvalidOperationException("Not found ARStreetscapeGeometryManager.StreetscapeGeometriesChanged");
                _updateDelegates = (MulticastDelegate)field.GetValue(this);
            }

            // Using cache to speed up invoke time
            var handlers = _updateDelegates.GetInvocationList()
                .Cast<Action<ARStreetscapeGeometriesChangedEventArgs>>();
            foreach (var handler in handlers)
            {
                handler?.Invoke(args);
            }
        }
    }
}
#endif // ARCORE_EXTENSIONS_ENABLED
