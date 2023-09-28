using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.ARSubsystems;

namespace ARFoundationReplay
{
    /// <summary>
    /// XRInputSubsystem requires native plugin to register itself
    /// It will be invoked from ARReplay as a workaround
    /// </summary>
    public class ARReplayInputSubsystem : System.IDisposable
    {
        // private readonly HandheldARInputDevice _device;

        public ARReplayInputSubsystem()
        {
            //     var desc = new InputDeviceDescription()
            //     {
            //         interfaceName = "XRInput",
            //         product = "ARFoundationReplay",
            //     };
            //     _device = (HandheldARInputDevice)InputSystem.AddDevice(desc);
            //     InputSystem.EnableDevice(_device);
        }

        public void Dispose()
        {
            // if (_device != null)
            // {
            //     InputSystem.DisableDevice(_device);
            //     InputSystem.RemoveDevice(_device);
            // }
        }

        internal void Update(FrameMetadata packet)
        {
            // Assert.IsNotNull(_device);

            var pose = (UnityEngine.Pose)packet.input;
            // ARReplayInputUpdate(pose);

            // using var buffer = StateEvent.From(_device, out var eventPtr);
            // _device.devicePosition.WriteValueIntoEvent(pose.position, eventPtr);
            // _device.deviceRotation.WriteValueIntoEvent(pose.rotation, eventPtr);
            // InputSystem.QueueEvent(eventPtr);
        }

#if !UNITY_EDITOR && UNITY_IOS
        const string DllName = "__Internal";
#else
        const string DllName = "ARFoundationReplayPlugin";
#endif

        [DllImport(DllName)]
        private static extern void ARReplayInputUpdate(UnityEngine.Pose pose);
    }
}
