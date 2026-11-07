using System.Collections.Generic;
using UnityEngine;

// Reusable scratch buffers. No per-face arrays or LINQ during rebuilds.
public sealed class BlockMeshUtility
{
    public static readonly Vector3Int[] Neighbours =
    {
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.up,
        Vector3Int.down,
        Vector3Int.forward,
        Vector3Int.back
    };
    private static readonly Vector3[] U =
    {
        Vector3.back,
        Vector3.forward,
        Vector3.right,
        Vector3.right,
        Vector3.right,
        Vector3.left
    };
    private static readonly Vector3[] V =
    {
        Vector3.up,
        Vector3.up,
        Vector3.back,
        Vector3.forward,
        Vector3.up,
        Vector3.up
    };
    private readonly List<Vector3> vertices = new List<Vector3>(4096);
    private readonly List<Vector3> normals = new List<Vector3>(4096);
    private readonly List<Vector2> uv = new List<Vector2>(4096);
    private readonly List<Vector2> localUV = new List<Vector2>(4096);
    private readonly List<Color32> colors = new List<Color32>(4096);
    private readonly List<int> triangles = new List<int>(6144);
    public void Begin()
    {
        vertices.Clear();
        normals.Clear();
        uv.Clear();
        localUV.Clear();
        colors.Clear();
        triangles.Clear();
    }

    public void Cell(Vector3Int cell, float size, HashSet<Vector3Int> occupied, BlockType type = BlockType.Cobblestone, Dictionary<Vector3Int, BlockType> types = null)
    {
        Vector3 center = ((Vector3)cell + Vector3.one * 0.5f) * size;
        for (int f = 0; f < 6; f++)
        {
            Vector3Int neighbour = cell + Neighbours[f];
            if (occupied != null && occupied.Contains(neighbour))
            {
                BlockType other = BlockType.Cobblestone;
                if (types != null)
                    types.TryGetValue(neighbour, out other);
                if (BlockPalette.HidesFace(type, other))
                    continue;
            }

            Vector3 n = Neighbours[f];
            Vector3 c = center + n * size * 0.5f;
            Vector3 u = U[f] * size * 0.5f, v = V[f] * size * 0.5f;
            int index = vertices.Count;
            vertices.Add(c - u - v);
            vertices.Add(c + u - v);
            vertices.Add(c + u + v);
            vertices.Add(c - u + v);
            for (int i = 0; i < 4; i++)
                normals.Add(n);
            Rect tile = BlockPalette.TileUV(BlockPalette.Tile(type, f));
            uv.Add(new Vector2(tile.xMin, tile.yMin));
            uv.Add(new Vector2(tile.xMax, tile.yMin));
            uv.Add(new Vector2(tile.xMax, tile.yMax));
            uv.Add(new Vector2(tile.xMin, tile.yMax));
            localUV.Add(Vector2.zero);
            localUV.Add(Vector2.right);
            localUV.Add(Vector2.one);
            localUV.Add(Vector2.up);
            Color32 flag = BlockPalette.Tint(type);
            for (int i = 0; i < 4; i++)
                colors.Add(flag);
            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
            triangles.Add(index);
            triangles.Add(index + 2);
            triangles.Add(index + 3);
        }
    }

    public void Apply(Mesh mesh)
    {
        mesh.Clear();
        // A worst-case 8^3 chunk is <=12288 vertices; portable UInt16 indices.
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uv);
        mesh.SetUVs(1, localUV);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
    }
}
