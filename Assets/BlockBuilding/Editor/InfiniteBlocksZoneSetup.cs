using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class InfiniteBlocksZoneSetup
{
    private const string Folder = "Assets/BlockBuilding/UI/InfiniteBlocks";
    [MenuItem("Tools/Block Building/Setup Infinite Blocks Zone")]
    public static string Setup()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Stop Play Mode first.");
        var session = Object.FindFirstObjectByType<BuildLevelSession>();
        if (Object.FindFirstObjectByType<InfiniteBlocksZone>() != null)
            throw new System.InvalidOperationException("Zone already installed; keep its settings.");
        ImportSprite(Folder + "/InfiniteBlocksIcon.png", 512);
        var circle = CreateCircle();
        var root = new GameObject("InfiniteBlocksZone");
        root.transform.SetParent(session.runtime.transform, false);
        root.transform.position = new Vector3(7, .17f, -9);
        var zone = root.AddComponent<InfiniteBlocksZone>();
        zone.session = session;
        zone.carrier = session.runtime.carrier;
        zone.feedback = Object.FindFirstObjectByType<StockGameAudio>();
        const string effectPath = "Assets/ExtraAssets/Particles/Epic Toon FX/Prefabs/Interactive/Zone/BasicZone/BasicZoneBlue.prefab";
        var effect = (GameObject)PrefabUtility.InstantiatePrefab(
            AssetDatabase.LoadAssetAtPath<GameObject>(effectPath),
            root.transform
        );
        effect.name = "BasicZoneBlue";
        effect.transform.localPosition = Vector3.zero;
        effect.transform.localScale = Vector3.one * 1.15f;
        foreach (var renderer in effect.GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            var source = renderer.sharedMaterial;
            string path = Folder + "/" + source.name + "_URP.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture("_BaseMap", source.mainTexture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", 2);
            material.SetFloat("_SrcBlend", 5);
            material.SetFloat("_DstBlend", 1);
            material.SetFloat("_SrcBlendAlpha", 1);
            material.SetFloat("_DstBlendAlpha", 1);
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_Cull", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = 3000;
            renderer.sharedMaterial = material;
            EditorUtility.SetDirty(material);
        }

        foreach (var ps in effect.GetComponentsInChildren<ParticleSystem>())
        {
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        }

        var tutorial = Object.FindFirstObjectByType<FirstBuildTutorial>();
        var title = Object.Instantiate(tutorial.pickupLabel, root.transform);
        title.name = "InfiniteBlocksTitle";
        title.gameObject.SetActive(true);
        title.transform.localPosition = new Vector3(0, 3.25f, 0);
        title.transform.localScale = Vector3.one;
        title.text = "Бесконечные блоки";
        title.rectTransform.sizeDelta = new Vector2(14, 1.2f);
        title.fontSize = 5;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.enableAutoSizing = false;
        title.enableWordWrapping = false;
        title.enableVertexGradient = false;
        title.color = Color.white;
        string textPath = Folder + "/RainbowTitle.mat";
        var textMaterial = new Material(title.fontSharedMaterial);
        textMaterial.SetColor("_FaceColor", Color.white);
        textMaterial.SetColor("_OutlineColor", Color.black);
        textMaterial.SetFloat("_OutlineWidth", .22f);
        AssetDatabase.CreateAsset(textMaterial, textPath);
        title.fontSharedMaterial = textMaterial;
        var world = title.GetComponent<WorldActivityLabel>();
        world.session = session;
        world.view = tutorial.view;
        world.label = title;
        world.iconCanvas = null;
        world.pulseScale = 1.04f;
        var subtitle = new GameObject("AdvertisementCaption").AddComponent<TextMeshPro>();
        subtitle.transform.SetParent(title.transform, false);
        subtitle.transform.localPosition = new Vector3(0, -.67f, 0);
        subtitle.font = title.font;
        subtitle.fontSharedMaterial = textMaterial;
        subtitle.fontSize = 3;
        subtitle.fontStyle = FontStyles.Bold;
        subtitle.color = Color.white;
        subtitle.enableVertexGradient = false;
        subtitle.rectTransform.sizeDelta = new Vector2(8, .7f);
        subtitle.alignment = TextAlignmentOptions.Center;
        subtitle.enableWordWrapping = false;
        subtitle.text = "за рекламу";
        var rainbow = title.gameObject.AddComponent<RainbowWorldText>();
        rainbow.label = title;
        rainbow.subtitle = subtitle;
        rainbow.session = session;
        var badge = Rect(
            "InfiniteBlocksBadge",
            session.gameplayHUD.transform,
            new Vector2(0, 1),
            new Vector2(78, -300),
            new Vector2(136, 160)
        );
        zone.badge = badge.gameObject;
        Image("BlackBorder", badge, circle, new Vector2(128, 128), new Vector2(0, 12), Color.black);
        Image("WhiteCircle", badge, circle, new Vector2(116, 116), new Vector2(0, 12), Color.white);
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/InfiniteBlocksIcon.png");
        Image("IconTrack", badge, icon, new Vector2(98, 98), new Vector2(0, 12), new Color(1, 1, 1, .15f));
        var fill = Image("IconCountdown", badge, icon, new Vector2(98, 98), new Vector2(0, 12), Color.white);
        fill.type = UnityEngine.UI.Image.Type.Filled;
        fill.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
        fill.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
        fill.fillClockwise = false;
        fill.fillAmount = 1;
        zone.countdownFill = fill;
        var time = Rect(
            "TimeRemaining",
            badge,
            new Vector2(.5f, .5f),
            new Vector2(0, -68),
            new Vector2(125, 38)
        ).gameObject.AddComponent<TextMeshProUGUI>();
        time.font = title.font;
        time.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/BlockBuilding/UI/BlockUIText.mat");
        time.fontStyle = FontStyles.Bold;
        time.fontSize = 28;
        time.alignment = TextAlignmentOptions.Center;
        time.color = Color.white;
        time.enableWordWrapping = false;
        time.raycastTarget = false;
        time.text = "30с";
        zone.timeText = time;
        badge.gameObject.SetActive(false);
        EditorUtility.SetDirty(zone);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
        return "BasicZoneBlue at (7, .17, -9); 30-second direct entry bonus; rainbow label and generated icon with radial Image fill.";
    }

    private static UnityEngine.UI.Image Image(string name, RectTransform parent, Sprite sprite, Vector2 size, Vector2 pos, Color color)
    {
        var image = Rect(name, parent, new Vector2(.5f, .5f), pos, size).gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var r = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = anchor;
        r.pivot = new Vector2(.5f, .5f);
        r.anchoredPosition = position;
        r.sizeDelta = size;
        return r;
    }

    private static void ImportSprite(string path, int max)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = max;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static Sprite CreateCircle()
    {
        string path = Folder + "/Circle.png";
        if (!System.IO.File.Exists(path))
        {
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            var pixels = new Color32[128 * 128];
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(64, 64));
                    pixels[y * 128 + x] = new Color(1, 1, 1, Mathf.Clamp01(63 - distance));
                }

            texture.SetPixels32(pixels);
            texture.Apply();
            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        ImportSprite(path, 128);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
