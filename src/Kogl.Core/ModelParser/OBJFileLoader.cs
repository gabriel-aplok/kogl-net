using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using Kogl.Common;
using Kogl.Common.Types;
using Kogl.Core.Resources;

namespace Kogl.Core.ModelParser;

/// <summary>OBJ file parser</summary>
public static class OBJFileLoader
{
    private readonly record struct VertexKey(int Position, int Normal, int UV);

    private struct VertexIndex
    {
        public int Position;
        public int Normal;
        public int UV;

        public VertexIndex()
        {
            Position = -1;
            Normal = -1;
            UV = -1;
        }
    }

    private class Cluster(int matIndex, int startIndex)
    {
        public int MaterialIndex = matIndex;
        public int StartIndex = startIndex;
        public int IndexCount = 0;
    }

    public static Model Load(string path)
    {
        if (!File.Exists(path))
        {
            LogCat.Error("MODEL", $"Invalid path. OBJ file not found. Path = {path}");
            throw new FileNotFoundException($"OBJ file not found: {path}");
            // return new Model();
        }

        List<Vector3> positionList = [];
        List<Vector3> normalList = [];
        List<Vector2> uvList = [];
        List<VertexIndex> vertexIndexList = [];
        List<Cluster> clusterList = [];
        List<string> materialNames = [];

        foreach (string lineString in File.ReadLines(path))
        {
            ReadOnlySpan<char> line = lineString.AsSpan().Trim();
            if (line.IsEmpty || line[0] == '#')
                continue;

            int firstSpace = line.IndexOf(' ');
            if (firstSpace == -1)
                continue;

            ReadOnlySpan<char> prefix = line[..firstSpace];
            ReadOnlySpan<char> data = line[(firstSpace + 1)..].TrimStart();

            if (prefix.SequenceEqual("v"))
            {
                positionList.Add(ParseVector3(data));
            }
            else if (prefix.SequenceEqual("vn"))
            {
                normalList.Add(ParseVector3(data));
            }
            else if (prefix.SequenceEqual("vt"))
            {
                uvList.Add(ParseVector2(data));
            }
            else if (prefix.SequenceEqual("f"))
            {
                ParseFace(data, vertexIndexList, clusterList);
            }
            else if (prefix.SequenceEqual("usemtl"))
            {
                string materialName = data.ToString();
                int mIdx = materialNames.IndexOf(materialName);
                if (mIdx == -1)
                {
                    mIdx = materialNames.Count;
                    materialNames.Add(materialName);
                }
                clusterList.Add(new Cluster(mIdx, vertexIndexList.Count));
            }
        }

        if (clusterList.Count == 0)
        {
            clusterList.Add(new Cluster(0, 0) { IndexCount = vertexIndexList.Count });
        }
        else
        {
            clusterList[^1].IndexCount = vertexIndexList.Count - clusterList[^1].StartIndex;
        }

        return ConstructGeometry(
            vertexIndexList,
            positionList,
            normalList,
            uvList,
            clusterList,
            materialNames
        );
    }

