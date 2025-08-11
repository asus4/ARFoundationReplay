using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

sealed class RaycastVisualizer : MonoBehaviour
{
    [SerializeField]
    TrackableType trackableType = TrackableType.AllTypes;

    Camera targetCamera;
    ARRaycastManager raycastManager;

    readonly List<ARRaycastHit> hits = new();


    void OnEnable()
    {
        EnhancedTouchSupport.Enable();

        var xrOrigin = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("XROrigin not found. RaycastVisualizer requires XROrigin in the scene.");
        }
        if (!xrOrigin.TryGetComponent(out raycastManager))
        {
            Debug.LogError("ARRaycastManager component not found on XROrigin.");
        }

        targetCamera = xrOrigin.Camera;
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse.leftButton.wasPressedThisFrame)
        {
            var position = mouse.position.ReadValue();

            if (raycastManager.Raycast(position, hits, trackableType))
            {
                Vector3 startPos = targetCamera.transform.position;
                var ray = hits[0];
                Vector3 dir = ray.pose.position - startPos;
                Debug.DrawRay(startPos, dir, Color.green, 1f);
            }
        }
    }
}
