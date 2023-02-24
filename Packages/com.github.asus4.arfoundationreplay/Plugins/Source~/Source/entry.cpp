#include "XR/IUnityXRTrace.h"

static IUnityXRTrace* s_XrTrace = nullptr;

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
UnityPluginLoad(IUnityInterfaces* unityInterfaces)
{
    // FIXME: This is not working
    s_XrTrace = unityInterfaces->Get<IUnityXRTrace>();
    XR_TRACE_LOG(s_XrTrace, "[ARReplay] UnityPluginLoad");
}
