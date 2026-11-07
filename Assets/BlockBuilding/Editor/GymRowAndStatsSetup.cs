using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using TMPro;

public static class GymRowAndStatsSetup
{
    [MenuItem("Tools/Block Building/Align Gym Row And Stats")]
    public static string Setup()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Stop Play Mode first.");
        var hub = Object.FindFirstObjectByType<GymEquipmentHub>();
        var runtime = hub.session.runtime;
        var stats = Object.FindFirstObjectByType<VictoryStatsHUD>(FindObjectsInactive.Include);
        var root = (RectTransform)stats.transform;
        root.sizeDelta = new Vector2(380, 210);
        Text(stats.strengthText, new Vector2(112, -32), new Vector2(250, 80), 64);
        Text(stats.capacityText, new Vector2(112, -82), new Vector2(250, 34), 22);
        Text(stats.winsText, new Vector2(112, -145), new Vector2(250, 80), 64);
        var row = runtime.transform.Find("GymStations");
        if (row == null)
        {
            row = new GameObject("GymStations").transform;
            row.SetParent(runtime.transform, false);
        }

        // Premium stations used to be children of the standard one; detach before moving it.
        for (int i = 1; i < hub.equipment.Length; i++)
            hub.equipment[i].transform.SetParent(row, true);
        var portal = Object.FindFirstObjectByType<LevelMenuPortal>();
        for (int i = 0; i < hub.equipment.Length; i++)
        {
            var station = hub.equipment[i].transform;
            Vector3 position = new Vector3(25, .125f, 11.5f - i * 6.5f);
            Vector3 look = portal.transform.position - position;
            look.y = 0;
            station.SetPositionAndRotation(position, Quaternion.LookRotation(look));
            EditorUtility.SetDirty(station);
        }

        foreach (var entry in hub.session.catalog.levels)
        {
            entry.data.level.trainingPoint = hub.equipment[0].transform.position;
            EditorUtility.SetDirty(entry.data);
        }

        var podium = GameObject.Find("Podium").transform;
        podium.position = new Vector3(podium.position.x, podium.position.y, -5);
        podium.localScale = new Vector3(60, .25f, 40);
        var fence = runtime.transform.Find("GymFence");
        if (fence == null)
            throw new System.InvalidOperationException("Gym fence missing.");
        var prototype = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var cube = prototype.GetComponent<MeshFilter>().sharedMesh;
        var parts = new List<CombineInstance>();
        System.Action<Vector3, Vector3> add = (p, s) => parts.Add(new CombineInstance { mesh = cube, transform = Matrix4x4.TRS(p, Quaternion.identity, s) });
        for (float z = -24; z <= 14.01f; z += 3.8f)
            add(new Vector3(29.4f, .975f, z), new Vector3(.22f, 1.7f, .22f));
        foreach (float z in new[]
        {
            -24f,
            14f
        }

        )
            foreach (float x in new[]
            {
                23.4f,
                26.4f
            }

            )
                add(new Vector3(x, .975f, z), new Vector3(.22f, 1.7f, .22f));
        foreach (float y in new[]
        {
            .65f,
            1.35f
        }

        )
        {
            add(new Vector3(29.4f, y, -5), new Vector3(.16f, .14f, 38));
            foreach (float z in new[]
            {
                -24f,
                14f
            }

            )
                add(new Vector3(26.4f, y, z), new Vector3(6, .14f, .16f));
        }

        var mesh = new Mesh
        {
            name = "Gym fence behind station row"
        };
        mesh.CombineMeshes(parts.ToArray());
        Object.DestroyImmediate(prototype);
        const string fencePath = "Assets/BlockBuilding/Generated/Gym/SideRowFence.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(fencePath);
        if (existing == null)
        {
            existing = mesh;
            AssetDatabase.CreateAsset(existing, fencePath);
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

        fence.GetComponent<MeshFilter>().sharedMesh = existing;
        fence.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        fence.localScale = Vector3.one;
        var collider = fence.GetComponent<MeshCollider>();
        collider.sharedMesh = null;
        collider.sharedMesh = existing;
        EditorUtility.SetDirty(collider);
        EditorUtility.SetDirty(stats);
        Header(runtime.transform, hub);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(runtime.gameObject.scene);
        EditorSceneManager.SaveScene(runtime.gameObject.scene);
        return "Stats have 22px icon clearance. Six benches spaced 6.5m, face portal; side fence moved behind them; podium extended away from construction.";
    }

