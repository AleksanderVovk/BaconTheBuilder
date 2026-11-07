using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BuildSiteBuilder : MonoBehaviour
{
    [Header("Rendering"), Tooltip("Shared material for all chunks.")]
    public Material material;

    [Header("Chunks"), Tooltip("Cells per chunk edge; max 8 keeps 16-bit mesh indices on WebGL."), Range(2, 8)]
    public int chunkSize = 8;

    [Header("Physics"), Tooltip("Generate one static, nonconvex MeshCollider per nonempty chunk.")]
    public bool createColliders = true;

    [Header("Cooking"), Tooltip("Minimum seconds between batched collider updates. Final completion flushes immediately."), Min(0.05f)]
    public float colliderInterval = 0.35f;
    private sealed class Chunk
    {
        internal readonly List<Vector3Int> cells = new List<Vector3Int>();
        internal Mesh visual, collision;
        internal MeshCollider collider;
        internal bool colliderDirty;
    }

    private readonly Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();
    private readonly Dictionary<Vector3Int, BlockType> types = new Dictionary<Vector3Int, BlockType>();
    private readonly HashSet<Vector3Int> built = new HashSet<Vector3Int>();
    private readonly HashSet<Vector3Int> dirty = new HashSet<Vector3Int>();
    private readonly BlockMeshUtility scratch = new BlockMeshUtility();
    private float size = 1, nextCook;
    public int BuiltCount
    {
        get
        {
            return built.Count;
        }
    }

    // Data queries only; no extra colliders or GameObjects per cell.
    public Bounds CellWorldBounds(Vector3Int cell)
    {
        Vector3 half = Vector3.one * (size * 0.5f);
        Vector3 x = transform.TransformVector(new Vector3(half.x, 0, 0));
        Vector3 y = transform.TransformVector(new Vector3(0, half.y, 0));
        Vector3 z = transform.TransformVector(new Vector3(0, 0, half.z));
        Vector3 extents = new Vector3(
            Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
            Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y),
            Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z)
        );
        return new Bounds(transform.TransformPoint(((Vector3)cell + Vector3.one * 0.5f) * size), extents * 2);
    }

    public float HighestBuiltTop(Vector3 center, float radius)
    {
        float top = float.NegativeInfinity;
        foreach (var cell in built)
        {
            Bounds b = CellWorldBounds(cell);
            float dx = Mathf.Max(b.min.x - center.x, Mathf.Max(0, center.x - b.max.x));
            float dz = Mathf.Max(b.min.z - center.z, Mathf.Max(0, center.z - b.max.z));
            if (dx * dx + dz * dz <= radius * radius)
                top = Mathf.Max(top, b.max.y);
        }

        return top;
    }

    // Query the built-cell set, independent of delayed MeshCollider cooking.
    public bool OverlapsBuiltBlocks(Bounds worldBounds)
    {
        if (built.Count == 0)
            return false;
        Vector3 e = worldBounds.extents;
        Vector3 a = transform.InverseTransformVector(new Vector3(e.x, 0, 0));
        Vector3 b = transform.InverseTransformVector(new Vector3(0, e.y, 0));
        Vector3 c = transform.InverseTransformVector(new Vector3(0, 0, e.z));
        Vector3 localExtents = new Vector3(
            Mathf.Abs(a.x) + Mathf.Abs(b.x) + Mathf.Abs(c.x),
            Mathf.Abs(a.y) + Mathf.Abs(b.y) + Mathf.Abs(c.y),
            Mathf.Abs(a.z) + Mathf.Abs(b.z) + Mathf.Abs(c.z)
        );
        Vector3 center = transform.InverseTransformPoint(worldBounds.center);
        Vector3 min = (center - localExtents) / size, max = (center + localExtents) / size;
        Vector3Int first = Vector3Int.FloorToInt(min), last = Vector3Int.FloorToInt(max);
        double nx = (double)last.x - first.x + 1, ny = (double)last.y - first.y + 1, nz = (double)last.z - first.z + 1;
        if (nx <= 0 || ny <= 0 || nz <= 0)
            return false;
        if (nx * ny * nz > 256)
        {
            // Bound work even if an unusually large player scale spans many empty cells.
            foreach (var cell in built)
                if (CellWorldBounds(cell).Intersects(worldBounds))
                    return true;
            return false;
        }

        for (int x = 0; x < (int)nx; x++)
            for (int y = 0; y < (int)ny; y++)
                for (int z = 0; z < (int)nz; z++)
                {
                    var cell = new Vector3Int(first.x + x, first.y + y, first.z + z);
                    if (built.Contains(cell) && CellWorldBounds(cell).Intersects(worldBounds))
                        return true;
                }

        return false;
    }

    public int ChunkCount
    {
        get
        {
            return chunks.Count;
        }
    }

    public void Initialize(IList<BlockCell> cells, float blockSize)
    {
        size = blockSize;
        chunkSize = Mathf.Clamp(chunkSize, 2, 8);
        foreach (var cell in cells)
        {
            types[cell.position] = cell.type;
            Vector3Int key = Key(cell.position);
            if (!chunks.ContainsKey(key))
            {
                var go = new GameObject("Chunk_" + key.x + "_" + key.y + "_" + key.z);
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
                var chunk = new Chunk
                {
                    visual = new Mesh
                    {
                        name = "Block visual"
                    },
                    collision = new Mesh
                    {
                        name = "Block collision"
                    }
                };
                chunk.visual.MarkDynamic();
                chunk.collision.MarkDynamic();
                go.AddComponent<MeshFilter>().sharedMesh = chunk.visual;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                if (createColliders)
                    chunk.collider = go.AddComponent<MeshCollider>();
                chunks.Add(key, chunk);
            }
        }
    }

    private Vector3Int Key(Vector3Int p)
    {
        return new Vector3Int(
            Mathf.FloorToInt((float)p.x / chunkSize),
            Mathf.FloorToInt((float)p.y / chunkSize),
            Mathf.FloorToInt((float)p.z / chunkSize)
        );
    }

    public bool AddBlock(Vector3Int p)
    {
        if (!types.ContainsKey(p) || !chunks.ContainsKey(Key(p)) || !built.Add(p))
            return false;
        chunks[Key(p)].cells.Add(p);
        dirty.Add(Key(p));
        for (int i = 0; i < 6; i++)
        {
            Vector3Int key = Key(p + BlockMeshUtility.Neighbours[i]);
            if (chunks.ContainsKey(key))
                dirty.Add(key);
        }

        return true;
    }

    private void LateUpdate()
    {
        FlushVisuals();
        if (Time.time >= nextCook)
        {
            FlushColliders();
            nextCook = Time.time + colliderInterval;
        }
    }

    public void FlushVisuals()
    {
        foreach (var key in dirty)
        {
            var chunk = chunks[key];
            scratch.Begin();
            foreach (var p in chunk.cells)
                scratch.Cell(p, size, built, types[p], types);
            scratch.Apply(chunk.visual);
            chunk.colliderDirty = true;
        }

        dirty.Clear();
    }

    public void FlushColliders()
    {
        foreach (var pair in chunks)
        {
            var chunk = pair.Value;
            if (!chunk.colliderDirty || chunk.collider == null)
                continue;
            // Separate meshes keep collider geometry stable between scheduled cooks.
            chunk.collider.sharedMesh = null;
            scratch.Begin();
            foreach (var p in chunk.cells)
                scratch.Cell(p, size, built, types[p], types);
            scratch.Apply(chunk.collision);
            if (chunk.collision.vertexCount > 0)
                chunk.collider.sharedMesh = chunk.collision;
            chunk.colliderDirty = false;
        }
    }

    public void Complete()
    {
        FlushVisuals();
        FlushColliders();
    }

    private static void Release(Mesh mesh)
    {
        if (Application.isPlaying)
            Destroy(mesh);
        else
            DestroyImmediate(mesh);
    }

    private void OnDestroy()
    {
        foreach (var pair in chunks)
        {
            Release(pair.Value.visual);
            Release(pair.Value.collision);
        }
    }
}
