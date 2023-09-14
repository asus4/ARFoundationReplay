using System;
using System.Collections.Generic;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ARFoundationReplay
{
    using NativeTrackableId = UnityEngine.XR.ARSubsystems.TrackableId;

    /// <summary>
    /// Base class to serialize TrackableChanges
    /// </summary>
    /// <typeparam name="T">Struct of trackable</typeparam>
    [Serializable]
    internal abstract class TrackableChangesPacket<T>
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
        public virtual bool IsAvailable
        {
            get
            {
                return added.Length > 0 || updated.Length > 0 || removed.Length > 0;
            }
        }

        public virtual void Reset()
        {
            if (added.Length > 0)
            {
                added = Array.Empty<byte>();
            }
            if (updated.Length > 0)
            {
                updated = Array.Empty<byte>();
            }
            if (removed.Length > 0)
            {
                removed = Array.Empty<byte>();
            }
        }

        /// <summary>
        /// Copy data from TrackableChanges
        /// </summary>
        /// <param name="changes">A TrackableChanges</param>
        public unsafe void CopyFrom(TrackableChanges<T> changes)
        {
            int strideT = UnsafeUtility.SizeOf<T>();
            int strideId = UnsafeUtility.SizeOf<NativeTrackableId>();

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
        /// Copy data from lists
        /// </summary>
        /// <param name="added">A list of added <typeparamref name="T"/></param>
        /// <param name="updated">A list of updated <typeparamref name="T"/></param>
        /// <param name="removed">A list of removed TrackableId</param>
        public unsafe void CopyFrom(
            IReadOnlyList<T> added,
            IReadOnlyList<T> updated,
            IReadOnlyList<NativeTrackableId> removed)
        {
            using var changes = new TrackableChanges<T>(
                added.Count, updated.Count, removed.Count, Allocator.Temp);

            var nativeAdded = changes.added;
            var nativeUpdated = changes.updated;
            var nativeRemoved = changes.removed;

            for (int i = 0; i < added.Count; i++)
            {
                nativeAdded[i] = added[i];
            }
            for (int i = 0; i < updated.Count; i++)
            {
                nativeUpdated[i] = updated[i];
            }
            for (int i = 0; i < removed.Count; i++)
            {
                nativeRemoved[i] = removed[i];
            }
            CopyFrom(changes);
        }

        /// <summary>
        /// Convert to TrackableChanges
        /// </summary>
        /// <param name="allocator">A lifetime allocator of the TrackableChanges</param>
        /// <returns>A created TrackableChanges</returns>
        public unsafe TrackableChanges<T> AsTrackableChanges(Allocator allocator)
        {
            int strideT = UnsafeUtility.SizeOf<T>();
            int strideId = UnsafeUtility.SizeOf<NativeTrackableId>();

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

        public override string ToString()
        {
            return $"added:{added.Length} updated:{updated.Length} removed:{removed.Length}";
        }
    }
}
