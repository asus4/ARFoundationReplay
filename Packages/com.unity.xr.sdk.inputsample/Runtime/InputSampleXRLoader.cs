using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace Unity.XR.SDK
{
    public class InputSampleXRLoader : XRLoaderHelper
    {
        private static List<XRInputSubsystemDescriptor> s_InputSubsystemDescriptors =
            new List<XRInputSubsystemDescriptor>();

        public override bool Initialize()
        {
            Debug.Log("InputSampleXRLoader.Initialize");
            CreateSubsystem<XRInputSubsystemDescriptor, XRInputSubsystem>(s_InputSubsystemDescriptors, "input0");
            return true;
        }

        public override bool Start()
        {
            Debug.Log("InputSampleXRLoader.Start");
            StartSubsystem<XRInputSubsystem>();
            return true;
        }

        public override bool Stop()
        {
            Debug.Log("InputSampleXRLoader.Stop");
            StopSubsystem<XRInputSubsystem>();
            return true;
        }

        public override bool Deinitialize()
        {
            Debug.Log("InputSampleXRLoader.Deinitialize");
            DestroySubsystem<XRInputSubsystem>();
            return true;
        }
    }
}
