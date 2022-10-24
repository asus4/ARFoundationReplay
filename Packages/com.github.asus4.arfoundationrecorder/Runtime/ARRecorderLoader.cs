using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;

namespace ARFoundationRecorder
{
    public sealed class ARRecorderLoader : XRLoaderHelper
    {
        static readonly List<XRSessionSubsystemDescriptor> s_SessionSubsystemDescriptors = new List<XRSessionSubsystemDescriptor>();
        static readonly List<XRCameraSubsystemDescriptor> s_CameraSubsystemDescriptors = new List<XRCameraSubsystemDescriptor>();


        public override bool Initialize()
        {
            CreateSubsystem<XRSessionSubsystemDescriptor, XRSessionSubsystem>(s_SessionSubsystemDescriptors, ARRecorderSessionSubsystem.ID);
            CreateSubsystem<XRCameraSubsystemDescriptor, XRCameraSubsystem>(s_CameraSubsystemDescriptors, ARRecorderCameraSubsystem.ID);

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
            Debug.Log("Deinitialize");

            DestroySubsystem<XRCameraSubsystem>();
            DestroySubsystem<XRSessionSubsystem>();

            return base.Deinitialize();
        }
    }
}
