using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem.XR;

namespace ARFoundationReplay
{
    /// <summary>
    /// XRInputSubsystem requires native plugin
    /// Invoked from ARReplay as a workaround
    /// </summary>
    public class ARReplayInputSubsystem : System.IDisposable
    {
        private Transform _target;
        private TrackedPoseDriver _driver;

        public ARReplayInputSubsystem()
        {
            _target = Object.FindObjectOfType<XROrigin>().Camera.transform;
            // Disable overriding in XRInputSubsystem
            if (_target.TryGetComponent(out _driver))
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
