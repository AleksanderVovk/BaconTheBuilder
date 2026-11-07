using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class FirstBuildTutorialSetup
{
    public static void Install()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Exit Play mode first");
        var s = UnityEngine.Object.FindFirstObjectByType<BuildLevelSession>();
        var r = s.runtime;
        foreach (var e in s.catalog.levels)
        {
            e.data.level.pickupPoint = new Vector3(0, 0, 5);
            EditorUtility.SetDirty(e.data);
        }

        r.pickupTransform.position = new Vector3(0, 0, 5);
        r.pickupTransform.localScale = Vector3.one;
        foreach (var collider in r.pickupTransform.GetComponents<Collider>())
            collider.enabled = false;
        var pieces = new List<CombineInstance>();
        var meshes = new List<Mesh>();
        var random = new System.Random(824);
        BlockType[] types =
        {
            BlockType.Cobblestone,
            BlockType.OakPlanks,
            BlockType.RoofBricks,
            BlockType.BirchLog,
            BlockType.StoneBricks
        };
        for (int i = 0; i < 26; i++)
        {
            var mesh = new Mesh();
            var scratch = new BlockMeshUtility();
            scratch.Begin();
            scratch.Cell(Vector3Int.zero, 1, null, types[i % types.Length]);
            scratch.Apply(mesh);
            var vertices = mesh.vertices;
            for (int v = 0; v < vertices.Length; v++)
                vertices[v] -= Vector3.one * .5f;
            mesh.vertices = vertices;
            meshes.Add(mesh);
            float angle = (float)random.NextDouble() * Mathf.PI * 2;
            float radius = i < 18 ? (float)random.NextDouble() * 1.6f : (float)random.NextDouble() * .7f;
            float size = .65f + (float)random.NextDouble() * .3f;
            float y = .12f + size * .5f + (i >= 18 ? .65f : 0);
            var pos = new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
            var rot = Quaternion.Euler(
                (float)random.NextDouble() * 16 - 8,
                (float)random.NextDouble() * 150,
                (float)random.NextDouble() * 16 - 8
            );
            pieces.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(pos, rot, Vector3.one * size) });
        }

        var combined = new Mesh
        {
            name = "Scattered supply blocks"
        };
        combined.CombineMeshes(pieces.ToArray(), true, true);
        const string path = "Assets/BlockBuilding/Generated/ScatteredSupply.asset";
        var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (old != null)
        {
            EditorUtility.CopySerialized(combined, old);
            UnityEngine.Object.DestroyImmediate(combined);
            combined = old;
        }
        else
            AssetDatabase.CreateAsset(combined, path);
        foreach (var mesh in meshes)
            UnityEngine.Object.DestroyImmediate(mesh);
        r.pickupTransform.GetComponent<MeshFilter>().sharedMesh = combined;
        r.pickupTransform.GetComponent<MeshRenderer>().sharedMaterial = r.data.blockMaterial;
        r.path.line.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Arrow.mat");
        r.path.line.textureMode = LineTextureMode.Tile;
        r.path.line.widthMultiplier = .65f;
        r.path.line.startColor = r.path.line.endColor = Color.white;
        r.path.line.alignment = LineAlignment.TransformZ;
        r.path.transform.rotation = Quaternion.Euler(90, 0, 0);
        r.path.sourceOffset = new Vector3(0, .25f, 0);
        var go = new GameObject("FirstBuildTutorial");
        go.transform.SetParent(r.transform, false);
        var t = go.AddComponent<FirstBuildTutorial>();
        t.session = s;
        t.view = GameObject.Find("Main Camera").GetComponent<Camera>();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/BlockBuilding/UI/BlockUIFont.asset");
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/BlockBuilding/UI/BlockUIText.mat");
        var label = new GameObject("PickupTutorialLabel").AddComponent<TextMeshPro>();
        label.transform.SetParent(r.pickupTransform, false);
        label.transform.localPosition = new Vector3(0, 2.5f, 0);
        label.font = font;
        label.fontSharedMaterial = mat;
        label.fontSize = 4;
        label.fontStyle = FontStyles.Bold;
        label.text = "Подбери блоки";
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(5, 1);
        label.color = new Color(1, .9f, .2f);
        label.raycastTarget = false;
        t.pickupLabel = label;
        label.gameObject.SetActive(false);
        var canvas = s.gameplayHUD.GetComponentInParent<Canvas>();
        var panel = (RectTransform)new GameObject(
            "TutorialPanel",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(UnityEngine.UI.Image)
        ).transform;
        panel.SetParent(canvas.transform, false);
        panel.anchorMin = panel.anchorMax = new Vector2(.5f, 1);
        panel.pivot = new Vector2(.5f, 1);
        panel.anchoredPosition = new Vector2(0, -230);
        panel.sizeDelta = new Vector2(720, 150);
        var bg = panel.GetComponent<UnityEngine.UI.Image>();
        bg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/BlockBuilding/UI/Panel.asset");
        bg.type = UnityEngine.UI.Image.Type.Sliced;
        bg.raycastTarget = false;
        t.panel = panel.GetComponent<CanvasGroup>();
        t.heading = Text("Heading", panel, font, mat, new Vector2(0, -28), new Vector2(680, 40), 26);
        t.heading.color = new Color(1, .85f, .15f);
        t.instruction = Text("Instruction", panel, font, mat, new Vector2(0, -95), new Vector2(680, 85), 30);
        panel.gameObject.SetActive(false);
        EditorUtility.SetDirty(t);
        EditorUtility.SetDirty(r.path);
        EditorSceneManager.MarkSceneDirty(go.scene);
        EditorSceneManager.SaveScene(go.scene);
        AssetDatabase.SaveAssets();
    }

    private static TMP_Text Text(string n, Transform parent, TMP_FontAsset font, Material mat, Vector2 pos, Vector2 size, float fs)
    {
        var rect = (RectTransform)new GameObject(n, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSharedMaterial = mat;
        text.fontSize = fs;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18;
        text.fontSizeMax = fs;
        text.raycastTarget = false;
        return text;
    }
}
