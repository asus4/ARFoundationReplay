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
        /// Track ID used as a key of track
        /// </summary>
        /// <value>A track ID</value>
        TrackID ID { get; }

        /// <summary>
        /// Initialize and check availability of Subsystem.
        /// </summary>
        /// <param name="origin">An XROrigin</param>
        /// <param name="material">A material for Multiplexing</param>
        /// <returns>Available or not</returns>
        bool Initialize(XROrigin origin, Material muxMaterial);

        /// <summary>
        /// Encode subsystem state into Metadata.
        /// </summary>
        /// <param name="metadata">The encoded metadata</param>
        void Encode(FrameMetadata metadata);

        /// <summary>
        /// Called after Encode() is called for all subsystems.
        /// </summary>
        void PostEncode();
    }
}
