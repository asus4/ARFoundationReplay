using System;
using UnityEngine;
using Unity.XR.CoreUtils;

namespace ARFoundationReplay
{

    /// <summary>
    /// Base interface for all encoders.
    /// </summary>
    internal interface IEncoder : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="origin">An XROrigin</param>
        /// <param name="packet">A packet to be encoded</param>
        /// <param name="material"></param>
        /// <returns>Available or not</returns>
        bool Initialize(XROrigin origin, Packet packet, Material material);
        void Update();
    }

    [Serializable]
    public abstract class TrackableChanges<T> where T : struct
    {
        public byte[] added;
        public byte[] updated;
        public byte[] removed;
    }
}
