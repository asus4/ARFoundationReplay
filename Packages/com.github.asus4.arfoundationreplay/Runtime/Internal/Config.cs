using UnityEngine;

namespace ARFoundationReplay
{
    internal static class Config
    {
        public const string VERSION = "0.1.0";

        // TODO: Make configurable
        public static readonly Vector2Int RecordResolution = new(1920, 1080);
        public static readonly Vector2 DepthRange = new(0.2f, 10f);
    }
}
