using UnityEngine;

namespace ARFoundationReplay
{
    internal static class DisposeUtil
    {
        public static void Dispose(Material o)
        {
            if (o != null)
            {
                Object.Destroy(o);
            }
        }

        public static void Dispose(RenderTexture o)
        {
            if (o != null)
            {
                o.Release();
                Object.Destroy(o);
            }
        }
    }
}
