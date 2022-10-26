using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ARRecorder
{
    internal static class NativeArrayExtensions
    {
        public static unsafe NativeArray<T> CopyToNativeArray<T>(this ReadOnlySpan<T> src, Allocator allocator)
            where T : unmanaged
        {
            var dst = new NativeArray<T>(src.Length, allocator, NativeArrayOptions.UninitializedMemory);
            fixed (void* sourcePtr = src)
            {
                UnsafeUtility.MemCpy(dst.GetUnsafePtr(), sourcePtr, src.Length * UnsafeUtility.SizeOf<T>());
            }
            return dst;
        }
    }
}
