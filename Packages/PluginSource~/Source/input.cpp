#include "input.h"

const UnityXRInternalInputDeviceId kReplayDeviceId = 0;

static IUnityXRTrace *s_XrTrace = nullptr;
static IUnityXRInputInterface *s_XrInput = nullptr;
static UnityXRPose s_Pose = {};
static UnityXRInputTrackingOriginModeFlags s_trackingMode = kUnityXRInputTrackingOriginModeUnknown;

//-------------
// Input Subsystem

static UnitySubsystemErrorCode Tick(UnitySubsystemHandle handle, void *userData, UnityXRInputUpdateType updateType)
{
    return kUnitySubsystemErrorCodeSuccess;
}

static UnitySubsystemErrorCode FillDeviceDefinition(UnitySubsystemHandle handle, void *userData, UnityXRInternalInputDeviceId deviceId, UnityXRInputDeviceDefinition *definition)
{
    // These definitions should reflect the devices you intend to use.
    switch (deviceId)
    {
    case kReplayDeviceId:
    {
        const UnityXRInputDeviceCharacteristics characteristics = (UnityXRInputDeviceCharacteristics)(kUnityXRInputDeviceCharacteristicsHeldInHand | kUnityXRInputDeviceCharacteristicsTrackedDevice);
        s_XrInput->DeviceDefinition_SetName(definition, "ARFoundationReplay");
        s_XrInput->DeviceDefinition_SetCharacteristics(definition, characteristics);

        s_XrInput->DeviceDefinition_AddFeatureWithUsage(definition, "Is Tracked", kUnityXRInputFeatureTypeBinary, kUnityXRInputFeatureUsageIsTracked);
        s_XrInput->DeviceDefinition_AddFeatureWithUsage(definition, "Tracking State", kUnityXRInputFeatureTypeDiscreteStates, kUnityXRInputFeatureUsageTrackingState);
        s_XrInput->DeviceDefinition_AddFeatureWithUsage(definition, "Device Position", kUnityXRInputFeatureTypeAxis3D, kUnityXRInputFeatureUsageDevicePosition);
        s_XrInput->DeviceDefinition_AddFeatureWithUsage(definition, "Device Rotation", kUnityXRInputFeatureTypeRotation, kUnityXRInputFeatureUsageDeviceRotation);
    }
    break;
    default:
        return kUnitySubsystemErrorCodeFailure;
    }
    return kUnitySubsystemErrorCodeSuccess;
}

static UnitySubsystemErrorCode UpdateDeviceState(UnitySubsystemHandle handle, void *userData, UnityXRInternalInputDeviceId deviceId, UnityXRInputUpdateType updateType, UnityXRInputDeviceState *state)
{
    UnityXRInputFeatureIndex featureIndex = 0;

    // Feature values should either be stored as indices when creating the device definitions, or you can also follow this incrementing pattern
    // and use the *exact* same feature order as declared in the definition.
    switch (deviceId)
    {
    case kReplayDeviceId:
    {
        s_XrInput->DeviceState_SetBinaryValue(state, featureIndex++, true);
        s_XrInput->DeviceState_SetDiscreteStateValue(state, featureIndex++, (kUnityXRInputTrackingStatePosition | kUnityXRInputTrackingStateRotation));
        s_XrInput->DeviceState_SetAxis3DValue(state, featureIndex++, s_Pose.position);
        s_XrInput->DeviceState_SetRotationValue(state, featureIndex++, s_Pose.rotation);
    }
    break;
    default:
        return kUnitySubsystemErrorCodeFailure;
    }
    return kUnitySubsystemErrorCodeSuccess;
}

static UnitySubsystemErrorCode HandleEvent(UnitySubsystemHandle handle, void *userData, unsigned int eventType, UnityXRInternalInputDeviceId deviceId, void *buffer, unsigned int size)
{
    XR_TRACE_LOG(s_XrTrace, "[ARReplay]: Handle Event received with eventType[%u], DeviceId[%llu], Buffer Size[%u].\n", eventType, deviceId, size);
    return kUnitySubsystemErrorCodeFailure;
}

static UnitySubsystemErrorCode HandleRecenter(UnitySubsystemHandle handle, void *userData)
{
    XR_TRACE_LOG(s_XrTrace, "[ARReplay]: Handle Recenter received.\n");
    return kUnitySubsystemErrorCodeSuccess;
}

static UnitySubsystemErrorCode HandleHapticImpulse(UnitySubsystemHandle handle, void *userData, UnityXRInternalInputDeviceId deviceId, int channel, float amplitude, float duration)
{
    XR_TRACE_LOG(s_XrTrace, "[ARReplay]: Handle Haptic Impulse received with DeviceId[%llu], Channel[%i], Amplitude[%f], Duration[%f].\n", deviceId, channel, amplitude, duration);
    return kUnitySubsystemErrorCodeFailure;
}

static UnitySubsystemErrorCode HandleHapticBuffer(UnitySubsystemHandle handle, void *userData, UnityXRInternalInputDeviceId deviceId, int channel, unsigned int bufferSize, const unsigned char *const buffer)
{
    XR_TRACE_LOG(s_XrTrace, "[ARReplay]: Handle Haptic Buffer received with DeviceId[%llu], Channel[%i], Buffer Size[%i].\n", deviceId, channel, bufferSize);
    return kUnitySubsystemErrorCodeFailure;
}

