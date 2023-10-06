#if ARCORE_EXTENSIONS_ENABLED
using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting;
using UnityEngine.SubsystemsImplementation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections.LowLevel.Unsafe;
using Google.XR.ARCoreExtensions;

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
            if (Register(info))
            {
                Debug.Log($"Register {ID} subsystem");
            }
            else
            {
                Debug.LogError($"Cannot register {ID} subsystem");
            }
        }

        class ARReplayProvider : Provider
        {
            public override void Start() { }

            public override void Stop() { }

            public override void Destroy() { }
        }
    }
}
#endif // ARCORE_EXTENSIONS_ENABLED
