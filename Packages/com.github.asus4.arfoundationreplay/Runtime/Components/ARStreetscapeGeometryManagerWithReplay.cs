#if ARCORE_EXTENSIONS_ENABLED

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using Google.XR.ARCoreExtensions;

namespace ARFoundationReplay
{

    public sealed class ARStreetscapeGeometryManagerWithReplay : ARStreetscapeGeometryManager
    {
        private static FieldInfo _changedEventField;

        private bool _useReplay = false;
        private XRStreetscapeGeometrySubsystem _subsystem;

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

            arGeometry = new ARStreetscapeGeometryWithReplay(geometry);
            if (_subsystem.TryGetMesh(geometry.trackableId, out Mesh mesh))
            {
                arGeometry.Mesh = mesh;
            }
            _geometries.Add(geometry.trackableId, arGeometry);
            return arGeometry;
        }


        private void InvokeChangedEvent(ARStreetscapeGeometriesChangedEventArgs args)
        {
            // Only the base class can raise the event, so invoke its backing field.
            // Read it every time, as handlers subscribe and unsubscribe at any time.
            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            _changedEventField ??= typeof(ARStreetscapeGeometryManager).GetField("StreetscapeGeometriesChanged", bindingFlags)
                ?? throw new InvalidOperationException("Not found ARStreetscapeGeometryManager.StreetscapeGeometriesChanged");
            var handler = (Action<ARStreetscapeGeometriesChangedEventArgs>)_changedEventField.GetValue(this);
            handler?.Invoke(args);
        }
    }
}
#endif // ARCORE_EXTENSIONS_ENABLED
