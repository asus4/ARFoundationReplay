using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace ARFoundationReplay
{
    [Serializable]
    public partial class Packet
    {
        public CameraFrameEvent cameraFrame;
        public Pose trackedPose;

        private static readonly BinaryFormatter formatter = new();
        private static readonly MemoryStream stream = new();
        private static byte[] buffer = new byte[8192];

        public ReadOnlySpan<byte> Serialize()
        {
            // TODO: Consider using faster serializer
            // instead of using BinaryFormatter?
            // https://github.com/Cysharp/MemoryPack
            // https://docs.unity3d.com/Packages/com.unity.serialization@3.1/manual/index.html

            lock (stream)
            {
                stream.Position = 0;
                formatter.Serialize(stream, this);
                int length = (int)stream.Position;
                if (buffer.Length < length)
                {
                    buffer = new byte[length];
                    Debug.Log($"Packet buffer resized: {length}");
                }
                var span = new Span<byte>(buffer, 0, length);

                stream.Position = 0;
                stream.Read(span);
                return span;
            }
        }

        public static Packet Deserialize(ReadOnlySpan<byte> data)
        {
            lock (stream)
            {
                stream.Position = 0;
                stream.Write(data);
                stream.Position = 0;
                return formatter.Deserialize(stream) as Packet;
            }
        }
    }
}
