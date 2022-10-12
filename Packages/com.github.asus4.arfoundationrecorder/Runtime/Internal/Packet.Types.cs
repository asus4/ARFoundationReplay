using System;
using Unity.Mathematics;

namespace ARFoundationRecorder
{

    internal partial class Packet
    {
        [Serializable]
        public struct CameraFrameEvent : IEquatable<CameraFrameEvent>
        {
            // public ARLightEstimationData lightEstimation;
            public long timestampNs;
            public float4x4 projectionMatrix;
            public float4x4 displayMatrix;
            // public double? exposureDuration;
            // public float? exposureOffset;

            public bool Equals(CameraFrameEvent o)
            {
                return timestampNs.Equals(o.timestampNs)
                    && projectionMatrix.Equals(o.projectionMatrix)
                    && displayMatrix.Equals(o.displayMatrix);
            }

            public override string ToString()
            {
                return $"[time: {timestampNs}, projection: {projectionMatrix}, display: {displayMatrix}]";
            }
        }
    }
}
