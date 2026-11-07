using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LegoMapBoundarySetup
{
    private const string Folder = "Assets/BlockBuilding/Generated/MapBoundary";
    private static Mesh cube;
    private static List<CombineInstance>[] groups;
    private static Material[] materials;
    [MenuItem("Tools/Block Building/Setup LEGO Map Walls")]
    public static string Setup()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Exit Play Mode before editing the map.");
        var ground = GameObject.Find("Ground").GetComponent<Renderer>();
        var map = GameObject.Find("Map").transform;
        var previous = map.Find("LegoMapBoundary");
        if (previous != null)
            Object.DestroyImmediate(previous.gameObject);
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/BlockBuilding/Generated", "MapBoundary");
        var root = new GameObject("LegoMapBoundary");
        root.transform.SetParent(map, true);
        var source = ground.sharedMaterial;
        materials = new[]
        {
            Tint(source, "Sandstone", new Color(.91f, .72f, .31f)),
            Tint(source, "SandSeams", new Color(1, .86f, .47f)),
            Tint(source, "GrassEdge", new Color(.16f, .61f, .26f)),
            Tint(source, "GrassTop", new Color(.30f, .87f, .40f))
        };
        var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube = temp.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(temp);
        Bounds b = ground.bounds;
        Wall(root.transform, "West", true, b.min.x - 4, b.min.z, b.max.z, 0);
        Wall(root.transform, "East", true, b.max.x + 4, b.min.z, b.max.z, 1);
        Wall(root.transform, "North", false, b.max.z + 4, b.min.x - 8, b.max.x + 8, 2);
        Wall(root.transform, "South", false, b.min.z - 4, b.min.x - 8, b.max.x + 8, 3);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
        return "Four static combined LEGO walls outside Ground; sandy cliffs, stepped green caps; four mesh colliders.";
    }

    private static void Wall(Transform parent, string name, bool alongZ, float cross, float min, float max, int side)
    {
        groups = new List<CombineInstance>[4];
        for (int m = 0; m < 4; m++)
            groups[m] = new List<CombineInstance>();
        int count = Mathf.CeilToInt((max - min) / 8);
        float length = (max - min) / count;
        int[] heights =
        {
            9,
            12,
            10,
            15,
            13,
            17,
            11,
            14,
            10,
            16,
            13,
            18,
            12,
            15,
            11,
            14,
            10,
            12
        };
        for (int i = 0; i < count; i++)
        {
            float center = min + (i + .5f) * length;
            float h = heights[(i + side * 3) % heights.Length];
            if (side == 2)
                h += 4;
            if (side == 3)
                h = 4 + (i * 3 + 2) % 5;
            // Each tower has several sandy tiers and a thick green LEGO cap.
            int tiers = Mathf.CeilToInt(h / 4);
            for (int tier = 0; tier < tiers; tier++)
            {
                float low = tier * h / tiers, high = (tier + 1) * h / tiers;
                Add(0, alongZ
                    ? new Vector3(cross, (low + high) * .5f, center)
                    : new Vector3(center, (low + high) * .5f, cross), alongZ
                    ? new Vector3(8, high - low, length + .015f)
                    : new Vector3(length + .015f, high - low, 8));
                if (tier > 0)
                    Add(1, alongZ
                        ? new Vector3(cross, low + .05f, center)
                        : new Vector3(center, low + .05f, cross), alongZ
                        ? new Vector3(8.05f, .10f, length + .02f)
                        : new Vector3(length + .02f, .10f, 8.05f));
            }

            Add(2, alongZ
                ? new Vector3(cross, h + .35f, center)
                : new Vector3(center, h + .35f, cross), alongZ
                ? new Vector3(8.65f, .70f, length + .06f)
                : new Vector3(length + .06f, .70f, 8.65f));
            Add(3, alongZ
                ? new Vector3(cross, h + .78f, center)
                : new Vector3(center, h + .78f, cross), alongZ
                ? new Vector3(8.65f, .16f, length + .06f)
                : new Vector3(length + .06f, .16f, 8.65f));
            // An occasional lower grass shelf breaks up the vertical outline.
            if (i % 3 == 1 && side != 3)
            {
                float inward = side == 0 || side == 3 ? 1 : -1;
                float shelfCross = cross + inward * 2.9f;
                Add(0, alongZ
                    ? new Vector3(shelfCross, h * .32f, center)
                    : new Vector3(center, h * .32f, shelfCross), alongZ
                    ? new Vector3(2.4f, h * .64f, length * .56f)
                    : new Vector3(length * .56f, h * .64f, 2.4f));
                Add(2, alongZ
                    ? new Vector3(shelfCross, h * .64f + .22f, center)
                    : new Vector3(center, h * .64f + .22f, shelfCross), alongZ
                    ? new Vector3(2.7f, .44f, length * .6f)
                    : new Vector3(length * .6f, .44f, 2.7f));
                Add(3, alongZ
                    ? new Vector3(shelfCross, h * .64f + .48f, center)
                    : new Vector3(center, h * .64f + .48f, shelfCross), alongZ
                    ? new Vector3(2.7f, .08f, length * .6f)
                    : new Vector3(length * .6f, .08f, 2.7f));
            }
        }

        var merged = new CombineInstance[4];
        var intermediates = new Mesh[4];
        for (int m = 0; m < 4; m++)
        {
            intermediates[m] = new Mesh();
            intermediates[m].CombineMeshes(groups[m].ToArray(), true, true);
            merged[m] = new CombineInstance
            {
                mesh = intermediates[m],
                transform = Matrix4x4.identity
            };
        }

        var mesh = new Mesh
        {
            name = "LegoBoundary_" + name
        };
        mesh.CombineMeshes(merged, false, true);
        foreach (var part in intermediates)
            Object.DestroyImmediate(part);
        string path = Folder + "/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null)
            AssetDatabase.CreateAsset(mesh, path);
        else
        {
            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            mesh = existing;
            EditorUtility.SetDirty(mesh);
        }

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.isStatic = true;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = materials;
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    private static void Add(int material, Vector3 position, Vector3 scale)
    {
        groups[material].Add(new CombineInstance { mesh = cube, transform = Matrix4x4.TRS(position, Quaternion.identity, scale) });
    }

    private static Material Tint(Material source, string name, Color color)
    {
        string path = Folder + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(source);
            AssetDatabase.CreateAsset(m, path);
        }

        m.SetColor("_Color", color);
        m.SetColor("_BaseColor", color);
        m.SetFloat("_IsDynamic", 0);
        m.SetFloat("_Scale", .5f);
        EditorUtility.SetDirty(m);
        return m;
    }
}
