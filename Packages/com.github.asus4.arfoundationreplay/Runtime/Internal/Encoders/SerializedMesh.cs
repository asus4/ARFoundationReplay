using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ARFoundationReplay
{
    internal class SerializedMesh : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Header
        {
            public int vertexCount;
            public int indexCount;
            public int attributeCount;
            public bool isU32Index;
            public int stride;

            public override readonly string ToString()
            {
                return $"vertex: {vertexCount}, index: {indexCount}, attr: {attributeCount}, isU32Index: {isU32Index}, stride: {stride}";
            }
        }

        public static readonly int headerSize = UnsafeUtility.SizeOf<Header>();
        public static readonly int attributeSize = UnsafeUtility.SizeOf<VertexAttributeDescriptor>();

        public Header header;
        public NativeArray<VertexAttributeDescriptor> attributes;
        public NativeArray<byte> indices;
        public NativeArray<byte> vertices;

        public void Dispose()
        {
            attributes.Dispose();
            indices.Dispose();
            vertices.Dispose();
        }

        public int TotalLength()
        {
            return
                headerSize // header
                + attributes.Length * attributeSize // attributes
                + indices.Length // indices
                + vertices.Length; // vertices
        }

        public static SerializedMesh FromMesh(Mesh mesh, Allocator allocator)
        {
            var meshes = Mesh.AcquireReadOnlyMeshData(mesh);
            Assert.IsTrue(meshes.Length > 0);
            Mesh.MeshData meshData = meshes[0];
            // Assert.IsTrue(meshData.subMeshCount == 0, "Sub mesh is not implemented");

            var header = new Header
            {
                vertexCount = mesh.vertexCount,
                indexCount = (int)mesh.GetIndexCount(0),
                attributeCount = mesh.vertexAttributeCount,
                isU32Index = mesh.indexFormat == IndexFormat.UInt32,
                stride = meshData.GetVertexBufferStride(0),
            };

            var attributes = new NativeArray<VertexAttributeDescriptor>(header.attributeCount, allocator);
            for (int i = 0; i < header.attributeCount; i++)
            {
                attributes[i] = mesh.GetVertexAttribute(i);
            }

            var indices = meshData.GetIndexData<byte>();
            var vertices = meshData.GetVertexData<byte>();
            var serialized = new SerializedMesh
            {
                header = header,
                attributes = attributes,
                indices = new NativeArray<byte>(indices, allocator),
                vertices = new NativeArray<byte>(vertices, allocator),
            };
            return serialized;
        }

        public void CopyToMesh(Mesh mesh)
        {
            mesh.SetIndexBufferParams(header.indexCount, header.isU32Index ? IndexFormat.UInt32 : IndexFormat.UInt16);
            mesh.SetIndexBufferData(indices, 0, 0, header.indexCount);
            mesh.SetVertexBufferParams(header.vertexCount, attributes);
            mesh.SetVertexBufferData(vertices, 0, 0, header.vertexCount);
        }
    }

    public static class MeshExtensions
    {
        public unsafe static byte[] ToByteArray(this Mesh mesh)
        {
            using var serialized = SerializedMesh.FromMesh(mesh, Allocator.Temp);

            var buffer = new byte[serialized.TotalLength()];

            int offset = serialized.header.CopyToBuffer(buffer, 0);
            offset += serialized.attributes.CopyToBuffer(buffer, offset);
            offset += serialized.indices.CopyToBuffer(buffer, offset);
            serialized.vertices.CopyToBuffer(buffer, offset);
            return buffer;
        }

        public unsafe static bool UpdateFromBytes(this Mesh mesh, byte[] bytes)
        {
            Assert.IsTrue(bytes.Length >= SerializedMesh.headerSize);
            Allocator allocator = Allocator.Temp;

            int offset = bytes.CopyToStruct(0, out SerializedMesh.Header header);
            offset += bytes.CopyToNativeArray(offset, header.attributeCount, out NativeArray<VertexAttributeDescriptor> attributes, allocator);
            offset += bytes.CopyToNativeArray(offset, header.indexCount * (header.isU32Index ? 4 : 2), out NativeArray<byte> indices, allocator);
            bytes.CopyToNativeArray(offset, header.vertexCount * header.stride, out NativeArray<byte> vertices, allocator);

            using var serialized = new SerializedMesh()
            {
                header = header,
                attributes = attributes,
                indices = indices,
                vertices = vertices,
            };
            serialized.CopyToMesh(mesh);
            return true;
        }
    }
}
