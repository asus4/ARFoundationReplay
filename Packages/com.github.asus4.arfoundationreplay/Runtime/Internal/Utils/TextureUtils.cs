using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace ARFoundationReplay
{
    internal static class TextureUtils
    {
        /// <summary>
        /// Create a RenderTexture for using in ComputeShader
        /// </summary>
        /// <param name="size">Size of texture</param>
        /// <param name="format">Texture format</param>
        /// <returns>RenderTexture</returns>
        public static RenderTexture CreateRWTexture2D(Vector2Int size, RenderTextureFormat format)
        {
            var rt = new RenderTexture(new RenderTextureDescriptor(size.x, size.y, format)
            {
                enableRandomWrite = true,
                useMipMap = false,
            });
            rt.Create();
            return rt;
        }

        public static XRTextureDescriptor ToTextureDescriptor(this Texture tex, int propertyNameId)
        {
            return new XRTextureDescriptor(
                tex.GetNativeTexturePtr(),
                tex.width,
                tex.height,
                tex.mipmapCount,
                tex is Texture2D tex2D ? tex2D.format : TextureFormat.ARGB32,
                propertyNameId,
                tex is RenderTexture rt ? rt.depth : 0,
                tex.dimension);
        }
    }
}
