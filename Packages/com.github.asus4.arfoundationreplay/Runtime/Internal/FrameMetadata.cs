using System;
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

        // TODO: implement IMemoryStream interface
        private static byte[] buffer = new byte[8192];
        private static FrameMetadata deserialized = null;

        public ReadOnlySpan<byte> Serialize()
        {
            buffer = MemoryPackSerializer.Serialize(this);
            return new ReadOnlySpan<byte>(buffer);
        }

        public static FrameMetadata Deserialize(ReadOnlySpan<byte> data)
        {
            MemoryPackSerializer.Deserialize<FrameMetadata>(data, ref deserialized);
            return deserialized;
        }
    }
}