static UnitySubsystemErrorCode QueryHapticCapabilities(UnitySubsystemHandle handle, void *userData, UnityXRInternalInputDeviceId deviceId, UnityXRHapticCapabilities *capabilities)
{
    XR_TRACE_LOG(s_XrTrace, "[ARReplay]: Query Haptic Capabilities received with DeviceId[%llu].\n", deviceId);

    capabilities->numChannels = 0;
    capabilities->supportsImpulse = false;
    capabilities->supportsBuffer = false;
    capabilities->bufferFrequencyHz = 0;
    capabilities->bufferMaxSize = 0;
    capabilities->bufferOptimalSize = 0;

    return kUnitySubsystemErrorCodeSuccess;
}

static UnitySubsystemErrorCode HandleHapticStop(UnitySubsystemHandle handle, void *userData, UnityXRInternalInputDeviceId deviceId)
{
    XR_TRACE_LOG(s_XrTrace, "[ARReplay]: Handle Haptic Stop received.\n");
    return kUnitySubsystemErrorCodeSuccess;
}

static UnitySubsystemErrorCode QueryTrackingOriginMode(UnitySubsystemHandle handle, void *userData, UnityXRInputTrackingOriginModeFlags *trackingOriginMode)
{
    *trackingOriginMode = s_trackingMode;
    return kUnitySubsystemErrorCodeSuccess;
}

static UnitySubsystemErrorCode QuerySupportedTrackingOriginModes(UnitySubsystemHandle handle, void *userData, UnityXRInputTrackingOriginModeFlags *supportedTrackingOriginModes)
{
    const UnityXRInputTrackingOriginModeFlags all = (UnityXRInputTrackingOriginModeFlags)(kUnityXRInputTrackingOriginModeDevice | kUnityXRInputTrackingOriginModeFloor);
    *supportedTrackingOriginModes = all;
    return kUnitySubsystemErrorCodeSuccess;
}

static UnitySubsystemErrorCode HandleSetTrackingOriginMode(UnitySubsystemHandle handle, void *userData, UnityXRInputTrackingOriginModeFlags trackingOriginMode)
{
    s_trackingMode = trackingOriginMode;
    return kUnitySubsystemErrorCodeSuccess;
}

/// Callback executed when a subsystem should initialize in preparation for becoming active.
UnitySubsystemErrorCode UNITY_INTERFACE_API Lifecycle_Initialize(UnitySubsystemHandle handle, void *data)
{
    XR_TRACE_LOG(s_XrTrace, "[ARReplay] Lifecycle_Initialize\n");

    UnityXRInputProvider inputProvider;
    inputProvider.userData = nullptr;
    inputProvider.Tick = &Tick;
    inputProvider.FillDeviceDefinition = &FillDeviceDefinition;
    inputProvider.UpdateDeviceState = &UpdateDeviceState;
    inputProvider.HandleEvent = &HandleEvent;
    inputProvider.HandleRecenter = &HandleRecenter;
    inputProvider.HandleHapticImpulse = &HandleHapticImpulse;
    inputProvider.HandleHapticBuffer = &HandleHapticBuffer;
    inputProvider.QueryHapticCapabilities = &QueryHapticCapabilities;
    inputProvider.HandleHapticStop = &HandleHapticStop;
    inputProvider.QueryTrackingOriginMode = &QueryTrackingOriginMode;
    inputProvider.QuerySupportedTrackingOriginModes = &QuerySupportedTrackingOriginModes;
    inputProvider.HandleSetTrackingOriginMode = &HandleSetTrackingOriginMode;

    // Tracking
    s_XrInput->RegisterInputProvider(handle, &inputProvider);

    return kUnitySubsystemErrorCodeSuccess;
}

/// Callback executed when a subsystem should become active.
UnitySubsystemErrorCode UNITY_INTERFACE_API Lifecycle_Start(UnitySubsystemHandle handle, void *data)
{
    XR_TRACE_LOG(s_XrTrace, "[ARReplay] Lifecycle_Start\n");
    s_XrInput->InputSubsystem_DeviceConnected(handle, kReplayDeviceId);

    UnityXRVector3 boundary[4] = {
        {-10.0f, 0.f, 10.0f},
        {10.0f, 0.f, 10.0f},
        {10.0f, 0.f, -10.0f},
        {-10.0f, 0.f, -10.0f}};
    s_XrInput->InputSubsystem_SetTrackingBoundary(handle, boundary, 4);

    return kUnitySubsystemErrorCodeSuccess;
}

/// Callback executed when a subsystem should become inactive.
void UNITY_INTERFACE_API Lifecycle_Stop(UnitySubsystemHandle handle, void *data)
{
    XR_TRACE_LOG(s_XrTrace, "[ARReplay] Lifecycle_Stop\n");
    s_XrInput->InputSubsystem_DeviceDisconnected(handle, kReplayDeviceId);
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

extern "C"
{
    void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API ARReplayInputUpdate(UnityXRPose pose)
    {
        s_Pose = pose;
    }
}
