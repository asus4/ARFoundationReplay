namespace ARFoundationReplay
{
    /// <summary>
    /// Track ID used as a key of external track
    /// </summary>
    internal enum TrackID
    {
        // Default tracks
        Camera = 0,
        Input = 1,


        // Optional tracks
        Occlusion = 2,
        Plane = 3,
        Mesh = 4,

        // Extra tracks
        ARCoreGeospatialEarth = 100,
        ARCoreStreetscapeGeometry = 101,
    }
}
