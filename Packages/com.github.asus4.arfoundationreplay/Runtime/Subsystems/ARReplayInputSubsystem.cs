using System.Runtime.InteropServices;

namespace ARFoundationReplay
{
    /// <summary>
    /// XRInputSubsystem requires native plugin to register itself
    /// It will be invoked from ARReplay as a workaround
    /// </summary>
    public sealed class ARReplayInputSubsystem
    {
        // The ID should be the same with 
        // - UnitySubsystemsManifest.json
        // - input.cpp native plugin
        public const string ID = "ARReplay-Input";

        static internal void Update(FrameMetadata packet)
        {
            var pose = (UnityEngine.Pose)packet.input;
            ARReplayInputUpdate(pose);
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
