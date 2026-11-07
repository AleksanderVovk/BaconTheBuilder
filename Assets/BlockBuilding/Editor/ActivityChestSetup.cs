using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class ActivityChestSetup
{
    private const string Folder = "Assets/BlockBuilding/Generated/ActivityChest";
    private static Mesh cube;
    private static Material[] materials;
    private static List<CombineInstance>[] parts;
    [MenuItem("Tools/Block Building/Setup Activity Chest")]
    public static string Setup()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Stop Play Mode first.");
        var session = Object.FindFirstObjectByType<BuildLevelSession>();
        var mobile = Object.FindFirstObjectByType<MobileBuildInteraction>();
        var existing = session.runtime.transform.Find("ActivityChest");
        if (existing != null)
            throw new System.InvalidOperationException("Chest already exists; preserve its scene settings.");
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/BlockBuilding/Generated", "ActivityChest");
        var root = new GameObject("ActivityChest");
        root.transform.SetParent(session.runtime.transform, false);
        root.transform.position = new Vector3(-7, .125f, -9);
        root.transform.rotation = Quaternion.Euler(0, 38, 0);
        var reward = root.AddComponent<ActivityChestReward>();
        reward.session = session;
        reward.strength = session.runtime.carrier.strength;
        var animation = root.AddComponent<ActivityChestAnimation>();
        animation.reward = reward;
        var ui = root.AddComponent<ActivityChestInteraction>();
        ui.reward = reward;
        ui.view = mobile.viewCamera;
        ui.audioFeedback = Object.FindFirstObjectByType<StockGameAudio>();
        materials = new[]
        {
            Mat("Teal", new Color(.06f, .28f, .32f)),
            Mat("Gold", new Color(1, .67f, .10f)),
            Mat("Dark", new Color(.06f, .09f, .15f)),
            Mat("Wood", new Color(.40f, .20f, .12f)),
            Mat("Gem", new Color(.40f, 1, .64f), true)
        };
        var prototype = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube = prototype.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(prototype);
        Begin();
        Box(2, new Vector3(0, .1f, 0), new Vector3(4.7f, .2f, 4.7f));
        Box(1, new Vector3(0, .22f, 0), new Vector3(4.45f, .04f, 4.45f));
        Box(0, new Vector3(0, .27f, 0), new Vector3(4.15f, .06f, 4.15f));
        Box(3, new Vector3(0, .9f, 0), new Vector3(3.15f, 1.2f, 2.15f));
        Box(0, new Vector3(0, .94f, 1.10f), new Vector3(2.95f, .85f, .15f));
        Box(0, new Vector3(0, .94f, -1.10f), new Vector3(2.95f, .85f, .15f));
        foreach (float x in new[]
        {
            -1.6f,
            1.6f
        }

        )
            Box(0, new Vector3(x, .94f, 0), new Vector3(.16f, .85f, 2.1f));
        foreach (float y in new[]
        {
            .41f,
            1.43f
        }

        )
            Box(1, new Vector3(0, y, 0), new Vector3(3.4f, .19f, 2.4f));
        foreach (float x in new[]
        {
            -1.25f,
            1.25f
        }

        )
            foreach (float z in new[]
            {
                -1.16f,
                1.16f
            }

            )
                Box(1, new Vector3(x, .94f, z), new Vector3(.23f, 1.07f, .18f));
        Box(1, new Vector3(0, 1.13f, 1.25f), new Vector3(.62f, .62f, .2f));
        Box(2, new Vector3(0, 1.12f, 1.37f), new Vector3(.21f, .31f, .08f));
        Box(4, new Vector3(0, 1.20f, 1.43f), new Vector3(.19f, .19f, .05f), 45);
        Meshes(root.transform, "Body");
        var hinge = new GameObject("LidHinge").transform;
        hinge.SetParent(root.transform, false);
        hinge.localPosition = new Vector3(0, 1.49f, -1.15f);
        animation.lid = hinge;
        Begin();
        Box(0, new Vector3(0, .13f, 1.15f), new Vector3(3.4f, .28f, 2.4f));
        Box(0, new Vector3(0, .4f, 1.15f), new Vector3(3.1f, .28f, 2.05f));
        Box(0, new Vector3(0, .61f, 1.15f), new Vector3(2.8f, .18f, 1.65f));
        foreach (float x in new[]
        {
            -1.25f,
            1.25f
        }

        )
        {
            Box(1, new Vector3(x, .36f, 1.15f), new Vector3(.23f, .69f, 2.1f));
            Box(1, new Vector3(x, .08f, 1.15f), new Vector3(.28f, .15f, 2.5f));
        }

        Box(1, new Vector3(0, .78f, 1.15f), new Vector3(.75f, .18f, .75f), 45);
        Box(4, new Vector3(0, 1.01f, 1.15f), new Vector3(.47f, .47f, .47f), 45);
        Meshes(hinge, "Lid");
        var collider = root.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, .9f, 0);
        collider.size = new Vector3(3.5f, 1.8f, 2.5f);
        var baseCollider = root.AddComponent<BoxCollider>();
        baseCollider.center = new Vector3(0, .1f, 0);
        baseCollider.size = new Vector3(4.7f, .2f, 4.7f);
        animation.sparks = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            var spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spark.name = "GoldenSpark" + i;
            spark.transform.SetParent(root.transform, false);
            spark.transform.localPosition = new Vector3((i % 2 == 0 ? -1 : 1) * 1.85f, 1.5f + (i / 2) * .7f, 0);
            spark.transform.localScale = Vector3.one * .17f;
            spark.transform.localRotation = Quaternion.Euler(20, 45, 35);
            spark.GetComponent<Renderer>().sharedMaterial = materials[1];
            Object.DestroyImmediate(spark.GetComponent<Collider>());
            animation.sparks[i] = spark.transform;
        }

        var fx = new GameObject("OpeningSparkles");
        fx.transform.SetParent(root.transform, false);
        fx.transform.localPosition = new Vector3(0, 1.3f, 0);
        var particles = fx.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = .8f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.7f, 1.25f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2, 4);
        main.startSize = new ParticleSystem.MinMaxCurve(.07f, .19f);
        main.startColor = new Color(1, .78f, .2f);
        main.gravityModifier = .2f;
        main.maxParticles = 64;
        var emission = particles.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0, 32) });
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(2, .2f, 1.4f);
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        string particlePath = Folder + "/Sparkles.mat";
        var particleMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        particleMat.SetColor("_BaseColor", new Color(1, .8f, .15f));
        AssetDatabase.CreateAsset(particleMat, particlePath);
        renderer.sharedMaterial = particleMat;
        animation.celebration = particles;
        var tutorial = Object.FindFirstObjectByType<FirstBuildTutorial>();
        var label = Object.Instantiate(tutorial.pickupLabel, root.transform);
        label.name = "ChestProgressLabel";
        label.gameObject.SetActive(true);
        label.transform.localPosition = new Vector3(0, 4.6f, 0);
        label.transform.localScale = Vector3.one;
        label.rectTransform.sizeDelta = new Vector2(14, 3);
        label.fontSize = 5;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.text = "ПОДАРОК ЗА СТРОЙКУ\n<size=65%>0 / 100 блоков</size>";
        var world = label.GetComponent<WorldActivityLabel>();
        world.session = session;
        world.view = mobile.viewCamera;
        world.label = label;
        world.pulseScale = 1.04f;
        ui.worldLabel = label;
        var button = Object.Instantiate(mobile.interaction, mobile.interaction.transform.parent);
        button.name = "ActivityChestInteract";
        button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        button.gameObject.SetActive(false);
        ui.button = button;
        ui.caption = button.GetComponentInChildren<TMP_Text>(true);
        ui.caption.text = "Открыть сундук";
        ui.caption.enableWordWrapping = false;
        ui.caption.enableAutoSizing = true;
        ui.caption.fontSizeMin = 14;
        ui.caption.fontSizeMax = 22;
        var audios = new List<UnityEngine.UI.Button>(ui.audioFeedback.buttons);
        audios.Add(button);
        ui.audioFeedback.buttons = audios.ToArray();
        EditorUtility.SetDirty(ui.audioFeedback);
        var notice = (RectTransform)new GameObject(
            "ChestRewardNotice",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(UnityEngine.UI.Image)
        ).transform;
        notice.SetParent(mobile.interaction.transform.parent, false);
        notice.anchorMin = notice.anchorMax = new Vector2(.5f, .62f);
        notice.sizeDelta = new Vector2(650, 100);
        var background = notice.GetComponent<UnityEngine.UI.Image>();
        background.color = new Color(.04f, .15f, .12f, .94f);
        background.raycastTarget = false;
        ui.toast = notice.GetComponent<CanvasGroup>();
        ui.toast.alpha = 0;
        ui.toast.blocksRaycasts = false;
        ui.toast.interactable = false;
        var icon = (RectTransform)new GameObject(
            "StrengthIcon",
            typeof(RectTransform),
            typeof(UnityEngine.UI.Image)
        ).transform;
        icon.SetParent(notice, false);
        icon.anchorMin = icon.anchorMax = new Vector2(0, .5f);
        icon.anchoredPosition = new Vector2(48, 0);
        icon.sizeDelta = new Vector2(62, 62);
        var image = icon.GetComponent<UnityEngine.UI.Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Graphics/Sprites/strong.png");
        image.preserveAspect = true;
        image.raycastTarget = false;
        var message = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        message.transform.SetParent(notice, false);
        message.rectTransform.anchorMin = Vector2.zero;
        message.rectTransform.anchorMax = Vector2.one;
        message.rectTransform.offsetMin = new Vector2(92, 8);
        message.rectTransform.offsetMax = new Vector2(-18, -8);
        message.font = mobile.caption.font;
        message.fontSharedMaterial = mobile.caption.fontSharedMaterial;
        message.fontSize = 32;
        message.fontStyle = FontStyles.Bold;
        message.alignment = TextAlignmentOptions.Center;
        message.enableWordWrapping = false;
        message.enableAutoSizing = true;
        message.fontSizeMin = 16;
        message.fontSizeMax = 32;
        message.color = new Color(.73f, 1, .58f);
        message.raycastTarget = false;
        message.text = "Вы получили +250 силы!";
        ui.toastText = message;
        AddTreasureInterior();
        EditorUtility.SetDirty(reward);
        EditorUtility.SetDirty(animation);
        EditorUtility.SetDirty(ui);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
        return "Activity chest installed at (-7, .125, -9), 100 blocks per gift, +50 strength, +50 per five openings, mobile interaction, saved progress.";
    }

    public static void AddTreasureInterior()
    {
        var reward = Object.FindFirstObjectByType<ActivityChestReward>();
        if (reward.transform.Find("TreasureInterior1") != null)
            return;
        materials = new Material[]
        {
            AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Teal.mat"),
            AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Gold.mat"),
            AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Dark.mat"),
            AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Wood.mat"),
            AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Gem.mat")
        };
        var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube = temp.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(temp);
        Begin();
        Box(2, new Vector3(0, 1.54f, 0), new Vector3(2.7f, .025f, 1.65f));
        for (int i = 0; i < 9; i++)
            Box(
                1,
                new Vector3((i % 3 - 1) * .66f, 1.60f + (i % 2) * .04f, (i / 3 - 1) * .44f),
                new Vector3(.48f, .10f + (i % 3) * .035f, .37f),
                i * 29
            );
        Meshes(reward.transform, "TreasureInterior");
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(reward.gameObject.scene);
        EditorSceneManager.SaveScene(reward.gameObject.scene);
    }

    private static void Begin()
    {
        parts = new List<CombineInstance>[5];
        for (int i = 0; i < 5; i++)
            parts[i] = new List<CombineInstance>();
    }

    private static void Box(int material, Vector3 p, Vector3 s, float yaw = 0)
    {
        parts[material].Add(new CombineInstance { mesh = cube, transform = Matrix4x4.TRS(p, Quaternion.Euler(0, yaw, 0), s) });
    }

    private static void Meshes(Transform parent, string prefix)
    {
        for (int i = 0; i < 5; i++)
            if (parts[i].Count > 0)
            {
                var mesh = new Mesh
                {
                    name = prefix + i
                };
                mesh.CombineMeshes(parts[i].ToArray(), true, true);
                AssetDatabase.CreateAsset(mesh, Folder + "/" + prefix + i + ".asset");
                var go = new GameObject(prefix + i);
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = materials[i];
            }
    }

    private static Material Mat(string name, Color color, bool glow = false)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", .35f);
        if (glow)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * 1.5f);
        }

        AssetDatabase.CreateAsset(m, Folder + "/" + name + ".mat");
        return m;
    }
}
