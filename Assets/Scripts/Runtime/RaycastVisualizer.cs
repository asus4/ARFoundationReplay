using System.Collections.Generic;
using System.Text;
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
    readonly StringBuilder sb = new();
    Vector3 startPosition;

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

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse.leftButton.wasPressedThisFrame)
        {
            var position = mouse.position.ReadValue();

            if (raycastManager.Raycast(position, hits, trackableType))
            {
                startPosition = targetCamera.transform.position;

                sb.Clear();
                sb.AppendLine($"Raycast Hits: {hits.Count}");
                int i = 1;
                foreach (var hit in hits)
                {
                    sb.AppendLine($"{i}: {hit.hitType} = {hit.pose.position}");
                    i++;
                }
                Debug.Log(sb);
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        int i = 1;
        foreach (var hit in hits)
        {
            Vector3 dir = hit.pose.position - startPosition;
            Gizmos.DrawRay(startPosition, dir);
            Gizmos.DrawWireSphere(hit.pose.position, 0.005f);
            i++;
        }
    }
#endif // UNITY_EDITOR
}
