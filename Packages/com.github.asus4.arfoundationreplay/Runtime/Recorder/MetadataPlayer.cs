using System;
using System.IO;
using UnityEngine;

namespace ARFoundationReplay
{
    public sealed class MetadataPlayer : IDisposable
    {
        // Keep in sync with kMETADATA_ID_RAW in Avfi.m
        private const string kMetadataIdentifier = "mdta/com.github.asus4.avfi.raw";

        private FileStream _file;
        private MetadataTrack _track;
        private readonly byte[] _buffer;

        /// <summary>
        /// Whether the loaded video file has an audio track.
        /// </summary>
        public bool HasAudioTrack { get; }

        public MetadataPlayer(string path)
        {
            if (File.Exists(path))
            {
                _file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                _track = MetadataTrack.Read(_file, kMetadataIdentifier, out bool hasAudioTrack);
                HasAudioTrack = hasAudioTrack;
            }
            if (_track == null)
            {
                Debug.LogWarning($"MetadataPlayer: No metadata track found in {path}");
                return;
            }
            _buffer = new byte[_track.MaxSampleSize];
        }

        public void Dispose()
        {
            _file?.Dispose();
            _file = null;
            _track = null;
        }

        /// <param name="frame">The index of the video frame, which has a metadata sample of the same index</param>
        /// <returns>The metadata, valid until the next read, or empty if there is none</returns>
        public ReadOnlySpan<byte> ReadFrame(int frame)
        {
            if (_track == null)
            {
                return ReadOnlySpan<byte>.Empty;
            }
            return _track.ReadSample(_file, frame, _buffer);
        }
    }
}
