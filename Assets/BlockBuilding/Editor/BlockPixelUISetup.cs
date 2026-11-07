using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// All artwork is drawn here into one small point-filtered atlas. No packages or external artwork.
public static class BlockPixelUISetup
{
    private const string Folder = "Assets/BlockBuilding/UI";
    private static Sprite blue, red, green, panel, slot, gold, track, fill, cube, barbell, grid, arrow, tool;
    private static TMP_FontAsset font;
    private static Material textMaterial;
    private static readonly Color Ink = new Color32(12, 19, 27, 255);
    private static readonly Color Slate = new Color32(29, 43, 59, 255);
    [MenuItem("Tools/Block Building/Apply Pixel UI Style")]
    public static string Install()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play mode first.");
        var ui = UnityEngine.Object.FindFirstObjectByType<BuildLevelMenu>();
        if (ui == null)
            throw new InvalidOperationException("Existing level interface is required.");
        Directory.CreateDirectory(Folder);
        CreateArtwork();
        CreateFont();
        var canvas = ui.GetComponentInParent<Canvas>();
        foreach (var label in canvas.GetComponentsInChildren<TMP_Text>(true))
            TextStyle(label);
        foreach (var button in canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            ButtonStyle(button, blue);
        // Existing menu and six cards retain their references and click handlers.
        var menu = ui.menu.transform;
        Surface(menu, panel, true);
        var header = Image("PixelHeader", menu, panel);
        Stretch(header.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -112), Vector2.zero);
        header.transform.SetAsFirstSibling();
        var heading = menu.Find("Heading").GetComponent<TMP_Text>();
        heading.text = "ПОСТРОЙ СВОЙ МИР";
        heading.fontSize = 42;
        Place(heading.rectTransform, new Vector2(0, 1), new Vector2(106, -24), new Vector2(1050, 62));
        var emblem = Image("WorldIcon", menu, cube);
        Place(emblem.rectTransform, new Vector2(0, 1), new Vector2(28, -24), new Vector2(60, 60));
        var subtitle = menu.Find("Subtitle").GetComponent<TMP_Text>();
        subtitle.text = "ВЫБЕРИ ПОСТРОЙКУ";
        subtitle.fontSize = 22;
        Place(subtitle.rectTransform, new Vector2(0, 1), new Vector2(48, -126), new Vector2(1000, 36));
        var cards = (RectTransform)menu.Find("Cards");
        Stretch(cards, Vector2.zero, Vector2.one, new Vector2(48, 86), new Vector2(-48, -180));
        var layout = cards.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        layout.spacing = new Vector2(24, 24);
        layout.padding = new RectOffset();
        for (int i = 0; i < ui.cards.Length; i++)
        {
            var card = ui.cards[i];
            ButtonStyle(card.button, blue);
            var backdrop = (RectTransform)card.transform.Find("IconBackdrop");
            Surface(backdrop, slot, false);
            Stretch(backdrop, new Vector2(0, 0.38f), Vector2.one, new Vector2(12, 0), new Vector2(-12, -12));
            Stretch(card.icon.rectTransform, Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -8));
            Stretch(
                card.title.rectTransform,
                new Vector2(0, 0.27f),
                new Vector2(1, 0.37f),
                new Vector2(18, 0),
                new Vector2(-18, 0)
            );
            card.title.fontSize = 28;
            card.title.alignment = TextAlignmentOptions.MidlineLeft;
            Stretch(
                card.details.rectTransform,
                new Vector2(0, 0.105f),
                new Vector2(1, 0.27f),
                new Vector2(18, 0),
                new Vector2(-18, 0)
            );
            card.details.fontSize = 20;
            card.details.alignment = TextAlignmentOptions.MidlineLeft;
            var ribbon = Image("ActionRibbon", card.transform, slot);
            Stretch(ribbon.rectTransform, Vector2.zero, new Vector2(1, 0.105f), new Vector2(12, 10), new Vector2(-12, 2));
            ribbon.transform.SetAsFirstSibling();
            Stretch(
                card.status.rectTransform,
                Vector2.zero,
                new Vector2(1, 0.105f),
                new Vector2(18, 9),
                new Vector2(-18, 1)
            );
            card.status.fontSize = 18;
            card.status.alignment = TextAlignmentOptions.Center;
            var selected = Image("SelectedFrame", card.transform, gold);
            Stretch(selected.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            selected.enabled = ui.session.SelectedIndex == i;
            card.selectionFrame = selected;
            card.Refresh(ui.session.catalog.levels[i], i + 1);
            EditorUtility.SetDirty(card);
        }

