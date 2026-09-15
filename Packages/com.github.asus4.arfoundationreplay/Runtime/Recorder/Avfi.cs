/*
 * Meta Avfi https://github.com/asus4/MetaAvfi
 * Based on https://github.com/keijiro/Avfi
 * Unlicense license
*/

using System.Runtime.InteropServices;

namespace ARFoundationReplay
{
    /// <summary>
    /// Native interface to Avfi.
    /// </summary>
    static internal class Avfi
    {
#if !UNITY_EDITOR && UNITY_IOS
        const string DllName = "__Internal";
#else
        const string DllName = "Avfi";
#endif

        #region Recording
        /// <param name="audioMode">See <see cref="AudioCaptureMode"/></param>
        /// <param name="sampleRate">Sample rate of the audio track. For UnityAudioOutput, the PCM format pushed via AppendAudio.</param>
        /// <param name="channels">Channel count (1 or 2) of the audio track.</param>
        [DllImport(DllName, EntryPoint = "Avfi_StartRecording")]
        public static extern void StartRecording(string filePath, int width, int height, int audioMode, int sampleRate, int channels);

        [DllImport(DllName, EntryPoint = "Avfi_AppendFrame")]
        public unsafe static extern void AppendFrame(
            void* pointer, uint size, void* metadata, uint metadataSize, double time);

        /// <summary>
        /// Append interleaved float32 PCM samples. Safe to call from the Unity audio thread.
        /// </summary>
        [DllImport(DllName, EntryPoint = "Avfi_AppendAudio")]
        public unsafe static extern void AppendAudio(float* interleaved, uint frameCount, uint channels);

        [DllImport(DllName, EntryPoint = "Avfi_EndRecording")]
        public static extern void EndRecording([MarshalAs(UnmanagedType.U1)] bool isSave);

        [DllImport(DllName, EntryPoint = "Avfi_RequestMicrophonePermission")]
        public static extern void RequestMicrophonePermission();

        [DllImport(DllName, EntryPoint = "Avfi_HasMicrophonePermission")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool HasMicrophonePermission();
        #endregion // Recording

        #region Metadata
        [DllImport(DllName, EntryPoint = "Avfi_LoadMetadata")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool LoadMetadata(string filePath);

        [DllImport(DllName, EntryPoint = "Avfi_UnloadMetadata")]
        public static extern void UnloadMetadata();

        [DllImport(DllName, EntryPoint = "Avfi_HasAudioTrack")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool HasAudioTrack();

        [DllImport(DllName, EntryPoint = "Avfi_GetBufferSize")]
        public static extern uint GetBufferSize();

        [DllImport(DllName, EntryPoint = "Avfi_PeekMetadata")]
        public unsafe static extern uint PeekMetadata(double time, byte* metadata);
        #endregion // Metadata
    }
}
