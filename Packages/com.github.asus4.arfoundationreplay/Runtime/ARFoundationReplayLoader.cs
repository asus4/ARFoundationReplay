using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;

namespace ARFoundationReplay
{
    public sealed class ARFoundationReplayLoader : XRLoaderHelper
    {
        static readonly List<XRSessionSubsystemDescriptor> s_SessionSubsystemDescriptors = new();
        static readonly List<XRCameraSubsystemDescriptor> s_CameraSubsystemDescriptors = new();
        static readonly List<XROcclusionSubsystemDescriptor> s_OcclusionSubsystemDescriptors = new();

        public override bool Initialize()
        {
            CreateSubsystem<XRSessionSubsystemDescriptor, XRSessionSubsystem>(s_SessionSubsystemDescriptors, ARReplaySessionSubsystem.ID);
            CreateSubsystem<XRCameraSubsystemDescriptor, XRCameraSubsystem>(s_CameraSubsystemDescriptors, ARReplayCameraSubsystem.ID);
            // Input subsystem requires to implement a native plugin
            // CreateSubsystem<XRInputSubsystemDescriptor, XRInputSubsystem>(s_InputSubsystemDescriptors, ARReplayInputSubsystem.ID);

            // Optional subsystems
            CreateSubsystem<XROcclusionSubsystemDescriptor, XROcclusionSubsystem>(s_OcclusionSubsystemDescriptors, ARReplayOcclusionSubsystem.ID);

            var sessionSubsystem = GetLoadedSubsystem<XRSessionSubsystem>();
            if (sessionSubsystem == null)
            {
                Debug.LogError("Failed to load session subsystem.");
            }
            return sessionSubsystem != null;
        }

        public override bool Start()
        {
            return true;
        }

        public override bool Stop()
        {
            return true;
        }

        public override bool Deinitialize()
        {
            DestroySubsystem<XRCameraSubsystem>();
            DestroySubsystem<XROcclusionSubsystem>();
            DestroySubsystem<XRSessionSubsystem>();

            return base.Deinitialize();
        }
    }
}
