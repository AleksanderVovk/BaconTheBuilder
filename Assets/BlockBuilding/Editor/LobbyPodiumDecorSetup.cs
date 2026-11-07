using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class LobbyPodiumDecorSetup
{
    private const string Folder = "Assets/BlockBuilding/Generated/PodiumDecor";
    private static Mesh cube;
    private static List<CombineInstance>[] parts;
    private static Transform root;
    private static Vector3 portal;
    private static readonly string[] Names =
    {
        "Paving",
        "DarkBorder",
        "GymFloor",
        "GoldAccent",
        "Grass",
        "Wood",
        "Metal",
        "LampGlow",
        "Flowers"
    };
    [MenuItem("Tools/Block Building/Decorate Existing Podium")]
    public static string Setup()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Stop Play Mode before editing the podium.");
        var session = Object.FindFirstObjectByType<BuildLevelSession>();
        var map = GameObject.Find("Map").transform;
        portal = Object.FindFirstObjectByType<LevelMenuPortal>().transform.position;
        var old = map.Find("PodiumDecor");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);
        root = new GameObject("PodiumDecor").transform;
        root.SetParent(map, true);
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/BlockBuilding/Generated", "PodiumDecor");
        var prototype = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube = prototype.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(prototype);
        parts = new List<CombineInstance>[Names.Length];
        for (int i = 0; i < parts.Length; i++)
            parts[i] = new List<CombineInstance>();
        // Surfaces sit just above the existing podium; collision remains on the podium itself.
        Box(1, new Vector3(24.65f, .148f, -4.75f), new Vector3(8.6f, .045f, 38.5f));
        Box(2, new Vector3(24.65f, .175f, -4.75f), new Vector3(8.15f, .025f, 38.05f));
        for (int i = 0; i < 6; i++)
        {
            float z = 11.5f - i * 6.5f;
            Box(3, new Vector3(21, .198f, z), new Vector3(.13f, .025f, 3.2f));
            if (i < 5)
                Box(1, new Vector3(24.65f, .195f, z - 3.25f), new Vector3(7.8f, .02f, .10f));
        }

        // A compact landing tile marks the spawn without blocking the character.
        Box(1, new Vector3(0, .195f, 0), new Vector3(4, .04f, 4));
        Box(0, new Vector3(0, .22f, 0), new Vector3(3.7f, .025f, 3.7f));
        Box(3, new Vector3(0, .24f, 0), new Vector3(.85f, .015f, .85f), 45);
        // A neat base around the supply pile keeps the walkway distinct from the blocks.
        Box(1, new Vector3(0, .15f, 5), new Vector3(4.1f, .035f, 4.1f));
        Box(3, new Vector3(0, .174f, 5), new Vector3(3.85f, .018f, 3.85f));
        Bench(new Vector3(-22, .125f, -7), 90);
        Bench(new Vector3(-9, .125f, -20), 0);
        Bench(new Vector3(8, .125f, -20), 0);
        Planter(new Vector3(-26, .125f, -20), 0);
        Planter(new Vector3(-26, .125f, -11), 1);
        Planter(new Vector3(-26, .125f, 12), 2);
        Planter(new Vector3(-13, .125f, 11.5f), 3);
        Planter(new Vector3(12, .125f, -21), 4);
        Planter(new Vector3(13, .125f, 11.5f), 5);
        Lamp(new Vector3(-28, .125f, -22));
        Lamp(new Vector3(-28, .125f, 12));
        Lamp(new Vector3(-15, .125f, -22));
        Lamp(new Vector3(14, .125f, -22));
        Lamp(new Vector3(15, .125f, 12));
        var source = GameObject.Find("Ground").GetComponent<Renderer>().sharedMaterial;
        Color[] colors =
        {
            new Color(.68f, .78f, .79f),
            new Color(.13f, .23f, .30f),
            new Color(.24f, .38f, .44f),
            new Color(1, .76f, .25f),
            new Color(.27f, .76f, .34f),
            new Color(.64f, .34f, .17f),
            new Color(.12f, .20f, .27f),
            new Color(.78f, .96f, 1),
            new Color(1, .43f, .56f)
        };
        int vertices = 0;
        for (int i = 0; i < parts.Length; i++)
        {
            string matPath = Folder + "/" + Names[i] + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (material == null)
            {
                material = new Material(i == 7 ? Shader.Find("Universal Render Pipeline/Lit") : source.shader);
                if (i != 7)
                    material.CopyPropertiesFromMaterial(source);
                AssetDatabase.CreateAsset(material, matPath);
            }

            material.SetColor("_Color", colors[i]);
            material.SetColor("_BaseColor", colors[i]);
            if (i != 7)
            {
                material.SetFloat("_IsDynamic", 0);
                material.SetFloat("_Scale", .5f);
            }
            else
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", colors[i] * 1.8f);
            }

            EditorUtility.SetDirty(material);
            var mesh = new Mesh
            {
                name = "PodiumDecor_" + Names[i]
            };
            mesh.CombineMeshes(parts[i].ToArray(), true, true);
            string meshPath = Folder + "/" + Names[i] + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing == null)
            {
                existing = mesh;
                AssetDatabase.CreateAsset(existing, meshPath);
            }
            else
            {
                existing.Clear();
                existing.vertices = mesh.vertices;
                existing.normals = mesh.normals;
                existing.uv = mesh.uv;
                existing.triangles = mesh.triangles;
                existing.RecalculateBounds();
                existing.UploadMeshData(false);
                Object.DestroyImmediate(mesh);
                EditorUtility.SetDirty(existing);
            }

            vertices += existing.vertexCount;
            var child = new GameObject(Names[i]);
            child.transform.SetParent(root, false);
            child.isStatic = true;
            child.AddComponent<MeshFilter>().sharedMesh = existing;
            child.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        var path = session.runtime.path;
        var surfaces = new List<Renderer>();
        foreach (var surface in path.groundSurfaces)
            if (surface != null)
                surfaces.Add(surface);
        surfaces.Add(root.Find("Paving").GetComponent<Renderer>());
        surfaces.Add(root.Find("GymFloor").GetComponent<Renderer>());
        path.groundSurfaces = surfaces.ToArray();
        EditorUtility.SetDirty(path);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        EditorSceneManager.SaveScene(root.gameObject.scene);
        return "Podium decorated: gym floor, 3 benches, 6 planters, 5 lanterns. Combined vertices=" + vertices;
    }

    private static void Path(Vector3 a, Vector3 b, float width)
    {
        Vector3 delta = b - a;
        float length = delta.magnitude;
        if (length < .1f)
            return;
        float yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
        Vector3 center = (a + b) * .5f;
        Box(1, new Vector3(center.x, .153f, center.z), new Vector3(width + .3f, .045f, length + .2f), yaw);
        int count = Mathf.CeilToInt(length / 1.8f);
        float step = length / count;
        for (int i = 0; i < count; i++)
        {
            Vector3 p = Vector3.Lerp(a, b, (i + .5f) / count);
            Box(0, new Vector3(p.x, .184f, p.z), new Vector3(width, .025f, step - .06f), yaw);
        }
    }

    private static bool NearPortal(Vector3 p, float distance)
    {
        Vector3 d = p - portal;
        return d.x * d.x + d.z * d.z < distance * distance;
    }

    private static void Bench(Vector3 p, float yaw)
    {
        if (NearPortal(p, 7))
            return;
        var rotation = Quaternion.Euler(0, yaw, 0);
        Local(6, p, rotation, new Vector3(-1.2f, .42f, 0), new Vector3(.22f, .84f, .85f));
        Local(6, p, rotation, new Vector3(1.2f, .42f, 0), new Vector3(.22f, .84f, .85f));
        for (int i = 0; i < 3; i++)
            Local(5, p, rotation, new Vector3(0, .88f, -.32f + i * .32f), new Vector3(3.3f, .18f, .27f));
        for (int i = 0; i < 2; i++)
            Local(5, p, rotation, new Vector3(0, 1.35f + i * .31f, -.52f), new Vector3(3.3f, .25f, .16f));
        foreach (float x in new[]
        {
            -1.4f,
            1.4f
        }

        )
            Local(6, p, rotation, new Vector3(x, 1.16f, 0), new Vector3(.14f, .12f, .9f));
        Solid("Bench", p + rotation * new Vector3(0, .9f, -.1f), new Vector3(3.4f, 1.8f, 1.15f), yaw);
    }

    private static void Planter(Vector3 p, int variant)
    {
        if (NearPortal(p, 5))
            return;
        Box(1, p + Vector3.up * .42f, new Vector3(2.25f, .84f, 2.25f));
        Box(3, p + Vector3.up * .82f, new Vector3(2.4f, .13f, 2.4f));
        Box(4, p + Vector3.up * .92f, new Vector3(2.08f, .13f, 2.08f));
        for (int i = 0; i < 3; i++)
        {
            Vector3 q = p + new Vector3((i - 1) * .57f, 1.25f, i % 2 == 0 ? -.26f : .33f);
            Box(4, q, new Vector3(.13f, .65f, .13f));
            Box(4, q + new Vector3(.14f, 0, 0), new Vector3(.42f, .13f, .3f), 20 + variant * 9);
            Box(variant % 2 == 0 ? 8 : 3, q + Vector3.up * .4f, new Vector3(.5f, .26f, .5f), variant * 17);
            Box(7, q + Vector3.up * .55f, new Vector3(.17f, .08f, .17f));
        }

        Solid("Planter", p + Vector3.up * .45f, new Vector3(2.4f, .9f, 2.4f), 0);
    }

    private static void Lamp(Vector3 p)
    {
        if (NearPortal(p, 5))
            return;
        Box(1, p + Vector3.up * .13f, new Vector3(.9f, .26f, .9f));
        Box(6, p + Vector3.up * 1.65f, new Vector3(.22f, 3.1f, .22f));
        Box(3, p + Vector3.up * 2.95f, new Vector3(.38f, .16f, .38f));
        Box(6, p + Vector3.up * 3.2f, new Vector3(.82f, .16f, .82f));
        Box(7, p + Vector3.up * 3.55f, new Vector3(.59f, .57f, .59f));
        Box(6, p + Vector3.up * 3.9f, new Vector3(.94f, .18f, .94f));
        Box(3, p + Vector3.up * 4.05f, new Vector3(.36f, .15f, .36f));
        Solid("LampPost", p + Vector3.up * 1.8f, new Vector3(.4f, 3.6f, .4f), 0);
    }

    private static void Local(int m, Vector3 origin, Quaternion rotation, Vector3 p, Vector3 scale)
    {
        Box(m, origin + rotation * p, scale, rotation.eulerAngles.y);
    }

    private static void Box(int m, Vector3 p, Vector3 scale, float yaw = 0)
    {
        parts[m].Add(new CombineInstance { mesh = cube, transform = Matrix4x4.TRS(p, Quaternion.Euler(0, yaw, 0), scale) });
    }

    private static void Solid(string name, Vector3 p, Vector3 scale, float yaw)
    {
        var go = new GameObject(name + "Collider");
        go.transform.SetParent(root, false);
        go.transform.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0));
        go.AddComponent<BoxCollider>().size = scale;
        go.isStatic = true;
    }
}
