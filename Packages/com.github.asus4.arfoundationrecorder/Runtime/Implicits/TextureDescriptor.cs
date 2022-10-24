using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARSubsystems;

namespace ARFoundationRecorder
{
    /// <summary>
    /// HACK: Need the unsafe struct cast
    /// since XRTextureDescriptor is private struct!
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct TextureDescriptor : IEquatable<TextureDescriptor>
    {
        public IntPtr nativeTexture;
        public int width;
        public int height;
        public int mipmapCount;
        public TextureFormat format;
        public int propertyNameId;
        public int depth;
        public TextureDimension dimension;

        public TextureDescriptor(Texture tex, int propertyNameId)
        {
            nativeTexture = tex.GetNativeTexturePtr();
            width = tex.width;
            height = tex.height;
            mipmapCount = tex.mipmapCount;
            if (tex is Texture2D)
            {
                format = ((Texture2D)tex).format;
            }
            else
            {
                format = TextureFormat.ARGB32;
            }
            this.propertyNameId = propertyNameId;
            depth = 0;
            dimension = tex.dimension;
        }

        public bool Equals(TextureDescriptor other)
        {
            return nativeTexture.Equals(other.nativeTexture)
                && width.Equals(other.width)
                && height.Equals(other.height)
                && mipmapCount.Equals(other.mipmapCount)
                && format.Equals(other.format)
                && propertyNameId.Equals(other.propertyNameId)
                && depth.Equals(other.depth)
                && dimension.Equals(other.dimension);
        }


        [StructLayout(LayoutKind.Explicit)]
        public struct TextureDescriptorUnion
        {
            [FieldOffset(0)] public TextureDescriptor a;
            [FieldOffset(0)] public XRTextureDescriptor b;
        }


        public static implicit operator XRTextureDescriptor(TextureDescriptor d)
        {
            var union = new TextureDescriptorUnion()
            {
                a = d,
            };
            return union.b;
        }

        public static implicit operator TextureDescriptor(XRTextureDescriptor d)
        {
            var union = new TextureDescriptorUnion()
            {
                b = d,
            };
            return union.a;
        }
    }
}
