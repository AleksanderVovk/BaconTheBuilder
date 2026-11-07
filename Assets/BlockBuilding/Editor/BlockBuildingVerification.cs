using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// No test package needed. Run from Tools after editing mesh/order/JSON code.
public static class BlockBuildingVerification
{
    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception("Block Building check failed: " + message);
    }

    private static void Reject(Action action, string message)
    {
        bool failed = false;
        try
        {
            action();
        }
        catch (ArgumentException)
        {
            failed = true;
        }

        Check(failed, message);
    }

    [MenuItem("Tools/Block Building/Verify Data and Mesh")]
    public static void Run()
    {
        var data = ScriptableObject.CreateInstance<BuildSiteData>();
        var mesh = new Mesh();
        try
        {
            data.level = BlockLevelDefinition.Pyramid();
            data.level.Validate();
            Check(data.level.sites[0].blocks.Count == 35, "pyramid has 25+9+1 blocks");
            string json = data.ToJson();
            data.LoadJson(json);
            Check(data.level.sites[0].blocks.Count == 35, "JSON roundtrip");
            Reject(() => data.LoadJson("{\"version\":99}"), "reject unknown version");
            Check(data.level.sites[0].blocks.Count == 35, "failed import preserves previous data");
            var site = data.level.sites[0];
            site.blocks.Add(new BlockCell { position = Vector3Int.zero });
            Reject(() => data.level.Validate(), "reject duplicate cells");
            site.blocks.RemoveAt(site.blocks.Count - 1);
            var order = new List<BlockCell>
            {
                new BlockCell
                {
                    position = new Vector3Int(0, 1, 0),
                    order = -999
                },
                new BlockCell
                {
                    position = new Vector3Int(0, 0, 0),
                    order = 5
                },
                new BlockCell
                {
                    position = new Vector3Int(2, 0, 0),
                    order = 1
                }
            };
            order.Sort(BlockLevelDefinition.Compare);
            Check(order[0].position.x == 2 && order[2].position.y == 1, "layer has priority over order");
            site.blocks.Clear();
            site.blocks.Add(new BlockCell { position = new Vector3Int(0, 0, 0) });
            site.blocks.Add(new BlockCell { position = new Vector3Int(2, 1, 0) });
            data.level.Validate(); // Roof does not require a directly supporting cell.
            var scratch = new BlockMeshUtility();
            var cells = new HashSet<Vector3Int>
            {
                Vector3Int.zero,
                Vector3Int.right
            };
            scratch.Begin();
            foreach (var cell in cells)
                scratch.Cell(cell, 1, cells);
            scratch.Apply(mesh);
            Check(mesh.vertexCount == 40 && mesh.triangles.Length == 60, "two cubes have ten exposed faces");
            Vector3[] vertices = mesh.vertices, normals = mesh.normals;
            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                Check(
                    Vector3.Dot(Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]), normals[a]) > 0,
                    "outward face winding"
                );
            }

            var root = new GameObject("Temporary verification chunks");
            try
            {
                var builder = root.AddComponent<BuildSiteBuilder>();
                builder.chunkSize = 2;
                builder.createColliders = true;
                var edge = new List<BlockCell>
                {
                    new BlockCell
                    {
                        position = new Vector3Int(1, 0, 0)
                    },
                    new BlockCell
                    {
                        position = new Vector3Int(2, 0, 0)
                    }
                };
                builder.Initialize(edge, 1);
                builder.AddBlock(edge[0].position);
                builder.Complete();
                Check(builder.ChunkCount == 2 && builder.BuiltCount == 1, "preallocate chunks");
                Check(!builder.AddBlock(new Vector3Int(0, 0, 1)), "unplanned cell rejected");
                Check(!builder.AddBlock(edge[0].position), "duplicate build rejected");
                builder.AddBlock(edge[1].position);
                builder.Complete();
                int count = 0;
                foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
                    count += filter.sharedMesh.vertexCount;
                Check(count == 40, "faces are removed across chunk boundary");
                foreach (var collider in root.GetComponentsInChildren<MeshCollider>())
                    Check(collider.sharedMesh != null && !collider.convex, "static chunk collider");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            Debug.Log("BLOCK_BUILDING_DATA_MESH_PASS: JSON, invalid imports, duplicates, ordering, overhangs, face winding, internal/cross-chunk culling, colliders.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(data);
            UnityEngine.Object.DestroyImmediate(mesh);
        }
    }
}