    private static Model ConstructGeometry(
        List<VertexIndex> vertexIndexList,
        List<Vector3> positions,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<Cluster> clusters,
        List<string> materialNames
    )
    {
        Model model = new();
        List<Mesh> subMeshes = [];
        List<int> meshMatIndices = [];

        // resolve raw global vertices to strip redundancy
        List<VertexData> globalVertices = [];
        List<int> globalIndices = new(vertexIndexList.Count);
        Dictionary<VertexKey, int> vertexCache = [];

        foreach (VertexIndex vIndex in vertexIndexList)
        {
            int posIdx = GetRealIndex(vIndex.Position, positions.Count);
            int normIdx = GetRealIndex(vIndex.Normal, normals.Count);
            int uvIdx = GetRealIndex(vIndex.UV, uvs.Count);

            VertexKey key = new(posIdx, normIdx, uvIdx);
            if (vertexCache.TryGetValue(key, out int cacheIndex))
            {
                globalIndices.Add(cacheIndex);
            }
            else
            {
                VertexData vertex = new(
                    posIdx >= 0 ? positions[posIdx] : Vector3.Zero,
                    uvIdx >= 0 ? uvs[uvIdx] : Vector2.Zero,
                    Vector4.One, // ensure standard visibility
                    normIdx >= 0 ? normals[normIdx] : Vector3.UnitZ,
                    new Vector4(1, 0, 0, 1)
                );

                globalVertices.Add(vertex);
                globalIndices.Add(globalVertices.Count - 1);
                vertexCache[key] = globalVertices.Count - 1;
            }
        }

        // split into distinct GPU sub-meshes per material cluster
        foreach (Cluster cluster in clusters)
        {
            if (cluster.IndexCount == 0)
                continue;

            List<VertexData> subVerts = [];
            List<ushort> subIndices = [];
            Dictionary<int, ushort> globalToLocal = [];

            for (int i = 0; i < cluster.IndexCount; i++)
            {
                int globalIdx = globalIndices[cluster.StartIndex + i];
                if (!globalToLocal.TryGetValue(globalIdx, out ushort localIdx))
                {
                    localIdx = (ushort)subVerts.Count;
                    subVerts.Add(globalVertices[globalIdx]);
                    globalToLocal[globalIdx] = localIdx;
                }
                subIndices.Add(localIdx);
            }

            MeshHandle handle = KoRender
                .GetBackend()
                .CreateMesh(
                    CollectionsMarshal.AsSpan(subVerts),
                    CollectionsMarshal.AsSpan(subIndices)
                );
            subMeshes.Add(new Mesh(handle, subIndices.Count));
            meshMatIndices.Add(cluster.MaterialIndex);
        }

        model.Meshes = [.. subMeshes];
        model.MeshMaterialIndices = [.. meshMatIndices];

        // fallback materials generator
        model.Materials = new Material[Math.Max(1, materialNames.Count)];
        for (int i = 0; i < model.Materials.Length; i++)
        {
            Material mat = KoRender.DefaultMaterial.CreateInstance();
            mat.Name = materialNames.Count > i ? materialNames[i] : "DefaultObjMaterial";
            model.Materials[i] = mat;
        }

        return model;
    }

    private static void ParseFace(
        ReadOnlySpan<char> data,
        List<VertexIndex> vertexIndexList,
        List<Cluster> clusterList
    )
    {
        List<VertexIndex> vIndexList = [];
        while (!data.IsEmpty)
        {
            int nextSpace = data.IndexOf(' ');
            ReadOnlySpan<char> token = nextSpace == -1 ? data : data[..nextSpace];

            if (!token.IsEmpty)
            {
                VertexIndex vIndex = new();
                int slash1 = token.IndexOf('/');

                if (slash1 == -1)
                {
                    vIndex.Position = int.Parse(token) - 1;
                }
                else
                {
                    vIndex.Position = int.Parse(token[..slash1]) - 1;
                    ReadOnlySpan<char> remainder = token[(slash1 + 1)..];
                    int slash2 = remainder.IndexOf('/');

                    if (slash2 == -1) // v/vt
                    {
                        vIndex.UV = int.Parse(remainder) - 1;
                    }
                    else if (slash2 == 0) // v//vn
                    {
                        vIndex.Normal = int.Parse(remainder[1..]) - 1;
                    }
                    else // v/vt/vn
                    {
                        vIndex.UV = int.Parse(remainder[..slash2]) - 1;
                        vIndex.Normal = int.Parse(remainder[(slash2 + 1)..]) - 1;
                    }
                }
                vIndexList.Add(vIndex);
            }

            if (nextSpace == -1)
                break;
            data = data[(nextSpace + 1)..].TrimStart();
        }

        // triangulate n-gons
        for (int i = 1; i < vIndexList.Count - 1; i++)
        {
            vertexIndexList.Add(vIndexList[0]);
            vertexIndexList.Add(vIndexList[i]);
            vertexIndexList.Add(vIndexList[i + 1]);

            if (clusterList.Count > 0)
                clusterList[^1].IndexCount += 3;
        }
    }

    private static Vector3 ParseVector3(ReadOnlySpan<char> span)
    {
        Span<float> v = stackalloc float[3];
        ParseFloats(span, v);
        return new Vector3(v[0], v[1], v[2]);
    }

    private static Vector2 ParseVector2(ReadOnlySpan<char> span)
    {
        Span<float> v = stackalloc float[2];
        ParseFloats(span, v);
        return new Vector2(v[0], v[1]);
    }

    private static void ParseFloats(ReadOnlySpan<char> span, Span<float> results)
    {
        int index = 0;
        while (!span.IsEmpty && index < results.Length)
        {
            int nextSpace = span.IndexOf(' ');
            ReadOnlySpan<char> token = nextSpace == -1 ? span : span[..nextSpace];

            if (!token.IsEmpty)
            {
                float.TryParse(
                    token,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out results[index]
                );
                index++;
            }

            if (nextSpace == -1)
                break;
            span = span[(nextSpace + 1)..].TrimStart();
        }
    }

    private static int GetRealIndex(int index, int maxCount)
    {
        return index < 0 ? maxCount + index : index;
    }
}
