#include "XR/IUnityXRInput.h"
#include "XR/IUnityXRTrace.h"
#include "input.h"

static IUnityInterfaces *s_UnityInterfaces = nullptr;
static IUnityXRTrace *s_XrTrace = nullptr;

extern "C"
{
    // Entry point for Unity XR SDK
    void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
    UnityPluginLoad(IUnityInterfaces *unityInterfaces)
    {
        // Setup logging
        s_UnityInterfaces = unityInterfaces;
        s_XrTrace = unityInterfaces->Get<IUnityXRTrace>();

        XR_TRACE_LOG(s_XrTrace, "[ARReplay] UnityPluginLoad\n");
        RegisterInputLifecycleProvider(unityInterfaces, s_XrTrace);
    }

    void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
    UnityPluginUnload()
    {
        XR_TRACE_LOG(s_XrTrace, "[ARReplay] UnityPluginUnload\n");
        s_UnityInterfaces = nullptr;
        s_XrTrace = nullptr;
    }

} // extern "C"
