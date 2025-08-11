using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Assertions;

namespace ARFoundationReplay
{
    using XROrigin = Unity.XR.CoreUtils.XROrigin;

    internal sealed class ARReplayRaycastSubsystem : XRRaycastSubsystem
    {
        public const string ID = "ARReplay-Raycast";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterDescriptor()
        {
            XRRaycastSubsystemDescriptor.Register(new XRRaycastSubsystemDescriptor.Cinfo
            {
                id = ID,
                providerType = typeof(ARReplayProvider),
                subsystemTypeOverride = typeof(ARReplayRaycastSubsystem),
                supportsViewportBasedRaycast = true,
                supportedTrackableTypes = TrackableType.FeaturePoint | TrackableType.Planes,
                supportsTrackedRaycasts = false,
            });
        }

        class ARReplayProvider : Provider
        {
            delegate NativeArray<XRRaycastHit> RaycastFunc(Ray sessionSpaceRay, TrackableType trackableTypeMask, Allocator allocator);

            readonly List<RaycastFunc> raycastFunctions = new();
            NativeList<XRRaycastHit> allHits;
            XROrigin xrOrigin;

            XROrigin GetXrOrigin()
            {
                if (xrOrigin == null)
                {
                    xrOrigin = Object.FindAnyObjectByType<XROrigin>();
                }
                return xrOrigin;
            }

            public override void Start()
            {
                raycastFunctions.Clear();

                if (allHits.IsCreated)
                {
                    allHits.Dispose();
                }
                allHits = new NativeList<XRRaycastHit>(0, Allocator.Persistent);

                // HACK: Registers Raycast() as delegate functions
                // Since unable to access internal UnityEngine.XR.ARFoundation.IRaycaster...
                var origin = GetXrOrigin();
                Assert.IsNotNull(origin, "XROrigin not found. ARReplayRaycastSubsystem requires XROrigin in the scene.");

                if (origin.TryGetComponent(out ARPlaneManager planeManager))
                {
                    raycastFunctions.Add(planeManager.Raycast);
                }
                if (origin.TryGetComponent(out ARPointCloudManager pointCloudManager))
                {
                    raycastFunctions.Add(pointCloudManager.Raycast);
                }
                if (origin.TryGetComponent(out ARBoundingBoxManager boundingBoxManager))
                {
                    raycastFunctions.Add(boundingBoxManager.Raycast);
                }
            }

            public override void Stop()
            {
                raycastFunctions.Clear();
                allHits.Dispose();
                xrOrigin = null;
            }

            public override NativeArray<XRRaycastHit> Raycast(XRRaycastHit defaultRaycastHit,
                Ray ray, TrackableType trackableTypeMask, Allocator allocator)
            {
                var origin = GetXrOrigin();
                var rayWS = origin.TrackablesParent.TransformRay(ray);
                return RaycastWS(rayWS, trackableTypeMask, allocator);
            }

            public override NativeArray<XRRaycastHit> Raycast(XRRaycastHit defaultRaycastHit,
                Vector2 viewportPoint, TrackableType trackableTypeMask, Allocator allocator)
            {
                var origin = GetXrOrigin();
                var rayWS = origin.Camera.ViewportPointToRay(viewportPoint);
                return RaycastWS(rayWS, trackableTypeMask, allocator);
            }

            NativeArray<XRRaycastHit> RaycastWS(Ray rayWS, TrackableType trackableTypeMask, Allocator allocator)
            {
                allHits.Clear();

                foreach (var raycastFunc in raycastFunctions)
                {
                    using var hits = raycastFunc(rayWS, trackableTypeMask, allocator);
                    if (hits.Length > 0)
                    {
                        allHits.AddRange(hits);
                    }
                }

                // No hits found
                if (allHits.Length == 0)
                {
                    return new NativeArray<XRRaycastHit>(0, allocator);
                }

                return allHits.ToArray(allocator);
            }
        }
    }
}
