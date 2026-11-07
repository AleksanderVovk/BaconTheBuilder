using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class GameSettingsSetup
{
    private const string UI = "Assets/BlockBuilding/UI/";
    private static TMP_FontAsset font;
    private static Material textMat;
    private static Sprite Sprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(UI + name);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var r = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f);
        r.sizeDelta = size;
        r.anchoredPosition = position;
        return r;
    }

    private static UnityEngine.UI.Image Image(RectTransform r, Sprite sprite, Color color, bool raycast = false)
    {
        var image = r.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = sprite;
        image.type = sprite != null ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    private static TMP_Text Text(string name, Transform parent, string caption, Vector2 size, Vector2 position, float fontSize)
    {
        var r = Rect(name, parent, size, position);
        var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = font;
        t.fontSharedMaterial = textMat;
        t.fontStyle = FontStyles.Bold;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.fontSize = fontSize;
        t.enableAutoSizing = true;
        t.fontSizeMin = 14;
        t.fontSizeMax = fontSize;
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        t.text = caption;
        return t;
    }

    private static UnityEngine.UI.Button Button(string name, Transform parent, string caption, Vector2 size, Vector2 position, string sprite)
    {
        var r = Rect(name, parent, size, position);
        var image = Image(r, Sprite(sprite), Color.white, true);
        var b = r.gameObject.AddComponent<UnityEngine.UI.Button>();
        b.targetGraphic = image;
        var colors = b.colors;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
        colors.pressedColor = new Color(.65f, .75f, .85f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = .06f;
        b.colors = colors;
        b.navigation = new UnityEngine.UI.Navigation
        {
            mode = UnityEngine.UI.Navigation.Mode.None
        };
        Text("Label", r, caption, size - new Vector2(24, 12), Vector2.zero, 28);
        return b;
    }

    private static UnityEngine.UI.Slider Row(Transform parent, int index, string caption, float min, float max, out TMP_Text value)
    {
        float y = 170 - index * 146;
        var label = Text("Label" + index, parent, caption, new Vector2(530, 46), new Vector2(-44, y), 28);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        value = Text("Value" + index, parent, "100%", new Vector2(105, 46), new Vector2(265, y), 28);
        value.color = new Color(.65f, 1, .48f);
        var r = Rect("Slider" + index, parent, new Vector2(620, 48), new Vector2(0, y - 55));
        var image = Image(r, Sprite("Track.asset"), Color.white, true);
        var slider = r.gameObject.AddComponent<UnityEngine.UI.Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = index == 0;
        var area = Rect("FillArea", r, Vector2.zero, Vector2.zero);
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(8, 7);
        area.offsetMax = new Vector2(-8, -7);
        var fill = Rect("Fill", area, Vector2.zero, Vector2.zero);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        Image(fill, Sprite("Roblox/Green.png"), Color.white);
        slider.fillRect = fill;
        var handleArea = Rect("HandleArea", r, Vector2.zero, Vector2.zero);
        handleArea.anchorMin = new Vector2(0, .5f);
        handleArea.anchorMax = new Vector2(1, .5f);
        handleArea.offsetMin = new Vector2(22, 0);
        handleArea.offsetMax = new Vector2(-22, 0);
        var handle = Rect("Handle", handleArea, new Vector2(54, 54), Vector2.zero);
        var rim = Image(handle, Sprite("InfiniteBlocks/Circle.png"), new Color(.025f, .045f, .065f), true);
        rim.type = UnityEngine.UI.Image.Type.Simple;
        var inside = Rect("Face", handle, new Vector2(43, 43), Vector2.zero);
        Image(inside, Sprite("InfiniteBlocks/Circle.png"), new Color(.95f, 1, .95f)).type = UnityEngine.UI.Image.Type.Simple;
        slider.handleRect = handle;
        slider.targetGraphic = inside.GetComponent<UnityEngine.UI.Image>();
        slider.navigation = new UnityEngine.UI.Navigation
        {
            mode = UnityEngine.UI.Navigation.Mode.None
        };
        return slider;
    }

    [MenuItem("Tools/Block Building/Install Game Settings")]
    public static string Apply()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Stop Play Mode first.");
        var ui = UnityEngine.Object.FindFirstObjectByType<BuildLevelMenu>();
        if (ui == null)
            throw new InvalidOperationException("Existing level UI required.");
        var existing = UnityEngine.Object.FindFirstObjectByType<GameSettingsPanel>();
        if (existing != null)
        {
            existing.settings.placement = UnityEngine.Object.FindFirstObjectByType<BlockPlacementAudio>();
            EditorUtility.SetDirty(existing.settings);
            foreach (var slider in new[]
            {
                existing.sensitivity,
                existing.sounds,
                existing.music
            }

            )
            {
                var area = (RectTransform)slider.handleRect.parent;
                area.anchorMin = new Vector2(0, .5f);
                area.anchorMax = new Vector2(1, .5f);
                area.offsetMin = new Vector2(22, 0);
                area.offsetMax = new Vector2(-22, 0);
            }

            EditorSceneManager.MarkSceneDirty(existing.gameObject.scene);
            EditorSceneManager.SaveScene(existing.gameObject.scene);
            return "Settings handle proportions corrected; scene saved.";
        }

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UI + "BlockUIFont.asset");
        textMat = AssetDatabase.LoadAssetAtPath<Material>(UI + "BlockUIText.mat");
        var go = new GameObject("GameSettingsSystem");
        var store = go.AddComponent<GameSettingsStore>();
        store.player = ui.session.runtime.carrier.player.GetComponent<PlayerCharacteristic>();
        store.music = UnityEngine.Object.FindFirstObjectByType<WorldMusicController>();
        store.placement = UnityEngine.Object.FindFirstObjectByType<BlockPlacementAudio>();
        store.soundSources = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(s => s.gameObject.scene == go.scene
            && s.gameObject != store.music.gameObject).ToArray();
        store.baseVolumes = store.soundSources.Select(s => s.volume).ToArray();
        var panel = go.AddComponent<GameSettingsPanel>();
        panel.settings = store;
        panel.session = ui.session;
        panel.openButton = Button(
            "OpenSettings",
            ui.transform,
            "НАСТРОЙКИ",
            new Vector2(270, 72),
            Vector2.zero,
            "ButtonBlue.asset"
        );
        var open = (RectTransform)panel.openButton.transform;
        open.anchorMin = open.anchorMax = open.pivot = Vector2.one;
        open.anchoredPosition = new Vector2(-28, -20);
        var levels = (RectTransform)ui.back.transform;
        levels.anchorMin = levels.anchorMax = levels.pivot = Vector2.one;
        levels.anchoredPosition = new Vector2(-28, -104);
        levels.sizeDelta = new Vector2(270, 72);
        var resume = (RectTransform)ui.resume.transform;
        resume.anchoredPosition = new Vector2(-28, -104);
        var overlay = Rect("SettingsOverlay", ui.transform, Vector2.zero, Vector2.zero);
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        Image(overlay, null, new Color(.015f, .025f, .045f, .82f), true);
        panel.overlay = overlay.gameObject;
        var box = Rect("SettingsBox", overlay, new Vector2(760, 680), Vector2.zero);
        Image(box, Sprite("Panel.asset"), Color.white, true);
        panel.box = box;
        var header = Rect("Header", box, new Vector2(716, 86), new Vector2(0, 275));
        Image(header, Sprite("ButtonBlue.asset"), Color.white);
        Text("Heading", header, "НАСТРОЙКИ", new Vector2(570, 65), new Vector2(-14, 0), 40);
        panel.closeButton = Button("Close", box, "X", new Vector2(66, 66), new Vector2(315, 275), "ButtonRed.asset");
        panel.values = new TMP_Text[3];
        panel.sensitivity = Row(box, 0, "Чувствительность камеры", 20, 600, out panel.values[0]);
        panel.sounds = Row(box, 1, "Громкость звуков", 0, 1, out panel.values[1]);
        panel.music = Row(box, 2, "Громкость музыки", 0, 1, out panel.values[2]);
        panel.saveButton = Button("Save", box, "СОХРАНИТЬ", new Vector2(410, 76), new Vector2(0, -276), "ButtonGreen.asset");
        var audio = UnityEngine.Object.FindFirstObjectByType<StockGameAudio>();
        if (audio != null)
        {
            panel.uiVoice = audio.uiVoice;
            if (audio.clips.Length > 5)
                panel.successClip = audio.clips[5];
            audio.buttons = audio.buttons.Concat(new[] { panel.openButton, panel.closeButton, panel.saveButton }).Distinct().ToArray();
            EditorUtility.SetDirty(audio);
        }

        overlay.gameObject.SetActive(false);
        EditorUtility.SetDirty(ui);
        EditorUtility.SetDirty(store);
        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(go.scene);
        EditorSceneManager.SaveScene(go.scene);
        AssetDatabase.SaveAssets();
        return "Settings installed; camera="
            + (store.player != null)
            + ", SFX sources="
            + store.soundSources.Length
            + ", music="
            + (store.music != null)
            + ". Scene saved.";
    }
}
