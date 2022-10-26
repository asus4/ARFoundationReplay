using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ARRecorder
{
    [Serializable]
    internal partial class Packet
    {
        public CameraFrameEvent cameraFrame;

        private static readonly BinaryFormatter formatter = new BinaryFormatter();
        private static readonly MemoryStream stream = new MemoryStream();
        private static byte[] buffer = new byte[8192];

        public ReadOnlySpan<byte> Serialize()
        {
            // TODO: Should make custom serialization 
            // instead of using BinaryFormatter?
            lock (stream)
            {
                stream.Position = 0;
                formatter.Serialize(stream, this);
                int length = (int)stream.Position;
                if (buffer.Length < length)
                {
                    buffer = new byte[length];
                }
                var span = new Span<byte>(buffer, 0, length);

                stream.Position = 0;
                stream.Read(span);
                return span;
            }
        }

        public static Packet Deserialize(byte[] data)
        {
            lock (stream)
            {
                stream.Position = 0;
                stream.Write(data, 0, data.Length);
                stream.Position = 0;
                return formatter.Deserialize(stream) as Packet;
            }
        }
    }
}
