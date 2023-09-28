#include "input.h"

static IUnityXRTrace *s_XrTrace = nullptr;
static IUnityXRInputInterface *s_XrInput = nullptr;

//-------------
// Input Subsystem

/// Callback executed when a subsystem should initialize in preparation for becoming active.
UnitySubsystemErrorCode UNITY_INTERFACE_API Lifecycle_Initialize(UnitySubsystemHandle handle, void *data)
{
    XR_TRACE_LOG(s_XrTrace, "[ARReplay] Lifecycle_Initialize\n");

    // s_ProviderData.Reset();

    // UnityXRInputProvider inputProvider;
    // inputProvider.userData = NULL;
    // inputProvider.Tick = &Tick;
    // inputProvider.FillDeviceDefinition = &FillDeviceDefinition;
    // inputProvider.UpdateDeviceState = &UpdateDeviceState;
    // inputProvider.HandleEvent = &HandleEvent;
    // inputProvider.HandleRecenter = &HandleRecenter;
    // inputProvider.HandleHapticImpulse = &HandleHapticImpulse;
    // inputProvider.HandleHapticBuffer = &HandleHapticBuffer;
    // inputProvider.QueryHapticCapabilities = &QueryHapticCapabilities;
    // inputProvider.HandleHapticStop = &HandleHapticStop;
    // inputProvider.QueryTrackingOriginMode = &QueryTrackingOriginMode;
    // inputProvider.QuerySupportedTrackingOriginModes = &QuerySupportedTrackingOriginModes;
    // inputProvider.HandleSetTrackingOriginMode = &HandleSetTrackingOriginMode;

    // //Tracking
    // s_XrInput->RegisterInputProvider(handle, &inputProvider);

    return kUnitySubsystemErrorCodeSuccess;
}

/// Callback executed when a subsystem should become active.
UnitySubsystemErrorCode UNITY_INTERFACE_API Lifecycle_Start(UnitySubsystemHandle handle, void *data)
{
    XR_TRACE_LOG(s_XrTrace, "[ARReplay] Lifecycle_Start\n");
    // s_ProviderData.Reset();
    // s_XrInput->InputSubsystem_DeviceConnected(handle, kDeviceId_HMD);

    // UnityXRVector3 boundary[4];
    // SetVector3(boundary[0], -1.f, 0.f, 1.f);
    // SetVector3(boundary[1], 1.f, 0.f, 1.f);
    // SetVector3(boundary[2], 1.f, 0.f, -1.f);
    // SetVector3(boundary[3], -1.f, 0.f, -1.f);

    // s_XrInput->InputSubsystem_SetTrackingBoundary(handle, boundary, 4);

    return kUnitySubsystemErrorCodeSuccess;
}

/// Callback executed when a subsystem should become inactive.
void UNITY_INTERFACE_API Lifecycle_Stop(UnitySubsystemHandle handle, void *data)
{
    XR_TRACE_LOG(s_XrTrace, "[ARReplay] Lifecycle_Stop\n");
    // s_XrInput->InputSubsystem_DeviceDisconnected(handle, kDeviceId_HMD);
    // s_XrInput->InputSubsystem_DeviceDisconnected(handle, kDeviceId_Controller);
}

/// Callback executed when a subsystem should release all resources and is about to be unloaded.
void UNITY_INTERFACE_API Lifecycle_Shutdown(UnitySubsystemHandle handle, void *data)
{
    XR_TRACE_LOG(s_XrTrace, "[ARReplay] Lifecycle_Shutdown\n");
}

void RegisterInputLifecycleProvider(IUnityInterfaces *unityInterfaces, IUnityXRTrace *xrTrace)
{
    s_XrTrace = xrTrace;

    // Setup input
    s_XrInput = unityInterfaces->Get<IUnityXRInputInterface>();
    UnityLifecycleProvider inputLifecycleHandler = {
        nullptr,
        &Lifecycle_Initialize,
        &Lifecycle_Start,
        &Lifecycle_Stop,
        &Lifecycle_Shutdown};

    s_XrInput->RegisterLifecycleProvider("AR Foundation Replay Plugin", "ARReplay-Input", &inputLifecycleHandler);

    XR_TRACE_LOG(s_XrTrace, "[ARReplay] Setup Input Provider with id: ARReplay-Input\n");
}
