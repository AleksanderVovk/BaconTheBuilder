using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LevelMenuPortalSetup
{
    private const string Folder = "Assets/BlockBuilding/Generated/Portal";
    private static Mesh cube;
    private static List<CombineInstance>[] parts;
    [MenuItem("Tools/Block Building/Setup Level Portal")]
    public static string Setup()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Stop Play Mode before setup.");
        var session = Object.FindFirstObjectByType<BuildLevelSession>();
        var tutorial = Object.FindFirstObjectByType<FirstBuildTutorial>();
        if (session == null || tutorial == null)
            throw new System.InvalidOperationException("Lobby systems missing.");
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/BlockBuilding/Generated", "Portal");
        var previous = session.runtime.transform.Find("AllBuildingsPortal");
        if (previous != null)
            Object.DestroyImmediate(previous.gameObject);
        var portal = new GameObject("AllBuildingsPortal");
        portal.transform.SetParent(session.runtime.transform, false);
        portal.transform.position = new Vector3(-11, .125f, 2);
        portal.transform.rotation = Quaternion.Euler(0, 75, 0);
        var component = portal.AddComponent<LevelMenuPortal>();
        component.session = session;
        component.player = session.runtime.carrier.player;
        var materials = new[]
        {
            Material("Obsidian", new Color(.105f, .115f, .17f)),
            Material("Stone", new Color(.24f, .26f, .34f)),
            Material("Crystal", new Color(.38f, .88f, 1), true),
            Material("Trim", new Color(.16f, .38f, .55f))
        };
        var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube = primitive.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(primitive);
        parts = new List<CombineInstance>[4];
        for (int i = 0; i < parts.Length; i++)
            parts[i] = new List<CombineInstance>();
        Box(0, new Vector3(0, .14f, 0), new Vector3(6, .28f, 3.2f));
        Box(1, new Vector3(0, .38f, 0), new Vector3(4.6f, .35f, 1.65f));
        Box(3, new Vector3(0, .57f, 0), new Vector3(3, .08f, 1.2f));
        for (int side = -1; side <= 1; side += 2)
        {
            Box(0, new Vector3(side * 2, .65f, 0), new Vector3(1.5f, .85f, 1.75f), new Vector3(0, side * 12, 0));
            for (int i = 0; i < 5; i++)
            {
                float x = side * (1.85f + (i % 2 == 0 ? .08f : -.02f));
                Box(i % 2, new Vector3(x, 1.25f + i * .79f, 0), new Vector3(.78f, .82f, 1.05f), new Vector3(0, side * (i % 2 == 0
                    ? 8
                    : -8), side * (i % 2 == 0
                    ? 5
                    : -4)));
                if (i == 1 || i == 4)
                    Box(2, new Vector3(x, 1.62f + i * .79f, 0), new Vector3(.89f, .12f, 1.15f));
            }

            Box(1, new Vector3(side * 1.85f, 5.3f, 0), new Vector3(1.15f, .65f, 1.25f), new Vector3(0, side * 12, side * 9));
            Box(0, new Vector3(side * 2.6f, .5f, .25f), new Vector3(1, .65f, 1.3f), new Vector3(0, side * 18, side * 10));
            Box(2, new Vector3(side * 2.5f, 1.07f, .25f), new Vector3(.36f, 1.15f, .42f), new Vector3(0, 0, -side * 24));
            Box(2, new Vector3(side * 2.85f, .91f, .35f), new Vector3(.3f, .72f, .35f), new Vector3(0, 0, -side * 42));
            Box(2, new Vector3(side * 2.25f, .94f, .48f), new Vector3(.28f, .8f, .28f), new Vector3(0, 0, side * 15));
            for (int i = 0; i < 4; i++)
                Box(2, new Vector3(side * 1.15f, .31f, .5f + i * .32f), new Vector3(.13f, .04f, .17f));
        }

        Box(0, new Vector3(0, 5.42f, 0), new Vector3(4.3f, .65f, 1.3f), new Vector3(0, 0, -3));
        Box(1, new Vector3(-.2f, 5.78f, -.07f), new Vector3(3.4f, .25f, 1.15f), new Vector3(0, 0, -3));
        Box(2, new Vector3(0, 5.03f, .05f), new Vector3(2.85f, .1f, .94f));
        for (int i = 0; i < 4; i++)
        {
            var mesh = new Mesh
            {
                name = "Portal_" + materials[i].name
            };
            mesh.CombineMeshes(parts[i].ToArray(), true, true);
            string path = Folder + "/Frame" + i + ".asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old == null)
                AssetDatabase.CreateAsset(mesh, path);
            else
            {
                EditorUtility.CopySerialized(mesh, old);
                Object.DestroyImmediate(mesh);
                mesh = old;
                EditorUtility.SetDirty(mesh);
            }

            var child = new GameObject(materials[i].name);
            child.transform.SetParent(portal.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            child.AddComponent<MeshRenderer>().sharedMaterial = materials[i];
        }

        Collider(portal.transform, new Vector3(-1.9f, 2.8f, 0), new Vector3(.9f, 4.8f, 1));
        Collider(portal.transform, new Vector3(1.9f, 2.8f, 0), new Vector3(.9f, 4.8f, 1));
        Collider(portal.transform, new Vector3(0, 5.4f, 0), new Vector3(4.7f, .75f, 1.2f));
        Collider(portal.transform, new Vector3(0, .15f, 0), new Vector3(6, .3f, 3.2f));
        var energy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        energy.name = "AnimatedEnergy";
        energy.transform.SetParent(portal.transform, false);
        energy.transform.localPosition = new Vector3(0, 2.85f, 0);
        energy.transform.localScale = new Vector3(2.95f, 4.35f, .08f);
        Object.DestroyImmediate(energy.GetComponent<UnityEngine.Collider>());
        string energyPath = Folder + "/Energy.mat";
        var energyMat = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(energyPath);
        if (energyMat == null)
        {
            energyMat = new UnityEngine.Material(Shader.Find("BlockBuilding/Portal Energy"));
            AssetDatabase.CreateAsset(energyMat, energyPath);
        }

        energy.GetComponent<Renderer>().sharedMaterial = energyMat;
        component.floatingCubes = new Transform[5];
        for (int i = 0; i < 5; i++)
        {
            var spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spark.name = "FloatingRune" + i;
            spark.transform.SetParent(portal.transform, false);
            spark.transform.localPosition = new Vector3((i % 2 == 0 ? -1 : 1) * (2.4f + (i % 3) * .16f), 2 + i * .66f, .7f);
            spark.transform.localScale = Vector3.one * (.12f + (i % 2) * .06f);
            spark.transform.localRotation = Quaternion.Euler(25, 45, 30);
            spark.GetComponent<Renderer>().sharedMaterial = materials[2];
            Object.DestroyImmediate(spark.GetComponent<UnityEngine.Collider>());
            component.floatingCubes[i] = spark.transform;
        }

        var label = Object.Instantiate(tutorial.pickupLabel, portal.transform);
        label.name = "AllBuildingsLabel";
        label.gameObject.SetActive(true);
        label.transform.localPosition = new Vector3(0, 6.65f, 0);
        label.transform.localRotation = Quaternion.identity;
        label.transform.localScale = Vector3.one;
        Style(label, "Все Постройки", new Color(.92f, .81f, 1), new Color(.36f, .68f, 1));
        label.fontSize = 5.5f;
        label.rectTransform.sizeDelta = new Vector2(9, 1.25f);
        var hint = label.GetComponent<WorldActivityLabel>();
        if (hint == null)
            hint = label.gameObject.AddComponent<WorldActivityLabel>();
        hint.session = session;
        hint.view = tutorial.view;
        hint.label = label;
        hint.pulseScale = 1.04f;
        hint.pulseSeconds = 2.5f;
        var pickup = tutorial.pickupLabel;
        pickup.gameObject.SetActive(true);
        pickup.transform.localScale = Vector3.one;
        Style(pickup, "Взять блоки", new Color(1, .98f, .58f), new Color(1, .62f, .12f));
        pickup.rectTransform.sizeDelta = new Vector2(7, 1.3f);
        pickup.fontSize = 5;
        hint = pickup.GetComponent<WorldActivityLabel>();
        if (hint == null)
            hint = pickup.gameObject.AddComponent<WorldActivityLabel>();
        hint.session = session;
        hint.view = tutorial.view;
        hint.label = pickup;
        hint.pulseScale = 1.15f;
        hint.pulseSeconds = 1.5f;
        EditorUtility.SetDirty(pickup);
        EditorUtility.SetDirty(hint);
        EditorUtility.SetDirty(component);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(portal.scene);
        EditorSceneManager.SaveScene(portal.scene);
        return "Portal at (-11, .125, 2); existing menu linked; permanent pickup label restored.";
    }

    private static void Style(TMP_Text text, string caption, Color top, Color bottom)
    {
        text.text = caption;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.fontStyle = FontStyles.Bold;
        text.enableVertexGradient = true;
        text.color = Color.white;
        text.colorGradient = new VertexGradient(top, top, bottom, bottom);
        string path = Folder + "/OutlinedWorldText.mat";
        var m = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
        if (m == null)
        {
            m = new UnityEngine.Material(text.fontSharedMaterial);
            m.SetColor("_OutlineColor", new Color(.035f, .025f, .08f));
            m.SetFloat("_OutlineWidth", .22f);
            AssetDatabase.CreateAsset(m, path);
        }

        text.fontSharedMaterial = m;
        text.raycastTarget = false;
    }

    private static UnityEngine.Material Material(string name, Color color, bool glow = false)
    {
        string path = Folder + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
        if (m == null)
        {
            m = new UnityEngine.Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }

        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", .23f);
        if (glow)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * .65f);
        }

        EditorUtility.SetDirty(m);
        return m;
    }

    private static void Box(int material, Vector3 position, Vector3 scale, Vector3 rotation = default(Vector3))
    {
        parts[material].Add(new CombineInstance { mesh = cube, transform = Matrix4x4.TRS(position, Quaternion.Euler(rotation), scale) });
    }

    private static void Collider(Transform parent, Vector3 center, Vector3 size)
    {
        var c = parent.gameObject.AddComponent<BoxCollider>();
        c.center = center;
        c.size = size;
    }
}
