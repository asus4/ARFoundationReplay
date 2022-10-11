using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ARFoundationRecorder
{
    public sealed class ARRecorder : MonoBehaviour
    {
        private ARCameraManager _cameraManager;

        public bool IsRecording { get; private set; }

        private void OnEnable()
        {
            _cameraManager = GameObject.FindObjectOfType<ARCameraManager>();
        }

        private void OnDisable()
        {
            if (IsRecording)
            {
                StopRecording();
            }
        }

        public void StartRecording()
        {
            if (IsRecording) { return; }
            Debug.Log("StartRecording");
            IsRecording = true;
            _cameraManager.frameReceived += OnFrameReceived;
        }

        public void StopRecording()
        {
            if (!IsRecording) { return; }
            Debug.Log("StopRecording");
            _cameraManager.frameReceived -= OnFrameReceived;
            IsRecording = false;
        }

        private void OnFrameReceived(ARCameraFrameEventArgs args)
        {
            Debug.Log($"OnFrameReceived: {args}");
        }
    }
}
