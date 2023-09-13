using System;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ARFoundationReplay
{
    /// <summary>
    /// Base class to serialize TrackableChanges
    /// </summary>
    /// <typeparam name="T">Struct of trackable</typeparam>
    [Serializable]
    public abstract class TrackableChangesPacket<T>
        where T : struct, ITrackable
    {
        public byte[] added;
        public byte[] updated;
        public byte[] removed;

        public TrackableChangesPacket()
        {
            added = Array.Empty<byte>();
            updated = Array.Empty<byte>();
            removed = Array.Empty<byte>();
        }

        /// <summary>
        /// Check if any changes are available
        /// </summary>
        /// <value>True if available</value>
        public bool IsAvailable
        {
            get
            {
                if (added == null || updated == null || removed == null)
                {
                    return false;
                }
                return added.Length > 0 || updated.Length > 0 || removed.Length > 0;
            }
        }

        /// <summary>
        /// Copy data from TrackableChanges
        /// </summary>
        /// <param name="changes">A TrackableChanges</param>
        public unsafe void CopyFrom(TrackableChanges<T> changes)
        {
            int strideT = UnsafeUtility.SizeOf<T>();
            int strideId = UnsafeUtility.SizeOf<TrackableId>();

            added = new byte[changes.added.Length * strideT];
            updated = new byte[changes.updated.Length * strideT];
            removed = new byte[changes.removed.Length * strideId];

            // get void ptr of each array
            fixed (void* addedPtr = added)
            fixed (void* updatedPtr = updated)
            fixed (void* removedPtr = removed)
            {
                UnsafeUtility.MemCpy(addedPtr, changes.added.GetUnsafeReadOnlyPtr(), added.Length);
                UnsafeUtility.MemCpy(updatedPtr, changes.updated.GetUnsafeReadOnlyPtr(), updated.Length);
                UnsafeUtility.MemCpy(removedPtr, changes.removed.GetUnsafeReadOnlyPtr(), removed.Length);
            }
        }

        /// <summary>
        /// Convert to TrackableChanges
        /// </summary>
        /// <param name="allocator">A lifetime allocator of the TrackableChanges</param>
        /// <returns>A created TrackableChanges</returns>
        public unsafe TrackableChanges<T> AsTrackableChanges(Allocator allocator)
        {
            int strideT = UnsafeUtility.SizeOf<T>();
            int strideId = UnsafeUtility.SizeOf<TrackableId>();

            // get void ptr of each array
            fixed (void* addedPtr = added)
            fixed (void* updatedPtr = updated)
            fixed (void* removedPtr = removed)
            {
                return new TrackableChanges<T>(
                    addedPtr, added.Length / strideT,
                    updatedPtr, updated.Length / strideT,
                    removedPtr, removed.Length / strideId,
                    default, strideT,
                    allocator
                );
            }
        }
    }
}
