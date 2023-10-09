using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using Unity.Collections;
using MemoryPack;

namespace ARFoundationReplay
{
    [MemoryPackable]
    internal partial class SerializedMesh : IDisposable
    {
        public byte[] indices;
        public byte[] vertices;
        public byte[] uvs;
        public byte[] normals;
        public byte[] tangents;
        public byte[] colors;

        public void Dispose()
        {
        }

        public static SerializedMesh FromMesh(Mesh mesh)
        {
            var meshes = Mesh.AcquireReadOnlyMeshData(mesh);
            Assert.IsTrue(meshes.Length > 0);
            Mesh.MeshData meshData = meshes[0];
            // Assert.IsTrue(meshData.subMeshCount == 0, "Sub mesh is not implemented");

            Allocator allocator = Allocator.Temp;

            var indices = new NativeArray<ushort>((int)mesh.GetIndexCount(0), allocator);
            meshData.GetIndices(indices, 0);

            int vertexCount = mesh.vertexCount;
            var vertices = new NativeArray<Vector3>(vertexCount, allocator);
            meshData.GetVertices(vertices);

            NativeArray<Vector2> uvs = default;
            if (meshData.HasVertexAttribute(VertexAttribute.TexCoord0))
            {
                uvs = new(vertexCount, allocator);
                meshData.GetUVs(0, uvs);
            }

            NativeArray<Vector3> normals = default;
            if (meshData.HasVertexAttribute(VertexAttribute.Normal))
            {
                normals = new(vertexCount, allocator);
                meshData.GetNormals(normals);

            }

            NativeArray<Vector4> tangents = default;
            if (meshData.HasVertexAttribute(VertexAttribute.Tangent))
            {
                tangents = new(vertexCount, allocator);
                meshData.GetTangents(tangents);
            }

            NativeArray<Color> colors = default;
            if (meshData.HasVertexAttribute(VertexAttribute.Color))
            {
                colors = new(vertexCount, allocator);
                meshData.GetColors(colors);
            }

            var serialized = new SerializedMesh
            {
                indices = indices.ToByteArray(),
                vertices = vertices.ToByteArray(),
                uvs = uvs.IsCreated ? uvs.ToByteArray() : null,
                normals = normals.IsCreated ? normals.ToByteArray() : null,
                tangents = tangents.IsCreated ? tangents.ToByteArray() : null,
                colors = colors.IsCreated ? colors.ToByteArray() : null,
            };
            return serialized;
        }

        public void CopyToMesh(Mesh mesh)
        {
            Allocator allocator = Allocator.Temp;
            mesh.Clear();

            mesh.SetVertices(vertices.AsNativeArray<Vector3>(allocator));
            mesh.SetIndices(indices.AsNativeArray<ushort>(allocator), MeshTopology.Triangles, 0);
            if (uvs != null)
                mesh.SetUVs(0, uvs.AsNativeArray<Vector2>(allocator));
            if (normals != null)
                mesh.SetNormals(normals.AsNativeArray<Vector3>(allocator));
            if (tangents != null)
                mesh.SetTangents(tangents.AsNativeArray<Vector4>(allocator));
            if (colors != null)
                mesh.SetColors(colors.AsNativeArray<Color>(allocator));
        }
    }
}
