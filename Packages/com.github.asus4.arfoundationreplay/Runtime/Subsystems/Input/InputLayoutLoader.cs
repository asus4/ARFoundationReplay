using UnityEngine;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARSubsystems;

using Inputs = UnityEngine.InputSystem.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ARFoundationReplay
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    sealed class InputLayoutLoader
    {

#if UNITY_EDITOR
        static InputLayoutLoader()
        {
            RegisterLayouts();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterLayouts()
        {
            if (!ARFoundationReplayLoader.TryGetLoader(out var _))
            {
                // Debug.LogWarning($"[Input] ARFoundationReplayLoader not found");
                return;
            }

            Inputs.RegisterLayout<HandheldARInputDevice>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct("(ARFoundationReplay)")
                );
        }
    }
}
