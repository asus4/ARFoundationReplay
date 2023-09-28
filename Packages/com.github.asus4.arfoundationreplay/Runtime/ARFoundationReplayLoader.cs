using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;

namespace ARFoundationReplay
{
    public class ARFoundationReplayLoader : XRLoaderHelper
    {
        static readonly List<XRSessionSubsystemDescriptor> s_SessionSubsystemDescriptors = new();
        static readonly List<XRCameraSubsystemDescriptor> s_CameraSubsystemDescriptors = new();
        static readonly List<XRInputSubsystemDescriptor> s_InputSubsystemDescriptors = new();
        static readonly List<XROcclusionSubsystemDescriptor> s_OcclusionSubsystemDescriptors = new();
        static readonly List<XRPlaneSubsystemDescriptor> s_PlaneSubsystemDescriptors = new();


        public override bool Initialize()
        {
            // Required subsystems
            CreateSubsystem<XRSessionSubsystemDescriptor, XRSessionSubsystem>(s_SessionSubsystemDescriptors, ARReplaySessionSubsystem.ID);
            CreateSubsystem<XRCameraSubsystemDescriptor, XRCameraSubsystem>(s_CameraSubsystemDescriptors, ARReplayCameraSubsystem.ID);
            CreateSubsystem<XRInputSubsystemDescriptor, XRInputSubsystem>(s_InputSubsystemDescriptors, "ARReplay-Input");

            // Optional subsystems
            CreateSubsystem<XROcclusionSubsystemDescriptor, XROcclusionSubsystem>(s_OcclusionSubsystemDescriptors, ARReplayOcclusionSubsystem.ID);
            CreateSubsystem<XRPlaneSubsystemDescriptor, XRPlaneSubsystem>(s_PlaneSubsystemDescriptors, ARReplayPlaneSubsystem.ID);

            var sessionSubsystem = GetLoadedSubsystem<XRSessionSubsystem>();
            if (sessionSubsystem == null)
            {
                Debug.LogError("Failed to load session subsystem.");
            }

            var inputSubsystem = GetLoadedSubsystem<XRInputSubsystem>();
            if (inputSubsystem == null)
            {
                Debug.LogError("Failed to load input subsystem.");
            }

            Debug.Log("ARFoundationReplayLoader.Initialize()");
            return sessionSubsystem != null;
        }

        public override bool Start()
        {
            var inputDescriptors = new List<XRInputSubsystemDescriptor>();
            SubsystemManager.GetSubsystemDescriptors(inputDescriptors);
            int i = 0;
            foreach (var descriptor in inputDescriptors)
            {
                Debug.Log($"[{i}]: {descriptor.id}");
                i++;
            }

            return true;
        }

        public override bool Stop()
        {
            return true;
        }

        public override bool Deinitialize()
        {
            DestroySubsystem<XRPlaneSubsystem>();
            DestroySubsystem<XROcclusionSubsystem>();

            DestroySubsystem<XRInputSubsystem>();
            DestroySubsystem<XRCameraSubsystem>();
            DestroySubsystem<XRSessionSubsystem>();

            return base.Deinitialize();
        }
    }
}
