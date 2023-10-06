using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;

namespace ARFoundationReplay
{
    /// <summary>
    /// The session subsystem for ARFoundationReplay.
    /// </summary>
    [Preserve]
    internal sealed class ARReplaySessionSubsystem : XRSessionSubsystem
    {
        public const string ID = "ARReplay-Session";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterDescriptor()
        {
            XRSessionSubsystemDescriptor.RegisterDescriptor(new XRSessionSubsystemDescriptor.Cinfo
            {
                id = ID,
                providerType = typeof(ARReplayProvider),
                subsystemTypeOverride = typeof(ARReplaySessionSubsystem),
                supportsInstall = false,
                supportsMatchFrameRate = false
            });
            Debug.Log($"Register {ID} subsystem");
        }

        class ARReplayProvider : Provider
        {
            private ARReplay _replay;

            public override Promise<SessionAvailability> GetAvailabilityAsync()
            {
                var flag = SessionAvailability.Supported | SessionAvailability.Installed;
                return Promise<SessionAvailability>.CreateResolvedPromise(flag);
            }

            public override void Start()
            {
                ARFoundationReplaySettings setting = null;
#if UNITY_EDITOR
                setting = ARFoundationReplaySettings.CurrentSettings;
#else
                // TODO: Support runtime replay
                throw new System.NotImplementedException("Runtime replay is not supported yet");
#endif
                _replay = new ARReplay(setting);
                StartExtraSystems();
            }

            public override void Stop()
            {
                _replay.Dispose();
                _replay = null;
            }

            public override void Update(XRSessionUpdateParams updateParams)
            {
                Assert.IsNotNull(_replay);
                _replay.Update();
            }

            public override TrackingState trackingState
            {
                get
                {
                    return TrackingState.Tracking;
                }
            }

            private void StartExtraSystems()
            {
                if (!ARFoundationReplayLoader.TryGetLoader(out var loader))
                {
                    Debug.LogError("Failed to get ARFoundationReplayLoader");
                    return;
                }
#if ARCORE_EXTENSIONS_ENABLED
                loader.EnsureSystemStarted<XRGeospatialEarthSubsystem>();
                loader.EnsureSystemStarted<XRStreetscapeGeometrySubsystem>();
#endif // ARCORE_EXTENSIONS_ENABLED
            }
        }
    }
}
