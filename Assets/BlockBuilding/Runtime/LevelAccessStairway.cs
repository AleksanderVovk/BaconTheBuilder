using System.Collections.Generic;
using UnityEngine;

// Permanent access infrastructure: one shared mesh/collider, no per-step objects.
public class LevelAccessStairway : MonoBehaviour
{
    [Header("Steps"), Tooltip("Step rise, below the existing CharacterController step offset."), Range(0.1f, 0.3f)]
    public float rise = 0.2f;

    [Header("Tread"), Tooltip("Depth of a step."), Min(0.25f)]
    public float tread = 0.5f;

    [Header("Width"), Tooltip("Walkable lane width."), Min(1.5f)]
    public float width = 2.5f;

    private Mesh mesh;
    private Vector3[] cubeVertices, cubeNormals;
    private Vector2[] cubeUV;
    private int[] cubeTriangles;
    public float TopHeight { get; private set; }

    public void Configure(BuildLevelEntry entry, Material material, int layer)
    {
        gameObject.SetActive(entry.stairs);
        if (!entry.stairs)
            return;
        transform.position = entry.stairOrigin;
        gameObject.layer = layer;
        TopHeight = entry.stairHeight;
        var filter = GetComponent<MeshFilter>();
        if (filter == null)
            filter = gameObject.AddComponent<MeshFilter>();
        var renderer = GetComponent<MeshRenderer>();
        if (renderer == null)
            renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        var collider = GetComponent<MeshCollider>();
        if (collider == null)
            collider = gameObject.AddComponent<MeshCollider>();
        collider.sharedMesh = null;
        if (mesh == null)
            mesh = new Mesh
            {
                name = "Switchback stairs",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt16
            };
        // Two stacked lanes. Each flight rises 3m, leaving generous headroom.
        if (cubeVertices == null)
        {
            var scratch = new BlockMeshUtility();
            var cube = new Mesh();
            scratch.Begin();
            scratch.Cell(Vector3Int.zero, 1, null, BlockType.StoneBricks);
            scratch.Apply(cube);
            cubeVertices = cube.vertices;
            cubeNormals = cube.normals;
            cubeUV = cube.uv;
            cubeTriangles = cube.triangles;
            if (Application.isPlaying)
                Destroy(cube);
            else
                DestroyImmediate(cube);
        }

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uv = new List<Vector2>();
        var colors = new List<Color32>();
        var triangles = new List<int>();
        int steps = Mathf.CeilToInt(TopHeight / rise);
        const int perRun = 15;
        for (int i = 0; i < steps; i++)
        {
            int run = i / perRun, at = i % perRun;
            bool forward = run % 2 == 0;
            float z = (forward ? at : perRun - 1 - at) * tread;
            Box(new Vector3(forward
                ? 0
                : width, (i + 1) * rise - 0.18f, z), new Vector3(width, 0.18f, tread), vertices, normals, uv, colors, triangles);
            if (at == perRun - 1)
                Box(new Vector3(0, (i + 1) * rise - 0.18f, forward
                    ? perRun * tread
                    : -width), new Vector3(width * 2, 0.18f, width), vertices, normals, uv, colors, triangles);
        }

        // Foot of the staircase and bridge to the skyscraper roof (54.05m).
        Box(new Vector3(0, -0.18f, -width), new Vector3(width, 0.18f, width), vertices, normals, uv, colors, triangles);
        if (TopHeight >= 54)
            Box(new Vector3(-4.5f, 53.85f, -0.5f), new Vector3(4.5f, 0.2f, 2), vertices, normals, uv, colors, triangles);
        // Four outer posts support the stacked flights without obstructing them.
        foreach (float x in new[]
        {
            -0.25f,
            width * 2
        }

        )
            foreach (float z in new[]
            {
                -width - 0.25f,
                perRun * tread + width
            }

            )
                Box(new Vector3(x, 0, z), new Vector3(0.25f, TopHeight, 0.25f), vertices, normals, uv, colors, triangles);
        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uv);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        filter.sharedMesh = mesh;
        collider.sharedMesh = mesh;
    }

    private void Box(Vector3 min, Vector3 size, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<Color32> c, List<int> t)
    {
        int start = v.Count;
        for (int i = 0; i < cubeVertices.Length; i++)
        {
            v.Add(min + Vector3.Scale(cubeVertices[i], size));
            n.Add(cubeNormals[i]);
            uv.Add(cubeUV[i]);
            c.Add(new Color32(210, 220, 240, 255));
        }

        foreach (int index in cubeTriangles)
            t.Add(start + index);
    }

    private void OnDestroy()
    {
        if (mesh != null)
        {
            if (Application.isPlaying)
                Destroy(mesh);
            else
                DestroyImmediate(mesh);
        }
    }
}
