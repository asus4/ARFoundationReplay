using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

namespace ARFoundationReplay
{
    /// <summary>
    /// XRInputSubsystem requires native plugin to register itself
    /// It will be invoked from ARReplay as a workaround
    /// </summary>
    public class ARReplayInputSubsystem : System.IDisposable
    {
        private readonly Transform _target;
        private readonly Behaviour _driver;

        public ARReplayInputSubsystem()
        {
            _target = Object.FindObjectOfType<XROrigin>().Camera.transform;

            // Disable TrackedPoseDriver or ARPoseDriver
            if (_target.TryGetComponent(out TrackedPoseDriver driver))
            {
                _driver = driver;
            }
#pragma warning disable 0618 // Obsolete support
            else if (_target.TryGetComponent(out ARPoseDriver arDriver))
            {
                _driver = arDriver;
            }
#pragma warning restore 0618
            if (_driver != null)
            {
                _driver.enabled = false;
            }
        }

        public void Dispose()
        {
            if (_driver != null)
            {
                _driver.enabled = true;
            }
        }

        public void Update(Packet packet)
        {
            var pose = packet.trackedPose;
            _target.localPosition = pose.position;
            _target.localRotation = pose.rotation;
        }
    }
}
