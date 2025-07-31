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
    internal sealed class PointCloudEncoder : ISubsystemEncoder
    {
        private ARPointCloudManager _pointCloudManager;

        public bool Initialize(XROrigin origin, Material muxMaterial)
        {
            _pointCloudManager = origin.GetComponentInChildren<ARPointCloudManager>();
            if (_pointCloudManager == null)
            {
                return false;
            }
            _pointCloudManager.trackablesChanged.AddListener(OnPointCloudsChanged);
            return true;
        }

        public void Dispose()
        {
            if (_pointCloudManager != null)
            {
                _pointCloudManager.trackablesChanged.RemoveListener(OnPointCloudsChanged);
                _pointCloudManager = null;
            }
        }

        public void Encode(FrameMetadata metadata)
        {
            //    metadata.plane = _packet.IsAvailable ? _packet : null;
        }

        public void PostEncode()
        {
            // _packet.Reset();
        }

        private void OnPointCloudsChanged(ARTrackablesChangedEventArgs<ARPointCloud> args)
        {
            foreach (var pointCloud in args.added)
            {
                Debug.Log($"Point cloud added: {pointCloud.trackableId}");
            }

            foreach (var pointCloud in args.updated)
            {
                Debug.Log($"Point cloud updated: {pointCloud.trackableId}");
            }

            foreach (var kv in args.removed)
            {
                Debug.Log($"Point cloud removed: {kv.Key}");
            }
        }
    }
}
