using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RobloxBoostSetup
{
    private const string Folder = "Assets/BlockBuilding/UI/Roblox";
    [MenuItem("Tools/Block Building/Apply Roblox Boost UI")]
    public static string Install()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play mode first.");
        Directory.CreateDirectory(Folder);
        var ui = UnityEngine.Object.FindFirstObjectByType<BuildLevelMenu>();
        var hub = ui.session.training.GetComponent<GymEquipmentHub>();
        hub.freeAccess = true;
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/BlockBuilding/UI/BlockUIFont.asset");
        var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/BlockBuilding/UI/BlockUIText.mat");
        var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/BoldOutline.mat");
        if (material == null)
        {
            material = new Material(source);
            AssetDatabase.CreateAsset(material, Folder + "/BoldOutline.mat");
        }

        material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.3f);
        material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.3f);
        material.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
        material.EnableKeyword("OUTLINE_ON");
        EditorUtility.SetDirty(material);
        var camera = GameObject.Find("Main Camera").transform;
        Color[] tops =
        {
            new Color(1, 1, .45f),
            new Color(1, .8f, .2f),
            new Color(.6f, 1, .7f),
            new Color(.7f, 1, 1),
            new Color(1, 1, .4f),
            new Color(1, .55f, 1)
        };
        Color[] bottoms =
        {
            new Color(.2f, 1, .3f),
            new Color(1, .25f, .05f),
            new Color(0, .7f, .4f),
            new Color(0, .4f, 1),
            new Color(1, .45f, 0),
            new Color(.5f, .05f, 1)
        };
        for (int i = 0; i < hub.equipment.Length; i++)
        {
            var station = hub.equipment[i];
            var child = station.transform.Find("GymLabel");
            if (child == null)
            {
                child = new GameObject("GymLabel", typeof(TextMeshPro)).transform;
                child.SetParent(station.transform, false);
            }

            child.localPosition = new Vector3(0, 3.25f, 0);
            var label = child.GetComponent<TMP_Text>();
            label.font = font;
            label.fontSharedMaterial = material;
            label.text = station.title.ToUpperInvariant() + "\n<size=150%>x" + station.multiplier + " СИЛЫ</size>";
            label.fontStyle = FontStyles.Bold;
            label.fontSize = 3.4f;
            label.enableAutoSizing = false;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(5, 1.5f);
            label.raycastTarget = false;
            label.enableVertexGradient = true;
            label.color = Color.white;
            label.colorGradient = new VertexGradient(tops[i], tops[i], bottoms[i], bottoms[i]);
            label.UpdateMeshPadding();
            var floating = child.GetComponent<GymFloatingTitle>();
            if (floating == null)
                floating = child.gameObject.AddComponent<GymFloatingTitle>();
            floating.view = camera;
            floating.label = label;
        }

        var green = Sprite("Green", new Color(.65f, 1, .22f), new Color(.12f, .62f, .08f));
        var dark = Sprite("Track", new Color(.25f, .32f, .2f), new Color(.12f, .18f, .1f));
        var bar = (RectTransform)ui.constructionProgress.transform;
        bar.offsetMin = new Vector2(0, -82);
        bar.offsetMax = Vector2.zero;
        var bg = bar.GetComponent<UnityEngine.UI.Image>();
        bg.sprite = dark;
        bg.type = UnityEngine.UI.Image.Type.Sliced;
        bg.color = Color.white;
        var fill = ui.constructionProgress.fillRect.GetComponent<UnityEngine.UI.Image>();
        fill.sprite = green;
        fill.type = UnityEngine.UI.Image.Type.Sliced;
        fill.color = Color.white;
        ui.progress.fontSharedMaterial = material;
        ui.progress.fontSizeMax = 54;
        ui.progress.fontSize = 54;
        ui.progress.fontStyle = FontStyles.Bold;
        ui.progress.UpdateMeshPadding();
        var parent = (RectTransform)ui.timer.transform.parent;
        parent.sizeDelta = new Vector2(780, 182);
        var row = Rect("TestBoosts", parent);
        row.anchorMin = new Vector2(0, 1);
        row.anchorMax = Vector2.one;
        row.offsetMin = new Vector2(115, -151);
        row.offsetMax = new Vector2(-115, -105);
        var boost = ui.GetComponent<BuildTestBoosts>();
        if (boost == null)
            boost = ui.gameObject.AddComponent<BuildTestBoosts>();
        boost.session = ui.session;
        boost.buttons = new UnityEngine.UI.Button[4];
        int[] amounts =
        {
            100,
            500,
            2084,
            10418
        };
        boost.amounts = amounts;
        for (int i = 0; i < 4; i++)
        {
            var r = Rect("Boost" + i, row);
            r.anchorMin = new Vector2(i / 4f, 0);
            r.anchorMax = new Vector2((i + 1) / 4f, 1);
            r.offsetMin = new Vector2(5, 0);
            r.offsetMax = new Vector2(-5, 0);
            var image = r.GetComponent<UnityEngine.UI.Image>();
            if (image == null)
                image = r.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = i == 0
                ? green
                : i == 1
                ? Sprite("Orange", new Color(1, .73f, .16f), new Color(1, .27f, .01f))
                : i == 2
                ? Sprite("Red", new Color(1, .28f, .22f), new Color(.72f, .01f, .06f))
                : Sprite("Rainbow", Color.white, Color.white);
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = Color.white;
            var button = r.GetComponent<UnityEngine.UI.Button>();
            if (button == null)
                button = r.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            boost.buttons[i] = button;
            Text(Rect("Amount", r), "+"
                + amounts[i].ToString("N0", System.Globalization.CultureInfo.InvariantCulture), font, material, 29, false);
            var price = Rect("Price", r);
            price.anchorMin = new Vector2(0, 1);
            price.anchorMax = Vector2.one;
            price.offsetMin = new Vector2(-4, 1);
            price.offsetMax = new Vector2(4, 26);
            Text(price, "0.99$", font, material, 22, true);
        }

        ui.timer.fontSizeMax = 20;
        ui.timer.rectTransform.offsetMin = new Vector2(0, 0);
        ui.timer.rectTransform.offsetMax = new Vector2(0, 24);
        foreach (var c in ui.transform.root.GetComponentsInChildren<Component>(true))
            EditorUtility.SetDirty(c);
        EditorUtility.SetDirty(hub);
        EditorUtility.SetDirty(boost);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        EditorSceneManager.SaveScene(ui.gameObject.scene);
        return "Free gym access, camera-facing gradient titles and four free test construction boosts installed.";
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var t = parent.Find(name);
        if (t != null)
            return (RectTransform)t;
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Text(RectTransform r, string value, TMP_FontAsset font, Material material, float size, bool gradient)
    {
        if (r.name == "Amount")
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(3, 0);
            r.offsetMax = new Vector2(-3, 0);
        }

        var text = r.GetComponent<TextMeshProUGUI>();
        if (text == null)
            text = r.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSharedMaterial = material;
        text.text = value;
        text.fontSize = size;
        text.fontSizeMax = size;
        text.fontSizeMin = 16;
        text.enableAutoSizing = true;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.color = Color.white;
        text.enableVertexGradient = gradient;
        text.colorGradient = new VertexGradient(
            new Color(.75f, 1, .35f),
            new Color(.75f, 1, .35f),
            new Color(.05f, .8f, .12f),
            new Color(.05f, .8f, .12f)
        );
        text.UpdateMeshPadding();
    }

    private static Sprite Sprite(string name, Color top, Color bottom)
    {
        string path = Folder + "/" + name + ".png";
        var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float dx = Mathf.Max(12 - x, Mathf.Max(0, x - 51));
                float dy = Mathf.Max(12 - y, Mathf.Max(0, y - 51));
                bool inside = dx * dx + dy * dy <= 144;
                bool inner = x >= 4 && x < 60 && y >= 4 && y < 60 && (dx + 3) * (dx + 3) + (dy + 3) * (dy + 3) <= 144;
                texture.SetPixel(x, y, !inside
                    ? Color.clear
                    : !inner
                    ? Color.black
                    : name == "Rainbow"
                    ? Color.HSVToRGB((x / 63f + y / 126f) % 1, 0.8f, 1)
                    : Color.Lerp(bottom, top, y / 63f));
            }

        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = new Vector4(16, 16, 16, 16);
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
