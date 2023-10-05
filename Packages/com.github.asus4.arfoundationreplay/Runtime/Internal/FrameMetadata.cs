using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace ARFoundationReplay
{
    /// <summary>
    /// A metadata encoded each frame into video file.
    /// </summary>
    [Serializable]
    internal sealed class FrameMetadata
    {
        public Dictionary<TrackID, object> tracks = new();

        public bool TryGetObject<T>(TrackID id, out T result)
        {
            if (tracks.TryGetValue(id, out object track))
            {
                result = (T)track;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        public bool TryGetByteStruct<T>(TrackID id, out T result)
            where T : struct
        {
            if (!tracks.TryGetValue(id, out object track))
            {
                result = default;
                return false;
            }
            if (track is not byte[] bytes)
            {
                throw new Exception("track is not byte[]");
            }

            result = bytes.ToStruct<T>();
            return true;
        }

        private static readonly BinaryFormatter formatter = new();
        private static readonly MemoryStream stream = new();
        private static byte[] buffer = new byte[8192];

        public ReadOnlySpan<byte> Serialize()
        {
            lock (stream)
            {
                stream.Position = 0;
                formatter.Serialize(stream, this);
                int length = (int)stream.Position;
                if (buffer.Length < length)
                {
                    buffer = new byte[Mathf.NextPowerOfTwo(length)];
                    Debug.Log($"Max buffer resized: {length}");
                }
                var span = new Span<byte>(buffer, 0, length);

                stream.Position = 0;
                stream.Read(span);
                return span;
            }
        }

        public static FrameMetadata Deserialize(ReadOnlySpan<byte> data)
        {
            lock (stream)
            {
                stream.Position = 0;
                stream.Write(data);
                stream.Position = 0;
                return formatter.Deserialize(stream) as FrameMetadata;
            }
        }
    }
}
