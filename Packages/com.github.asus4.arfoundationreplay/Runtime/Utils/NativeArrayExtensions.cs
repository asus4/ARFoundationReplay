using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ARFoundationReplay
{
    public static class NativeArrayExtensions
    {
        /// <summary>
        /// Copy C# span into new NativeArray.
        /// </summary>
        /// <param name="src">The span</param>
        /// <param name="allocator">NativeArray Allocator</param>
        /// <typeparam name="T">unmanaged type</typeparam>
        /// <returns>A NativeArray</returns>
        public static unsafe NativeArray<T> CopyToNativeArray<T>(this ReadOnlySpan<T> src, Allocator allocator)
            where T : unmanaged
        {
            var dst = new NativeArray<T>(src.Length, allocator, NativeArrayOptions.UninitializedMemory);
            fixed (void* sourcePtr = src)
            {
                UnsafeUtility.MemCpy(
                    dst.GetUnsafePtr(),
                    sourcePtr,
                    src.Length * UnsafeUtility.SizeOf<T>());
            }
            return dst;
        }


        // Convert NativeSlice to ReadOnlySpan
        public static unsafe ReadOnlySpan<T> AsReadOnlySpan<T>(this NativeSlice<T> src)
            where T : unmanaged
        {
            return new ReadOnlySpan<T>(src.GetUnsafePtr(), src.Length);
        }
    }
}
