using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AtlasAdminSetup
{
    [MenuItem("Tools/Block Building/Install Atlas and Admin Tools")]
    public static void Install()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play mode first.");
        var session = UnityEngine.Object.FindFirstObjectByType<BuildLevelSession>();
        var existingUI = UnityEngine.Object.FindFirstObjectByType<BuildLevelMenu>();
        if (session == null || existingUI == null)
            throw new InvalidOperationException("Open Lobby with the connected campaign.");
        var atlas = VoxelAtlasPacker.Rebuild();
        session.runtime.data.blockMaterial.SetTexture("_BaseMap", atlas);
        session.runtime.ghostMaterial.SetTexture("_BaseMap", atlas);
        EditorUtility.SetDirty(session.runtime.data.blockMaterial);
        EditorUtility.SetDirty(session.runtime.ghostMaterial);
        foreach (var entry in session.catalog.levels)
        {
            if (entry.id != "volcano")
                continue;
            foreach (var site in entry.data.level.sites)
                foreach (var cell in site.blocks)
                    if (cell.type == BlockType.Slate)
                        cell.type = BlockType.Basalt;
            entry.data.level.Validate();
            EditorUtility.SetDirty(entry.data);
            File.WriteAllText("Assets/BlockBuilding/Generated/Campaign/volcano.json", entry.data.ToJson());
        }

        var canvas = existingUI.GetComponentInParent<Canvas>();
        var existing = canvas.transform.Find("BuildAdminInterface");
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing.gameObject);
        var font = existingUI.cards[0].title.font;
        var root = UI("BuildAdminInterface", canvas.transform);
        Stretch(root);
        var admin = root.AddComponent<BuildAdminMenu>();
        admin.session = session;
        var open = Button("OpenAdmin", root.transform, "ADMIN [F8]", new Vector2(24, -950), new Vector2(220, 54), font);
        var openRect = (RectTransform)open.transform;
        openRect.anchorMin = openRect.anchorMax = openRect.pivot = new Vector2(0, 0);
        openRect.anchoredPosition = new Vector2(24, 85);
        var panel = UI("AdminPanel", root.transform);
        Stretch(panel);
        Background(panel, new Color(0.025f, 0.04f, 0.065f, 0.99f), true);
        admin.panel = panel;
        var box = UI("AdminBox", panel.transform);
        var br = (RectTransform)box.transform;
        br.anchorMin = br.anchorMax = br.pivot = new Vector2(0.5f, 0.5f);
        br.sizeDelta = new Vector2(940, 900);
        Text("Title", box.transform, "АДМИН-ПАНЕЛЬ", 38, new Vector2(20, -15), new Vector2(900, 60), font);
        admin.status = Text("Status", box.transform, "", 23, new Vector2(20, -90), new Vector2(900, 120), font);
        string[] labels =
        {
            "Выдать 999 блоков",
            "Очистить руки",
            "Сила +100 (временно)",
            "Вернуть обычную силу",
            "К куче блоков",
            "К целевому блоку",
            "Скорость стройки x1 / x10",
            "Завершить эту постройку",
            "Начать уровень заново",
            "В меню уровней"
        };
        admin.buttons = new UnityEngine.UI.Button[12];
        admin.buttons[0] = open;
        admin.buttons[1] = Button(
            "Close",
            box.transform,
            "ПРОДОЛЖИТЬ / F8",
            new Vector2(20, -800),
            new Vector2(900, 70),
            font
        );
        for (int i = 0; i < labels.Length; i++)
            admin.buttons[i + 2] = Button("Tool_"
                + i, box.transform, labels[i], new Vector2(20 + (i % 2) * 465, -240 - (i / 2) * 105), new Vector2(435, 80), font);
        panel.SetActive(false);
        var carrier = session.runtime.carrier;
        carrier.maximumVisible = 18;
        var counter = carrier.GetComponent<CarrierStackCounter>();
        if (counter == null)
            counter = carrier.gameObject.AddComponent<CarrierStackCounter>();
        counter.carrier = carrier;
        var camera = Camera.main;
        counter.viewCamera = camera != null ? camera.transform : null;
        var label = carrier.transform.Find("CarryCountLabel");
        if (label == null)
        {
            label = new GameObject("CarryCountLabel").transform;
            label.SetParent(carrier.transform, false);
        }

        var tmp = label.GetComponent<TextMeshPro>();
        if (tmp == null)
            tmp = label.gameObject.AddComponent<TextMeshPro>();
        tmp.font = font;
        tmp.fontSize = 5;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1, 0.88f, 0.35f);
        tmp.text = "";
        tmp.GetComponent<RectTransform>().sizeDelta = new Vector2(4, 0.8f);
        counter.label = tmp;
        tmp.gameObject.SetActive(false);
        EditorUtility.SetDirty(counter);
        EditorUtility.SetDirty(carrier);
        // UVs in cached editor preview meshes must be rebuilt for the expanded atlas.
        BlockVillageSetup.RebuildSceneVisuals(session.runtime);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(session.gameObject.scene);
        EditorSceneManager.SaveScene(session.gameObject.scene);
        Debug.Log("Extended painted atlas + admin UI installed. F8 toggles admin. Assisted runs skip record saves.");
    }

    private static GameObject UI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(GameObject go)
    {
        var r = (RectTransform)go.transform;
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    private static void Place(GameObject go, Vector2 at, Vector2 size)
    {
        var r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
        r.anchoredPosition = at;
        r.sizeDelta = size;
    }

    private static UnityEngine.UI.Image Background(GameObject go, Color color, bool hit)
    {
        var im = go.AddComponent<UnityEngine.UI.Image>();
        im.color = color;
        im.raycastTarget = hit;
        return im;
    }

    private static TMP_Text Text(string name, Transform parent, string value, int size, Vector2 at, Vector2 dimensions, TMP_FontAsset font)
    {
        var go = UI(name, parent);
        Place(go, at, dimensions);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.fontSizeMin = size * 0.7f;
        text.fontSizeMax = size;
        text.enableAutoSizing = true;
        text.color = Color.white;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Truncate;
        return text;
    }

    private static UnityEngine.UI.Button Button(string name, Transform parent, string label, Vector2 at, Vector2 size, TMP_FontAsset font)
    {
        var go = UI(name, parent);
        Place(go, at, size);
        var image = Background(go, new Color(0.16f, 0.25f, 0.34f), true);
        var button = go.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        Text("Label", go.transform, label, 25, Vector2.zero, size, font).alignment = TextAlignmentOptions.Center;
        return button;
    }
}
