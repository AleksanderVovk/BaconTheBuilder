using System.Linq;
using TMPro;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class BuildMilestoneSetup
{
    public static string ConfigureStack()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Stop Play Mode first.");
        var f = Object.FindFirstObjectByType<BuildMilestoneFeedback>(FindObjectsInactive.Include);
        var stack = f.area.Find("RightNotificationStack") as RectTransform;
        if (stack == null)
            stack = Rect("RightNotificationStack", f.area, new Vector2(390, 320), new Vector2(-24, -220));
        stack.anchorMin = stack.anchorMax = stack.pivot = Vector2.one;
        stack.sizeDelta = new Vector2(390, 320);
        var layout = stack.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        if (layout == null)
            layout = stack.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.spacing = 12;
        layout.childAlignment = TextAnchor.UpperRight;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        var fitter = stack.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (fitter == null)
            fitter = stack.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        foreach (var p in f.popups)
        {
            if (p.slot == null)
                p.slot = Rect("NotificationSlot", stack, new Vector2(390, 94), Vector2.zero);
            var element = p.slot.GetComponent<UnityEngine.UI.LayoutElement>();
            if (element == null)
                element = p.slot.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            element.minHeight = element.preferredHeight = 94;
            element.flexibleHeight = 0;
            p.root.SetParent(p.slot, false);
            p.root.anchorMin = p.root.anchorMax = p.root.pivot = new Vector2(.5f, .5f);
            p.root.anchoredPosition = Vector2.zero;
            p.root.sizeDelta = new Vector2(390, 94);
            p.root.localScale = Vector3.one;
            var badge = p.root.Find("Badge") as RectTransform;
            badge.sizeDelta = new Vector2(64, 64);
            badge.anchoredPosition = new Vector2(48, 0);
            p.text.rectTransform.offsetMin = new Vector2(96, 8);
            p.text.rectTransform.offsetMax = new Vector2(-12, -6);
            p.text.text = "<size=15><color=#ACFF64>ЭТАП ЗАВЕРШЁН</color></size>\n<size=30>100 БЛОКОВ</size>";
            p.root.gameObject.SetActive(false);
            p.slot.gameObject.SetActive(false);
        }

        f.stack = stack;
        f.maxVisible = 3;
        f.messageInterval = .75f;
        f.lifetime = 2.8f;
        EditorUtility.SetDirty(f);
        EditorSceneManager.MarkSceneDirty(f.gameObject.scene);
        EditorSceneManager.SaveScene(f.gameObject.scene);
        return "Right VerticalLayoutGroup, six pooled slots, three visible cards, animated entry/reflow/exit; scene saved.";
    }

    public static string Restyle()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Stop Play Mode first.");
        var f = Object.FindFirstObjectByType<BuildMilestoneFeedback>(FindObjectsInactive.Include);
        var rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        var circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        foreach (var p in f.popups)
        {
            p.root.sizeDelta = new Vector2(420, 100);
            var bg = p.root.GetComponent<UnityEngine.UI.Image>();
            bg.sprite = rounded;
            bg.type = UnityEngine.UI.Image.Type.Sliced;
            bg.color = new Color(.055f, .1f, .145f, .97f);
            var outline = p.root.GetComponent<UnityEngine.UI.Outline>();
            if (outline == null)
                outline = p.root.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0, 0, 0, .8f);
            outline.effectDistance = new Vector2(2, -3);
            foreach (Transform child in p.root)
                if (child.name.StartsWith("Spark"))
                    child.gameObject.SetActive(false);
            var t = p.text;
            t.enableVertexGradient = false;
            t.color = Color.white;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.lineSpacing = 2;
            t.rectTransform.anchorMin = Vector2.zero;
            t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = new Vector2(110, 9);
            t.rectTransform.offsetMax = new Vector2(-12, -7);
            t.text = "<size=17><color=#ACFF64>ЭТАП ЗАВЕРШЁН</color></size>\n<size=34>100 БЛОКОВ</size>";
            if (p.root.Find("Badge") == null)
            {
                var badge = Rect("Badge", p.root, new Vector2(72, 72), new Vector2(54, 0));
                badge.anchorMin = badge.anchorMax = new Vector2(0, .5f);
                var image = badge.gameObject.AddComponent<UnityEngine.UI.Image>();
                image.sprite = circle;
                image.color = new Color(.1f, .23f, .18f);
                image.raycastTarget = false;
                var icon = Rect("Cube", badge, new Vector2(54, 58), Vector2.zero);
                icon.gameObject.AddComponent<BuildMilestoneCubeIcon>().raycastTarget = false;
                var accent = Rect("Accent", p.root, new Vector2(4, 60), new Vector2(3, 0));
                accent.anchorMin = accent.anchorMax = new Vector2(0, .5f);
                var line = accent.gameObject.AddComponent<UnityEngine.UI.Image>();
                line.color = new Color(.55f, 1, .23f);
                line.raycastTarget = false;
            }

            var cube = p.root.GetComponentInChildren<BuildMilestoneCubeIcon>(true);
            if (cube != null && cube.GetComponent<CanvasRenderer>() == null)
                cube.gameObject.AddComponent<CanvasRenderer>();
            p.root.gameObject.SetActive(false);
        }

        f.lifetime = 1.65f;
        EditorUtility.SetDirty(f);
        EditorSceneManager.MarkSceneDirty(f.gameObject.scene);
        EditorSceneManager.SaveScene(f.gameObject.scene);
        return "Compact milestone toasts saved";
    }

    public static string PreviewToast(bool stackPreview = false)
    {
        var f = Object.FindFirstObjectByType<BuildMilestoneFeedback>(FindObjectsInactive.Include);
        var cameraObject = new GameObject("Toast preview camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.backgroundColor = new Color(.15f, .2f, .25f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.cullingMask = 1 << 31;
        camera.enabled = false;
        var holder = new GameObject("Toast preview canvas", typeof(RectTransform), typeof(Canvas));
        var canvas = holder.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1;
        int width = stackPreview ? 760 : 520, height = stackPreview ? 540 : 180;
        var rt = new RenderTexture(width, height, 24);
        camera.targetTexture = rt;
        var old = RenderTexture.active;
        try
        {
            for (int i = 0; i < (stackPreview ? 3 : 1); i++)
            {
                var clone = Object.Instantiate(f.popups[i].root, holder.transform);
                clone.gameObject.SetActive(true);
                clone.GetComponent<CanvasGroup>().alpha = 1;
                clone.anchoredPosition = stackPreview
                    ? new Vector2(width * .5f - 24 - clone.sizeDelta.x * .5f, 100 - i * 106)
                    : Vector2.zero;
                clone.localScale = Vector3.one;
                clone.GetComponentInChildren<TMP_Text>().text = "<size=15><color=#ACFF64>ЭТАП ЗАВЕРШЁН</color></size>\n<size=30>" + (i == 0
                    ? 50
                    : i == 1
                    ? 100
                    : 200) + " БЛОКОВ</size>";
            }

            foreach (var child in holder.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = 31;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = rt;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            System.IO.File.WriteAllBytes("F:/ClearVovkCore/BlockBuildingBackup/" + (stackPreview
                ? "MilestoneStack.png"
                : "MilestoneToast.png"), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }
        finally
        {
            RenderTexture.active = old;
            camera.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(holder);
            Object.DestroyImmediate(cameraObject);
        }

        return "Toast preview saved";
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f);
        r.sizeDelta = size;
        r.anchoredPosition = position;
        return r;
    }

    private static TMP_Text Text(string name, Transform parent, Vector2 size, Vector2 position, TMP_Text source, float fontSize)
    {
        var r = Rect(name, parent, size, position);
        var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = source.font;
        t.fontSharedMaterial = source.fontSharedMaterial;
        t.fontSize = fontSize;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    [MenuItem("Tools/Block Building/Install Construction Milestones")]
    public static string Apply()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Stop Play Mode first.");
        var menu = Object.FindFirstObjectByType<BuildLevelMenu>(FindObjectsInactive.Include);
        if (menu == null || menu.constructionProgress == null)
            throw new System.InvalidOperationException("Existing menu/bar missing.");
        var existing = Object.FindFirstObjectByType<BuildMilestoneFeedback>(FindObjectsInactive.Include);
        if (existing != null)
            return "Already installed";
        var canvas = menu.GetComponentInParent<Canvas>();
        var area = Rect("ConstructionMilestones", menu.runHUD.transform, Vector2.zero, Vector2.zero);
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = area.offsetMax = Vector2.zero;
        var feedback = area.gameObject.AddComponent<BuildMilestoneFeedback>();
        feedback.session = menu.session;
        feedback.canvas = canvas;
        feedback.area = area;
        feedback.bar = (RectTransform)menu.constructionProgress.transform;
        feedback.settings = Object.FindFirstObjectByType<GameSettingsStore>(FindObjectsInactive.Include);
        var ray = area.gameObject.AddComponent<CanvasGroup>();
        ray.interactable = false;
        ray.blocksRaycasts = false;
        var label = Text("MilestoneMarks", feedback.bar, new Vector2(-24, 20), new Vector2(0, 13), menu.progress, 17);
        label.rectTransform.anchorMin = new Vector2(0, 0);
        label.rectTransform.anchorMax = new Vector2(1, 0);
        label.text = "50    |    100    |    200";
        feedback.marks = label;
        var counter = menu.progress.rectTransform;
        counter.anchorMin = Vector2.zero;
        counter.anchorMax = Vector2.one;
        counter.offsetMin = new Vector2(12, 23);
        counter.offsetMax = new Vector2(-12, -3);
        menu.progress.enableAutoSizing = true;
        menu.progress.fontSizeMin = 26;
        menu.progress.fontSizeMax = 58;
        feedback.popups = new BuildMilestoneFeedback.Popup[6];
        var sprite = menu.constructionProgress.GetComponent<UnityEngine.UI.Image>().sprite;
        for (int i = 0; i < feedback.popups.Length; i++)
        {
            var root = Rect("MilestonePopup" + i, area, new Vector2(390, 120), Vector2.zero);
            var bg = root.gameObject.AddComponent<UnityEngine.UI.Image>();
            bg.sprite = sprite;
            bg.type = UnityEngine.UI.Image.Type.Sliced;
            bg.color = new Color(.06f, .17f, .09f, .87f);
            bg.raycastTarget = false;
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            var text = Text("Message", root, new Vector2(370, 112), Vector2.zero, menu.progress, 48);
            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(
                new Color(1, 1, .65f),
                new Color(1, 1, .65f),
                new Color(.5f, 1, .15f),
                new Color(.5f, 1, .15f)
            );
            for (int s = 0; s < 8; s++)
            {
                float a = s * Mathf.PI / 4;
                var spark = Rect("Spark" + s, root, new Vector2(s % 2 == 0
                    ? 12
                    : 7, s % 2 == 0
                    ? 12
                    : 7), new Vector2(Mathf.Cos(a) * 205, Mathf.Sin(a) * 69));
                spark.localRotation = Quaternion.Euler(0, 0, 45);
                var image = spark.gameObject.AddComponent<UnityEngine.UI.Image>();
                image.color = s % 2 == 0 ? new Color(1, .85f, .05f) : new Color(.3f, 1, .4f);
                image.raycastTarget = false;
            }

            feedback.popups[i] = new BuildMilestoneFeedback.Popup
            {
                root = root,
                group = group,
                text = text
            };
            root.gameObject.SetActive(false);
        }

        feedback.voice = area.gameObject.AddComponent<AudioSource>();
        feedback.voice.playOnAwake = false;
        feedback.voice.spatialBlend = 0;
        const string audio = "Assets/BlockBuilding/Audio/Milestones/";
        foreach (string file in new[]
        {
            "BuildImpact.ogg",
            "BuildSuccess.wav"
        }

        )
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(audio + file);
            importer.forceToMono = true;
            var sample = importer.defaultSampleSettings;
            sample.loadType = AudioClipLoadType.DecompressOnLoad;
            sample.compressionFormat = AudioCompressionFormat.Vorbis;
            sample.quality = .7f;
            importer.defaultSampleSettings = sample;
            importer.SaveAndReimport();
        }

        feedback.impact = AssetDatabase.LoadAssetAtPath<AudioClip>(audio + "BuildImpact.ogg");
        feedback.celebration = AssetDatabase.LoadAssetAtPath<AudioClip>(audio + "BuildSuccess.wav");
        var settings = feedback.settings;
        settings.soundSources = settings.soundSources.Concat(new[] { feedback.voice }).ToArray();
        settings.baseVolumes = settings.baseVolumes.Concat(new[] { feedback.volume }).ToArray();
        EditorUtility.SetDirty(settings);
        EditorUtility.SetDirty(feedback);
        EditorUtility.SetDirty(menu);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
        EditorSceneManager.SaveScene(menu.gameObject.scene);
        return string.Join("; ", menu.session.catalog.levels.Select(e => e.title
            + ": "
            + e.BlockCount
            + " to build, "
            + (BuildFoundation.RawCount(e.data) - e.BlockCount)
            + " foundation"));
    }
}
