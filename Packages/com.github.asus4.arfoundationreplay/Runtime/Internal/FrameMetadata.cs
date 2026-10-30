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
        public PointCloudPacket pointCloud;

#if ARCORE_EXTENSIONS_ENABLED
        public GeospatialEarthPacket geospatialEarth;
        public StreetscapeGeometryPacket streetscapeGeometry;
#endif // ARCORE_EXTENSIONS_ENABLED

        private static readonly ArrayBufferWriter<byte> bufferWriter = new(512);
        private static FrameMetadata deserialized = null;

        // Register custom formatter at startup
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void Register()
        {
            MemoryPackFormatterProvider.Register(new MeshFormatter());
        }

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

        /// <summary>
        /// Merge the trackable changes of an earlier frame into this one.
        /// </summary>
        public void MergeEarlier(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                return;
            }
            // Not into the shared instance, which may be this one
            var earlier = MemoryPackSerializer.Deserialize<FrameMetadata>(data);

            if (plane == null)
            {
                plane = earlier.plane;
            }
            else if (earlier.plane != null)
            {
                plane.MergeEarlier(earlier.plane);
            }

            if (pointCloud == null)
            {
                pointCloud = earlier.pointCloud;
            }
            else if (earlier.pointCloud != null)
            {
                pointCloud.MergeEarlier(earlier.pointCloud);
            }

#if ARCORE_EXTENSIONS_ENABLED
            if (streetscapeGeometry == null)
            {
                streetscapeGeometry = earlier.streetscapeGeometry;
            }
            else if (earlier.streetscapeGeometry != null)
            {
                streetscapeGeometry.MergeEarlier(earlier.streetscapeGeometry);
            }
#endif // ARCORE_EXTENSIONS_ENABLED
        }
    }
}
