using System;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ARFoundationReplay
{
    internal static class SerializeExtensions
    {
        /// <summary>
        /// Convert struct into byte array.
        /// 
        /// The struct must be blittable.
        /// </summary>
        /// <param name="input">A struct</param>
        /// <typeparam name="T">Type of struct</typeparam>
        /// <returns>A byte array</returns>
        public static byte[] ToByteArray<T>(ref this T input)
            where T : struct
        {
            Assert.IsTrue(UnsafeUtility.IsBlittable<T>());

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

        /// <summary>
        /// Convert byte array into struct.
        /// 
        /// The struct must be blittable.
        /// bytes must be the same size of struct.
        /// </summary>
        /// <param name="bytes">A bytes array</param>
        /// <typeparam name="T">The type of the struct</typeparam>
        /// <returns>A struct</returns>
        public static T ToStruct<T>(this byte[] bytes)
            where T : struct
        {
            Assert.IsNotNull(bytes);
            Assert.AreEqual(UnsafeUtility.SizeOf<T>(), bytes.Length);

            int size = Marshal.SizeOf<T>();
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(bytes, 0, ptr, size);
                return (T)Marshal.PtrToStructure(ptr, typeof(T));
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        public unsafe static int CopyToBuffer<T>(this ref T src, byte[] dst, int offset)
            where T : struct
        {
            int length = UnsafeUtility.SizeOf<T>();
            var span = new Span<byte>(dst, offset, length);
            fixed (void* dstPtr = span)
            {
                UnsafeUtility.CopyStructureToPtr(ref src, dstPtr);
            }
            return length;
        }

        public unsafe static int CopyToBuffer<T>(this ref NativeArray<T>.ReadOnly src, byte[] dst, int offset)
            where T : struct
        {
            int length = src.Length * UnsafeUtility.SizeOf<T>();
            var span = new Span<byte>(dst, offset, length);
            fixed (void* dstPtr = span)
            {
                UnsafeUtility.MemCpy(dstPtr, src.GetUnsafeReadOnlyPtr(), length);
            }
            return length;
        }

        public unsafe static int CopyToStruct<T>(this byte[] src, int offset, out T dst)
            where T : struct
        {
            int length = UnsafeUtility.SizeOf<T>();
            var span = new ReadOnlySpan<byte>(src, offset, length);
            fixed (void* srcPtr = span)
            {
                UnsafeUtility.CopyPtrToStructure(srcPtr, out dst);

            }
            return length;
        }

        public unsafe static int CopyToNativeArray<T>(
            this byte[] src, int offset, int structLength, out NativeArray<T> dst, Allocator allocator)
            where T : struct
        {
            dst = new NativeArray<T>(structLength, allocator);
            int length = structLength * UnsafeUtility.SizeOf<T>();
            var span = new ReadOnlySpan<byte>(src, offset, length);
            fixed (void* srcPtr = span)
            {
                UnsafeUtility.MemCpy(dst.GetUnsafePtr(), srcPtr, length);
            }
            return length;
        }
    }
}
