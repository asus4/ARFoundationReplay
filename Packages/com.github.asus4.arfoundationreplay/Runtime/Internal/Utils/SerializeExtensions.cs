using System;
using System.Runtime.InteropServices;

namespace ARFoundationReplay
{
    internal static class SerializeExtensions
    {
        public static byte[] ToByteArray<T>(ref this T input)
            where T : struct
        {
            int size = Marshal.SizeOf(input);
            byte[] arr = new byte[size];

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(input, ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return arr;
        }

        public static T ToStruct<T>(this byte[] bytes)
            where T : struct
        {
            T output = default;
            int size = Marshal.SizeOf(output);
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(bytes, 0, ptr, size);
                output = (T)Marshal.PtrToStructure(ptr, typeof(T));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return output;
        }
    }
}
