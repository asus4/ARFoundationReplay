using System;
using UnityEngine;

namespace ARFoundationReplay
{
    public class MetadataPlayer : IDisposable
    {
        private byte[] _buffer;

        /// <summary>
        /// Whether the loaded video file has an audio track.
        /// </summary>
        public bool HasAudioTrack { get; }

        public MetadataPlayer(string path)
        {
            if (!Avfi.LoadMetadata(path))
            {
                Debug.LogWarning($"MetadataPlayer: No metadata track found in {path}");
            }
            HasAudioTrack = Avfi.HasAudioTrack();
            // Get max size of metadata
            uint size = Avfi.GetBufferSize();
            _buffer = new byte[size];
        }

        public void Dispose()
        {
            Avfi.UnloadMetadata();
        }

        public unsafe ReadOnlySpan<byte> PeekMetadata(double time)
        {
            fixed (byte* ptr = _buffer)
            {
                uint size = Avfi.PeekMetadata(time, ptr);
                if (size == 0)
                {
                    return ReadOnlySpan<byte>.Empty;
                }
                return new ReadOnlySpan<byte>(ptr, (int)size);
            }
        }
    }
}
