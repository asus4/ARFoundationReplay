#if ARCORE_EXTENSIONS_ENABLED

using UnityEngine;
using Google.XR.ARCoreExtensions;
using UnityEngine.XR.ARSubsystems;


namespace ARFoundationReplay
{
    using NativeTrackableId = UnityEngine.XR.ARSubsystems.TrackableId;

    public class ARStreetscapeGeometryWithReplay : ARStreetscapeGeometry
    {
        private StreetscapeGeometry geometry;

        public Mesh Mesh { get; set; }

        public ARStreetscapeGeometryWithReplay(StreetscapeGeometry geometry) : base(geometry.nativePtr)
        {
            this.geometry = geometry;
        }

        public override Mesh mesh => Mesh;

        public override NativeTrackableId trackableId => geometry.trackableId;

        public override Pose pose => geometry.pose;
        public override StreetscapeGeometryType streetscapeGeometryType => geometry.streetscapeGeometryType;
        public override TrackingState trackingState => geometry.trackingState;
        public override StreetscapeGeometryQuality quality => geometry.quality;

        public void UpdateGeometry(StreetscapeGeometry geometry)
        {
            this.geometry = geometry;
        }
    }
}
#endif // ARCORE_EXTENSIONS_ENABLED
