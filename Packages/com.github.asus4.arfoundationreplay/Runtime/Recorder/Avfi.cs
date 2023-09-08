/*
 * Meta Avfi https://github.com/asus4/MetaAvfi
 * Based on https://github.com/keijiro/Avfi
 * Unlicense license
*/

using DllImportAttribute = System.Runtime.InteropServices.DllImportAttribute;
using IntPtr = System.IntPtr;

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

        [DllImport(DllName, EntryPoint = "Avfi_StartRecording")]
        public static extern void StartRecording(string filePath, int width, int height);

        [DllImport(DllName, EntryPoint = "Avfi_AppendFrame")]
        public unsafe static extern void AppendFrame(
            void* pointer, uint size, void* metadata, uint metadataSize, double time);

        [DllImport(DllName, EntryPoint = "Avfi_EndRecording")]
        public static extern void EndRecording();

        #region Metadata
        [DllImport(DllName, EntryPoint = "Avfi_LoadMetadata")]
        public static extern void LoadMetadata(string filePath);

        [DllImport(DllName, EntryPoint = "Avfi_UnloadMetadata")]
        public static extern void UnloadMetadata();

        [DllImport(DllName, EntryPoint = "Avfi_GetBufferSize")]
        public static extern uint GetBufferSize();

        [DllImport(DllName, EntryPoint = "Avfi_PeekMetadata")]
        public unsafe static extern uint PeekMetadata(double time, byte* metadata);
        #endregion // Metadata
    }
}
