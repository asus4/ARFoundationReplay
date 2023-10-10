using System;
using System.Buffers;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using MemoryPack;

namespace ARFoundationReplay
{
    /// <summary>
    /// A metadata encoded each frame into video file.
    /// </summary>
    [MemoryPackable]
    internal sealed partial class FrameMetadata
    {
        public Pose input;
        public XRCameraFrame camera;
        public PlanePacket plane;

#if ARCORE_EXTENSIONS_ENABLED
        public GeospatialEarthPacket geospatialEarth;
        public StreetscapeGeometryPacket streetscapeGeometry;
#endif // ARCORE_EXTENSIONS_ENABLED

        private static readonly ArrayBufferWriter<byte> bufferWriter = new(512);
        private static FrameMetadata deserialized = null;

        public ReadOnlySpan<byte> Serialize()
        {
            bufferWriter.Clear();
            MemoryPackSerializer.Serialize(bufferWriter, this);
            return bufferWriter.WrittenSpan;
        }

        public static FrameMetadata Deserialize(ReadOnlySpan<byte> data)
        {
            MemoryPackSerializer.Deserialize(data, ref deserialized);
            return deserialized;
        }
    }
}
