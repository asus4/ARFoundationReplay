using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;

namespace ARFoundationRecorder
{
    /// <summary>
    /// The session subsystem for ARRecorder.
    /// </summary>
    [Preserve]
    internal sealed class ARRecorderSessionSubsystem : XRSessionSubsystem
    {
        public const string ID = "ARRecorder-Session";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterDescriptor()
        {
            XRSessionSubsystemDescriptor.RegisterDescriptor(new XRSessionSubsystemDescriptor.Cinfo
            {
                id = ID,
                providerType = typeof(ARRecorderProvider),
                subsystemTypeOverride = typeof(ARRecorderSessionSubsystem),
                supportsInstall = false,
                supportsMatchFrameRate = false
            });
            Debug.Log($"Register {ID} subsystem");
        }

        class ARRecorderProvider : Provider
        {
            public override Promise<SessionAvailability> GetAvailabilityAsync()
            {
                var flag = SessionAvailability.Supported | SessionAvailability.Installed;
                return Promise<SessionAvailability>.CreateResolvedPromise(flag);
            }

            public override TrackingState trackingState
            {
                get
                {
                    return TrackingState.None;
                }
            }
        }
    }
}
