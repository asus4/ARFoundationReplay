using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace ARFoundationReplay
{
    internal static class NativeArrayExtensions
    {
        public static byte[] ToByteArray<T>(in this NativeSlice<T> arr)
            where T : unmanaged
        {
            var slice = arr.SliceConvert<byte>();
            var bytes = new byte[slice.Length];
            slice.CopyTo(bytes);
            return bytes;
        }

        public static byte[] ToByteArray<T>(in this NativeArray<T> arr)
            where T : unmanaged
        {
            return ToByteArray(new NativeSlice<T>(arr));
        }

        public static NativeArray<T> AsNativeArray<T>(this byte[] bytes, Allocator allocator)
            where T : unmanaged
        {
            int stride = UnsafeUtility.SizeOf<T>();
            Assert.AreEqual(0, bytes.Length % stride);

            var arr = new NativeArray<T>(bytes.Length / stride, allocator);

            NativeSlice<byte> slice = new NativeSlice<T>(arr).SliceConvert<byte>();
            slice.CopyFrom(bytes);
            return arr;
        }

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
            fixed (void* srcPtr = src)
            {
                UnsafeUtility.MemCpy(
                    dst.GetUnsafePtr(),
                    srcPtr,
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
