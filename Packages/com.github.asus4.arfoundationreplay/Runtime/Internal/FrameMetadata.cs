using System;
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
        public CameraPacket camera;
        public Pose input;
        public PlanePacket plane;
        public MeshPacket mesh;

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
