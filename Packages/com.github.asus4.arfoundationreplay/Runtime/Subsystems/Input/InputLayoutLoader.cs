using UnityEngine;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;

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
            if (FindLoader() == null)
            {
                // Debug.LogWarning($"[Input] ARFoundationReplayLoader not found");
                return;
            }

            Inputs.RegisterLayout<HandheldARInputDevice>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct("(ARFoundationReplay)")
                );
            Debug.Log($"[Input] Registered input layout");
        }

        private static ARFoundationReplayLoader FindLoader()
        {
            var instance = XRGeneralSettings.Instance;
            if (instance == null)
            {
                return null;
            }
            var manager = instance.Manager;
            if (manager == null || manager.activeLoaders == null)
            {
                return null;
            }
            foreach (var loader in manager.activeLoaders)
            {
                if (loader is ARFoundationReplayLoader replayLoader)
                {
                    return replayLoader;
                }
            }

            return null;
        }
    }
}