        var footer = menu.Find("Footer").GetComponent<TMP_Text>();
        footer.text = "ЗАВЕРШИ ПОСТРОЙКУ, ЧТОБЫ ОТКРЫТЬ ЦВЕТНУЮ ИКОНКУ";
        footer.fontSize = 18;
        Stretch(footer.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(320, 20), new Vector2(-48, 64));
        Place((RectTransform)ui.resume.transform, Vector2.one, new Vector2(-28, -24), new Vector2(330, 64));
        ButtonStyle(ui.resume, green);
        // One read-only build slider. Text is drawn last, above the fill.
        var timerPanel = (RectTransform)ui.timer.transform.parent;
        var oldPanel = timerPanel.GetComponent<UnityEngine.UI.Image>();
        if (oldPanel != null)
        {
            oldPanel.color = Color.clear;
            oldPanel.raycastTarget = false;
        }

        timerPanel.sizeDelta = new Vector2(650, 100);
        var bar = Image("ConstructionBar", timerPanel, track);
        Place(bar.rectTransform, new Vector2(0.5f, 1), Vector2.zero, new Vector2(650, 62));
        bar.rectTransform.pivot = new Vector2(0.5f, 1);
        bar.rectTransform.anchorMin = new Vector2(0, 1);
        bar.rectTransform.anchorMax = Vector2.one;
        bar.rectTransform.offsetMin = new Vector2(0, -62);
        bar.rectTransform.offsetMax = Vector2.zero;
        var area = Rect("FillArea", bar.transform);
        Stretch(area, Vector2.zero, Vector2.one, new Vector2(7, 7), new Vector2(-7, -7));
        var filled = Image("Fill", area, fill);
        Stretch(filled.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var slider = bar.GetComponent<UnityEngine.UI.Slider>();
        if (slider == null)
            slider = bar.gameObject.AddComponent<UnityEngine.UI.Slider>();
        slider.fillRect = filled.rectTransform;
        slider.handleRect = null;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.wholeNumbers = false;
        slider.interactable = false;
        slider.transition = UnityEngine.UI.Selectable.Transition.None;
        var nav = slider.navigation;
        nav.mode = UnityEngine.UI.Navigation.Mode.None;
        slider.navigation = nav;
        slider.SetValueWithoutNotify(0);
        ui.constructionProgress = slider;
        ui.progress.transform.SetParent(bar.transform, false);
        Stretch(ui.progress.rectTransform, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0));
        ui.progress.fontSize = 32;
        ui.progress.fontSizeMax = 32;
        ui.progress.text = "0 / 784";
        ui.progress.alignment = TextAlignmentOptions.Center;
        Stretch(ui.timer.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 32));
        ui.timer.fontSize = 24;
        ui.timer.alignment = TextAlignmentOptions.Center;
        Place((RectTransform)ui.back.transform, Vector2.one, new Vector2(-28, -24), new Vector2(270, 76));
        Decorate(ui.back, grid, "УРОВНИ [Esc]");
        var hud = ui.session.gameplayHUD.GetComponent<BlockBuildingHUD>();
        var stats = (RectTransform)hud.statsText.transform.parent;
        Surface(stats, panel, false);
        Place(stats, new Vector2(0, 1), new Vector2(24, -24), new Vector2(370, 106));
        Stretch(hud.statsText.rectTransform, new Vector2(0, 0.42f), Vector2.one, new Vector2(16, 0), new Vector2(-16, -8));
        hud.statsText.fontSize = 24;
        hud.statsText.alignment = TextAlignmentOptions.Center;
        hud.hintText.gameObject.SetActive(false);
        var strengthRect = (RectTransform)hud.progress.transform;
        Stretch(strengthRect, Vector2.zero, new Vector2(1, 0), new Vector2(16, 18), new Vector2(-16, 40));
        Surface(strengthRect, track, false);
        var strengthFill = hud.progress.fillRect.GetComponent<UnityEngine.UI.Image>();
        strengthFill.sprite = fill;
        strengthFill.type = UnityEngine.UI.Image.Type.Sliced;
        strengthFill.color = Color.white;
        hud.progress.interactable = false;
        hud.progress.transition = UnityEngine.UI.Selectable.Transition.None;
        var controls = (RectTransform)hud.trainButton.transform.parent;
        Place(controls, new Vector2(1, 0), new Vector2(-28, 130), new Vector2(350, 80));
        Stretch((RectTransform)hud.trainButton.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Decorate(hud.trainButton, barbell, "ТРЕНИРОВКА [F]");
        var safety = ui.session.runtime.GetComponent<BuildPlayerSafety>();
        Place((RectTransform)safety.returnButton.transform, new Vector2(1, 0), new Vector2(-28, 228), new Vector2(350, 80));
        Decorate(safety.returnButton, arrow, "К ПОСТРОЙКЕ [T]");
        var jump = canvas.transform.Find("Controllers/Mobile/Jump");
        foreach (var b in canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            if (b.name == "Jump")
            {
                var icon = Image("PixelJump", b.transform, arrow);
                Stretch(icon.rectTransform, Vector2.zero, Vector2.one, new Vector2(18, 18), new Vector2(-18, -18));
            }

        // Same frame language in result and debug overlay.
        ui.result.GetComponent<UnityEngine.UI.Image>().color = new Color(0.055f, 0.085f, 0.13f, 0.96f);
        var box = ui.result.transform.Find("ResultBox");
        Surface(box, panel, false);
        ui.resultText.fontSize = 38;
        ui.resultText.margin = new Vector4(20, 12, 20, 12);
        ButtonStyle(ui.again, green);
        ButtonStyle(ui.levels, blue);
        ui.again.GetComponentInChildren<TMP_Text>(true).text = "ОСМОТРЕТЬ КАРТУ";
        var admin = canvas.GetComponentInChildren<BuildAdminMenu>(true);
        if (admin != null)
        {
            admin.panel.GetComponent<UnityEngine.UI.Image>().color = new Color(0.055f, 0.085f, 0.13f, 0.97f);
            Surface(admin.panel.transform.Find("AdminBox"), panel, false);
            Decorate(admin.buttons[0], tool, "ADMIN [F8]");
            Place((RectTransform)admin.buttons[0].transform, Vector2.zero, new Vector2(24, 16), new Vector2(235, 66));
            ButtonStyle(admin.buttons[1], green);
            ButtonStyle(admin.buttons[3], red);
            ButtonStyle(admin.buttons[10], red);
            foreach (var b in admin.buttons)
            {
                var label = b.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                    label.fontSize = 23;
            }
        }

        foreach (var obj in canvas.GetComponentsInChildren<Component>(true))
            if (obj != null)
                EditorUtility.SetDirty(obj);
        EditorUtility.SetDirty(ui);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);
        return "Pixel style applied to "
            + canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true).Length
            + " buttons; construction slider installed.";
    }

    private static void TextStyle(TMP_Text label)
    {
        label.font = font;
        label.fontSharedMaterial = textMaterial;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;
        label.raycastTarget = false;
        label.enableAutoSizing = true;
        label.fontSizeMin = 14;
        label.fontSizeMax = Mathf.Max(24, label.fontSize);
        label.overflowMode = TextOverflowModes.Truncate;
        label.UpdateMeshPadding();
    }

    private static void ButtonStyle(UnityEngine.UI.Button button, Sprite sprite)
    {
        var image = button.GetComponent<UnityEngine.UI.Image>();
        if (image == null)
            image = button.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = sprite;
        image.type = UnityEngine.UI.Image.Type.Sliced;
        image.color = Color.white;
        image.raycastTarget = true;
        button.targetGraphic = image;
        button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        colors.pressedColor = new Color(0.65f, 0.75f, 0.85f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.48f, 0.53f, 0.59f, 1);
        colors.fadeDuration = 0.06f;
        button.colors = colors;
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null && button.GetComponent<BuildLevelCard>() == null)
        {
            Stretch(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(12, 5), new Vector2(-12, -5));
            label.alignment = TextAlignmentOptions.Center;
            label.fontSizeMax = 28;
        }
    }

    private static void Decorate(UnityEngine.UI.Button button, Sprite icon, string text)
    {
        var image = Image("PixelIcon", button.transform, icon);
        Place(image.rectTransform, new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(44, 44));
        image.rectTransform.pivot = new Vector2(0, 0.5f);
        var label = button.GetComponentInChildren<TMP_Text>(true);
        label.text = text;
        label.fontSize = 23;
        label.fontSizeMax = 24;
        Stretch(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(66, 6), new Vector2(-12, -6));
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var found = parent.Find(name);
        if (found != null)
            return (RectTransform)found;
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static UnityEngine.UI.Image Image(string name, Transform parent, Sprite sprite)
    {
        var r = Rect(name, parent);
        return Surface(r, sprite, false);
    }

    private static UnityEngine.UI.Image Surface(Transform obj, Sprite sprite, bool hit)
    {
        var image = obj.GetComponent<UnityEngine.UI.Image>();
        if (image == null)
            image = obj.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.type = UnityEngine.UI.Image.Type.Sliced;
        image.raycastTarget = hit;
        return image;
    }

    private static void Place(RectTransform r, Vector2 anchor, Vector2 position, Vector2 size)
    {
        r.anchorMin = r.anchorMax = r.pivot = anchor;
        r.anchoredPosition = position;
        r.sizeDelta = size;
    }

    private static void Stretch(RectTransform r, Vector2 min, Vector2 max, Vector2 lo, Vector2 hi)
    {
        r.anchorMin = min;
        r.anchorMax = max;
        r.offsetMin = lo;
        r.offsetMax = hi;
    }

    private static void CreateFont()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder + "/BlockUIFont.asset");
        if (font == null)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            font = TMP_FontAsset.CreateFontAsset(
                source,
                64,
                9,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic
            );
            font.name = "BlockUIFont";
            string chars = "";
            for (int i = 32; i < 127; i++)
                chars += (char)i;
            for (int i = 0x410; i <= 0x44F; i++)
                chars += (char)i;
            chars += "Ёё";
            font.TryAddCharacters(chars);
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font, Folder + "/BlockUIFont.asset");
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var tex in font.atlasTextures)
                AssetDatabase.AddObjectToAsset(tex, font);
        }

        textMaterial = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/BlockUIText.mat");
        if (textMaterial == null)
        {
            textMaterial = new Material(font.material);
            AssetDatabase.CreateAsset(textMaterial, Folder + "/BlockUIText.mat");
        }

        textMaterial.SetColor(ShaderUtilities.ID_OutlineColor, Ink);
        textMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.15f);
        textMaterial.EnableKeyword("OUTLINE_ON");
        textMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0.18f);
        EditorUtility.SetDirty(textMaterial);
    }

    private static void CreateArtwork()
    {
        var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        var clear = new Color[256 * 256];
        tex.SetPixels(clear);
        Frame(tex, 0, new Color32(62, 118, 167, 255), new Color32(108, 162, 197, 255), new Color32(36, 76, 113, 255));
        Frame(tex, 1, new Color32(156, 65, 63, 255), new Color32(205, 110, 102, 255), new Color32(90, 39, 43, 255));
        Frame(tex, 2, new Color32(57, 133, 69, 255), new Color32(118, 189, 103, 255), new Color32(31, 79, 44, 255));
        Frame(tex, 3, Slate, new Color32(58, 81, 105, 255), new Color32(17, 28, 41, 255));
        Frame(tex, 4, new Color32(36, 64, 91, 255), new Color32(84, 139, 175, 255), new Color32(22, 44, 63, 255));
        Frame(tex, 5, Color.clear, new Color32(255, 218, 92, 255), new Color32(174, 122, 30, 255));
        Frame(tex, 6, new Color32(20, 35, 49, 255), new Color32(72, 100, 124, 255), new Color32(10, 20, 30, 255));
        Frame(tex, 7, new Color32(67, 142, 193, 255), new Color32(134, 198, 225, 255), new Color32(37, 87, 133, 255));
        // Pixel cube, an upward return arrow, barbell, level grid and a tool.
        TileRect(tex, 8, 10, 12, 44, 40, Ink);
        TileRect(tex, 8, 14, 16, 36, 32, new Color32(171, 114, 54, 255));
        TileRect(tex, 8, 14, 38, 36, 12, new Color32(241, 194, 99, 255));
        TileRect(tex, 8, 37, 16, 13, 22, new Color32(121, 77, 39, 255));
        TileRect(tex, 9, 25, 8, 14, 30, Ink);
        TileRect(tex, 9, 12, 31, 40, 9, Ink);
        TileRect(tex, 9, 18, 40, 28, 8, Ink);
        TileRect(tex, 9, 25, 48, 14, 7, Ink);
        TileRect(tex, 9, 29, 12, 6, 31, Color.white);
        TileRect(tex, 9, 18, 35, 28, 5, Color.white);
        TileRect(tex, 9, 24, 40, 16, 5, Color.white);
        TileRect(tex, 9, 29, 45, 6, 6, Color.white);
        TileRect(tex, 10, 5, 24, 54, 16, Ink);
        TileRect(tex, 10, 8, 29, 48, 6, new Color32(206, 221, 232, 255));
        for (int x = 10; x <= 42; x += 32)
        {
            TileRect(tex, 10, x, 12, 12, 40, Ink);
            TileRect(tex, 10, x + 3, 16, 6, 32, new Color32(179, 201, 216, 255));
        }

        for (int y = 12; y <= 36; y += 24)
            for (int x = 12; x <= 36; x += 24)
            {
                TileRect(tex, 11, x, y, 18, 18, Ink);
                TileRect(tex, 11, x + 3, y + 3, 12, 12, new Color32(232, 239, 245, 255));
            }

        TileRect(tex, 12, 16, 10, 10, 32, Ink);
        TileRect(tex, 12, 19, 13, 4, 25, new Color32(171, 113, 57, 255));
        TileRect(tex, 12, 12, 38, 38, 16, Ink);
        TileRect(tex, 12, 16, 42, 30, 8, new Color32(190, 210, 225, 255));
        tex.Apply();
        File.WriteAllBytes(Folder + "/BlockUIAtlas.png", tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(Folder + "/BlockUIAtlas.png", ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + "/BlockUIAtlas.png");
        importer.textureType = TextureImporterType.Default;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/BlockUIAtlas.png");
        blue = SpriteAsset(atlas, 0, "ButtonBlue", true);
        red = SpriteAsset(atlas, 1, "ButtonRed", true);
        green = SpriteAsset(atlas, 2, "ButtonGreen", true);
        panel = SpriteAsset(atlas, 3, "Panel", true);
        slot = SpriteAsset(atlas, 4, "Slot", true);
        gold = SpriteAsset(atlas, 5, "Selection", true);
        track = SpriteAsset(atlas, 6, "Track", true);
        fill = SpriteAsset(atlas, 7, "Fill", true);
        cube = SpriteAsset(atlas, 8, "Cube", false);
        arrow = SpriteAsset(atlas, 9, "Return", false);
        barbell = SpriteAsset(atlas, 10, "Barbell", false);
        grid = SpriteAsset(atlas, 11, "Levels", false);
        tool = SpriteAsset(atlas, 12, "Tool", false);
    }

    private static Sprite SpriteAsset(Texture2D atlas, int tile, string name, bool sliced)
    {
        string path = Folder + "/" + name + ".asset";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
            return sprite;
        sprite = Sprite.Create(atlas, new Rect((tile % 4) * 64, (tile / 4) * 64, 64, 64), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, sliced
            ? new Vector4(8, 8, 8, 8)
            : Vector4.zero);
        sprite.name = name;
        AssetDatabase.CreateAsset(sprite, path);
        return sprite;
    }

    private static void Frame(Texture2D tex, int tile, Color face, Color light, Color shade)
    {
        TileRect(tex, tile, 0, 0, 64, 64, Ink);
        TileRect(tex, tile, 3, 3, 58, 58, shade);
        TileRect(tex, tile, 3, 8, 58, 53, light);
        TileRect(tex, tile, 7, 8, 50, 49, face);
    }

    private static void TileRect(Texture2D tex, int tile, int x, int y, int w, int h, Color color)
    {
        for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
                tex.SetPixel((tile % 4) * 64 + xx, (tile / 4) * 64 + yy, color);
    }
}
