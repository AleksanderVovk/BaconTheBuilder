using TMPro;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class VictoryEconomySetup
{
    public static void Install()
    {
        var hub = UnityEngine.Object.FindFirstObjectByType<GymEquipmentHub>();
        hub.freeAccess = false;
        hub.paymentsConfigured = false;
        var hud = UnityEngine.Object.FindFirstObjectByType<BlockBuildingHUD>();
        hud.statsText.transform.parent.gameObject.SetActive(false);
        var root = Rect(
            "VictoryStats",
            hub.session.gameplayHUD.transform,
            new Vector2(0, 1),
            new Vector2(16, -30),
            new Vector2(350, 210)
        );
        root.pivot = new Vector2(0, 1);
        var effect = root.gameObject.GetComponent<VictoryStatsHUD>();
        if (effect == null)
            effect = root.gameObject.AddComponent<VictoryStatsHUD>();
        effect.hud = hud;
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/BlockBuilding/UI/BlockUIFont.asset");
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/BlockBuilding/UI/BlockUIText.mat");
        var icon = Rect("StrengthIcon", root, new Vector2(0, 1), new Vector2(45, -40), new Vector2(90, 90));
        var image = icon.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Graphics/Sprites/strong.png");
        image.preserveAspect = true;
        image.raycastTarget = false;
        effect.strengthText = Text("Strength", root, font, mat, new Vector2(205, -32), new Vector2(240, 80), 64);
        effect.capacityText = Text("Capacity", root, font, mat, new Vector2(200, -78), new Vector2(270, 34), 24);
        var cup = Rect("VictoryIcon", root, new Vector2(0, 1), new Vector2(45, -145), new Vector2(85, 85));
        cup.gameObject.AddComponent<VictoryCupGraphic>().raycastTarget = false;
        effect.winsText = Text("Victories", root, font, mat, new Vector2(205, -145), new Vector2(240, 80), 64);
        int[] prices =
        {
            0,
            1,
            3,
            6,
            10,
            15
        };
        for (int i = 0; i < hub.equipment.Length; i++)
        {
            var s = hub.equipment[i];
            s.victoryPrice = prices[i];
            EditorUtility.SetDirty(s);
            if (i == 0)
                continue;
            var label = s.transform.Find("GymLabel");
            var price = new GameObject("VictoryPrice").AddComponent<TextMeshPro>();
            price.transform.SetParent(label, false);
            price.transform.localPosition = new Vector3(-.2f, -1.2f, 0);
            price.font = font;
            price.fontSharedMaterial = mat;
            price.fontSize = 2.6f;
            price.rectTransform.sizeDelta = new Vector2(6, .65f);
            price.alignment = TextAlignmentOptions.Center;
            price.raycastTarget = false;
            price.enableVertexGradient = true;
            price.colorGradient = new VertexGradient(
                new Color(1, 1, .6f),
                new Color(1, 1, .6f),
                new Color(1, .55f, .02f),
                new Color(1, .55f, .02f)
            );
            var world = new GameObject("VictoryCup", typeof(RectTransform), typeof(Canvas));
            world.transform.SetParent(label, false);
            world.transform.localPosition = new Vector3(1.95f, -1.2f, 0);
            var wr = (RectTransform)world.transform;
            wr.sizeDelta = new Vector2(.6f, .6f);
            world.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            world.AddComponent<VictoryCupGraphic>().raycastTarget = false;
            var binding = label.gameObject.AddComponent<GymVictoryPrice>();
            binding.hub = hub;
            binding.station = s;
            binding.caption = price;
            binding.cup = world;
        }

        EditorUtility.SetDirty(effect);
        EditorUtility.SetDirty(hub);
        EditorSceneManager.MarkSceneDirty(hub.gameObject.scene);
        EditorSceneManager.SaveScene(hub.gameObject.scene);
    }

    private static RectTransform Rect(string name, Transform p, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var r = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        r.SetParent(p, false);
        r.anchorMin = r.anchorMax = anchor;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        return r;
    }

    private static TMP_Text Text(string n, Transform p, TMP_FontAsset f, Material m, Vector2 pos, Vector2 size, float fs)
    {
        var r = Rect(n, p, new Vector2(0, 1), pos, size);
        var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = f;
        t.fontSharedMaterial = m;
        t.fontSize = fs;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.enableAutoSizing = true;
        t.fontSizeMin = 20;
        t.fontSizeMax = fs;
        t.raycastTarget = false;
        return t;
    }
}
