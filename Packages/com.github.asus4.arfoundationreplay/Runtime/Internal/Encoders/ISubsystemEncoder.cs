using System;
using UnityEngine;
using Unity.XR.CoreUtils;

namespace ARFoundationReplay
{

    /// <summary>
    /// Base interface that encode subsystem state into binary.
    /// </summary>
    internal interface ISubsystemEncoder : IDisposable
    {
        /// <summary>
        /// Initialize and check availability of Subsystem.
        /// </summary>
        /// <param name="origin">An XROrigin</param>
        /// <param name="material">A material for Multiplexing</param>
        /// <returns>Available or not</returns>
        bool Initialize(XROrigin origin, Material muxMaterial);
        void Encode(FrameMetadata metadata);
    }

    [Serializable]
    public abstract class TrackableChanges<T> where T : struct
    {
        public byte[] added;
        public byte[] updated;
        public byte[] removed;
    }
}
