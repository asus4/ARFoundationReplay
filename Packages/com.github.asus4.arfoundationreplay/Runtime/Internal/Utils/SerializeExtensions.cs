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



    }
}