    private static void Header(Transform parent, GymEquipmentHub hub)
    {
        var root = parent.Find("GymAreaHeading");
        if (root == null)
            root = new GameObject("GymAreaHeading", typeof(RectTransform), typeof(TextMeshPro)).transform;
        root.SetParent(parent, false);
        Vector3 center = (hub.equipment[0].transform.position + hub.equipment[hub.equipment.Length - 1].transform.position) * .5f;
        root.position = center + Vector3.up * 7;
        root.localScale = Vector3.one;
        var text = root.GetComponent<TextMeshPro>();
        text.text = "ПРОКАЧАЙ СИЛУ";
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/BlockBuilding/UI/BlockUIFont.asset");
        text.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/BlockBuilding/Generated/Portal/OutlinedWorldText.mat");
        text.fontSize = 10;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.enableAutoSizing = false;
        text.raycastTarget = false;
        text.rectTransform.sizeDelta = new Vector2(20, 2.4f);
        text.color = Color.white;
        text.enableVertexGradient = true;
        text.colorGradient = new VertexGradient(
            new Color(1, 1, .68f),
            new Color(1, 1, .68f),
            new Color(1, .58f, .06f),
            new Color(1, .58f, .06f)
        );
        text.ForceMeshUpdate(true);
        float edge = Mathf.Max(Mathf.Abs(text.textBounds.min.x), Mathf.Abs(text.textBounds.max.x));
        var canvasTransform = root.Find("StrengthIcons");
        if (canvasTransform == null)
            canvasTransform = new GameObject("StrengthIcons", typeof(RectTransform), typeof(Canvas)).transform;
        canvasTransform.SetParent(root, false);
        canvasTransform.localPosition = Vector3.zero;
        canvasTransform.localRotation = Quaternion.identity;
        canvasTransform.localScale = Vector3.one;
        var canvas = canvasTransform.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        ((RectTransform)canvasTransform).sizeDelta = new Vector2(24, 3);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Graphics/Sprites/strong.png");
        for (int i = 0; i < 2; i++)
        {
            string name = i == 0 ? "LeftStrengthIcon" : "RightStrengthIcon";
            var icon = canvasTransform.Find(name) as RectTransform;
            if (icon == null)
                icon = (RectTransform)new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)).transform;
            icon.SetParent(canvasTransform, false);
            icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(.5f, .5f);
            icon.anchoredPosition = new Vector2((i == 0 ? -1 : 1) * (edge + 1.25f), text.textBounds.center.y);
            icon.sizeDelta = new Vector2(1.9f, 1.9f);
            icon.localScale = Vector3.one;
            var image = icon.GetComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        var hint = root.GetComponent<WorldActivityLabel>();
        if (hint == null)
            hint = root.gameObject.AddComponent<WorldActivityLabel>();
        hint.session = hub.session;
        hint.view = Object.FindFirstObjectByType<FirstBuildTutorial>().view;
        hint.label = text;
        hint.iconCanvas = canvas;
        hint.pulseScale = 1.03f;
        hint.pulseSeconds = 2.5f;
        hint.nearDistance = 9;
        EditorUtility.SetDirty(text);
        EditorUtility.SetDirty(hint);
    }

    private static void Text(TMP_Text text, Vector2 position, Vector2 size, float fontSize)
    {
        var rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        text.margin = Vector4.zero;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.fontSize = fontSize;
        text.fontSizeMax = fontSize;
        text.enableAutoSizing = true;
        text.enableWordWrapping = false;
        EditorUtility.SetDirty(text);
    }
}
