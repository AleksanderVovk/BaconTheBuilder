using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public static class BlockBuildingInstaller
{
    private const string Folder = "Assets/BlockBuilding/Generated";
    [MenuItem("Tools/Block Building/Connect to Existing Lobby")]
    public static void Install()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play mode first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "Lobby")
            throw new InvalidOperationException("Open the existing Lobby scene first.");
        var player = GameObject.Find("Player");
        if (player == null)
            throw new InvalidOperationException("Player object not found.");
        var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
            throw new InvalidOperationException("Existing Canvas not found.");
        if (GameObject.Find("BlockBuildingSystem") != null)
            throw new InvalidOperationException("Already connected. Use Level Editor > Apply Positions / Visuals.");
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        // Keep a recoverable pre-integration scene copy outside Assets.
        string backup = "BlockBuildingBackup/Lobby.before-block-building.unity";
        Directory.CreateDirectory("BlockBuildingBackup");
        if (!File.Exists(backup))
            File.Copy(scene.path, backup);
        var blockMaterial = MaterialAsset("Blocks", new Color(1, 0.65f, 0.12f));
        var cyan = MaterialAsset("Guide", new Color(0.05f, 0.85f, 1));
        var benchMaterial = MaterialAsset("Bench", new Color(0.12f, 0.2f, 0.28f));
        var ghostMaterial = MaterialAsset("Ghost", new Color(0.1f, 0.9f, 1, 0.32f), true);
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var cubeMesh = cube.GetComponent<MeshFilter>().sharedMesh;
        cube.name = "BlockVisual";
        cube.GetComponent<MeshRenderer>().sharedMaterial = blockMaterial;
        UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
        string prefabPath = Folder + "/BlockVisual.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            prefab = PrefabUtility.SaveAsPrefabAsset(cube, prefabPath);
        UnityEngine.Object.DestroyImmediate(cube);
        var data = AssetDatabase.LoadAssetAtPath<BuildSiteData>(Folder + "/LobbyBlockLevel.asset");
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<BuildSiteData>();
            data.level = BlockLevelDefinition.Pyramid();
            data.blockMaterial = blockMaterial;
            data.blockPrefab = prefab;
            AssetDatabase.CreateAsset(data, Folder + "/LobbyBlockLevel.asset");
        }

        File.WriteAllText(Folder + "/Pyramid.json", data.ToJson());
        var system = New("BlockBuildingSystem", null);
        Undo.RegisterCreatedObjectUndo(system, "Connect block building");
        var strength = Undo.AddComponent<StrengthUpgradeSystem>(player);
        strength.levelAudio = Undo.AddComponent<AudioSource>(player);
        strength.levelAudio.playOnAwake = false;
        strength.levelAudio.spatialBlend = 0;
        var carrier = Undo.AddComponent<PlayerBlockCarrier>(player);
        carrier.player = player.transform;
        carrier.strength = strength;
        carrier.blockMesh = cubeMesh;
        carrier.material = data.blockMaterial;
        var anchor = New("BlockCarryAnchor", player.transform);
        anchor.transform.localPosition = new Vector3(0, 1.5f, 0.4f);
        carrier.carryAnchor = anchor.transform;
        var pathObject = New("BuildPath", system.transform);
        var line = pathObject.AddComponent<LineRenderer>();
        line.sharedMaterial = cyan;
        line.startWidth = 0.055f;
        line.endWidth = 0.055f;
        line.numCapVertices = 2;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var path = pathObject.AddComponent<BuildPathLine>();
        path.source = player.transform;
        path.line = line;
        var runtime = system.AddComponent<BlockLevelRuntime>();
        runtime.data = data;
        runtime.carrier = carrier;
        runtime.path = path;
        runtime.cubeMesh = cubeMesh;
        runtime.ghostMaterial = ghostMaterial;
        var pile = New("PickupPile", system.transform);
        var pickup = pile.AddComponent<BlockPickupZone>();
        pickup.carrier = carrier;
        pile.AddComponent<MeshFilter>();
        pile.AddComponent<MeshRenderer>().sharedMaterial = data.blockMaterial;
        var bench = New("StrengthBench", system.transform);
        var training = bench.AddComponent<StrengthTrainingZone>();
        training.carrier = carrier;
        training.strength = strength;
        var controller = Array.Find(player.GetComponents<MonoBehaviour>(), component => component != null
            && component.GetType().Name == "PlayerController");
        training.movementToPause = controller != null ? new Behaviour[]
        {
            controller
        }

        : new Behaviour[0];
        var animator = player.GetComponentInChildren<Animator>();
        training.modelAnimator = animator;
        if (animator != null && animator.transform != player.transform)
            training.visualModel = animator.transform;
        Primitive("Seat", bench.transform, new Vector3(0, 0.55f, 0), new Vector3(0.8f, 0.16f, 2), benchMaterial, false);
        Primitive(
            "LegA",
            bench.transform,
            new Vector3(0, 0.25f, -0.65f),
            new Vector3(0.65f, 0.5f, 0.15f),
            benchMaterial,
            false
        );
        Primitive(
            "LegB",
            bench.transform,
            new Vector3(0, 0.25f, 0.65f),
            new Vector3(0.65f, 0.5f, 0.15f),
            benchMaterial,
            false
        );
        var pose = New("LyingPose", bench.transform);
        pose.transform.localPosition = new Vector3(0, 0.7f, 0.8f);
        pose.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        training.poseAnchor = pose.transform;
        var bar = New("Barbell", bench.transform);
        bar.transform.localPosition = new Vector3(0, 1.1f, -0.35f);
        training.barbell = bar.transform;
        Primitive("Bar", bar.transform, Vector3.zero, new Vector3(1.6f, 0.06f, 0.06f), cyan, false);
        Primitive(
            "LeftWeight",
            bar.transform,
            new Vector3(-0.7f, 0, 0),
            new Vector3(0.16f, 0.4f, 0.4f),
            benchMaterial,
            false
        );
        Primitive(
            "RightWeight",
            bar.transform,
            new Vector3(0.7f, 0, 0),
            new Vector3(0.16f, 0.4f, 0.4f),
            benchMaterial,
            false
        );
        CreateHUD(canvas, strength, carrier, training, pickup, runtime);
        ApplyLevel(data);
        EditorUtility.SetDirty(carrier);
        EditorUtility.SetDirty(strength);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Block Building connected to existing Lobby. Save scene after reviewing.");
    }

    public static void ApplyLevel(BuildSiteData data)
    {
        data.level.Validate();
        if (data.blockMaterial == null)
            throw new InvalidOperationException("Assign Block Material in the level asset first.");
        var runtime = UnityEngine.Object.FindFirstObjectByType<BlockLevelRuntime>();
        if (runtime == null)
            throw new InvalidOperationException("Connect the scene first.");
        Undo.RecordObject(runtime, "Apply block level");
        runtime.data = data;
        if (data.blockMaterial != null)
            runtime.carrier.material = data.blockMaterial;
        if (data.blockPrefab != null)
        {
            var mesh = data.blockPrefab.GetComponentInChildren<MeshFilter>();
            if (mesh != null && mesh.sharedMesh != null)
            {
                if (!mesh.sharedMesh.isReadable || mesh.sharedMesh.subMeshCount != 1)
                    throw new InvalidOperationException("Carried prefab mesh needs Read/Write enabled and exactly one submesh.");
                runtime.carrier.blockMesh = mesh.sharedMesh;
            }
        }

        var pickup = runtime.GetComponentInChildren<BlockPickupZone>();
        var training = runtime.GetComponentInChildren<StrengthTrainingZone>();
        if (pickup != null)
        {
            Undo.RecordObject(pickup.transform, "Move pile");
            pickup.transform.position = data.level.pickupPoint;
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/SupplyPile.asset");
            if (mesh == null)
            {
                mesh = new Mesh
                {
                    name = "Supply pile"
                };
                AssetDatabase.CreateAsset(mesh, Folder + "/SupplyPile.asset");
            }

            var scratch = new BlockMeshUtility();
            scratch.Begin();
            var occupied = new System.Collections.Generic.HashSet<Vector3Int>();
            for (int z = 0; z < 3; z++)
                for (int x = 0; x < 3; x++)
                    occupied.Add(new Vector3Int(x, 0, z));
            occupied.Add(new Vector3Int(1, 1, 1));
            foreach (var cell in occupied)
                scratch.Cell(cell, 0.5f, occupied);
            scratch.Apply(mesh);
            EditorUtility.SetDirty(mesh);
            pickup.GetComponent<MeshFilter>().sharedMesh = mesh;
            pickup.GetComponent<MeshRenderer>().sharedMaterial = data.blockMaterial;
        }

        if (training != null)
        {
            Undo.RecordObject(training.transform, "Move bench");
            training.transform.position = data.level.trainingPoint;
        }

        // Delivery markers are only a handful of scene objects, never one per cell.
        var markers = runtime.transform.Find("DeliveryMarkers");
        if (markers == null)
            markers = New("DeliveryMarkers", runtime.transform).transform;
        for (int i = markers.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(markers.GetChild(i).gameObject);
        var markerMaterial = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Guide.mat");
        foreach (var site in data.level.sites)
        {
            var marker = Primitive("Deliver_"
                + site.id, markers, Vector3.zero, new Vector3(1.2f, 0.03f, 1.2f), markerMaterial, false);
            marker.transform.position = site.deliveryPoint + Vector3.up * 0.025f;
        }

        runtime.carrier.useAtlas = data.useAtlas;
        if (data.useAtlas)
            BlockVillageSetup.RebuildSceneVisuals(runtime);
        EditorUtility.SetDirty(runtime);
        EditorUtility.SetDirty(runtime.carrier);
        EditorSceneManager.MarkSceneDirty(runtime.gameObject.scene);
        SceneView.RepaintAll();
    }

    private static Material MaterialAsset(string name, Color color, bool transparent = false)
    {
        string path = Folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
            return material;
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            throw new InvalidOperationException("URP Unlit shader not found.");
        material = new Material(shader)
        {
            name = name,
            enableInstancing = true
        };
        material.SetColor("_BaseColor", color);
        if (transparent)
        {
            material.SetFloat("_Surface", 1);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
            material.SetOverrideTag("RenderType", "Transparent");
        }

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static GameObject New(string name, Transform parent)
    {
        var go = new GameObject(name);
        if (parent != null)
            go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject Primitive(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool collision)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (!collision)
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static TMP_Text Text(string name, Transform parent, string value, Vector2 position, Vector2 size, float fontSize)
    {
        var rect = Rect(name, parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), position, size);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.raycastTarget = false;
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/defaultFont.asset");
        if (font != null)
            text.font = font;
        return text;
    }

    private static UnityEngine.UI.Button Button(Transform parent, string name, string caption, Vector2 position)
    {
        var rect = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero, position, new Vector2(210, 72));
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.08f, 0.4f, 0.48f, 0.95f);
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        var text = Text("Label", rect, caption, new Vector2(0, 0), new Vector2(210, 72), 28);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private static void CreateHUD(Canvas canvas, StrengthUpgradeSystem strength, PlayerBlockCarrier carrier, StrengthTrainingZone training, BlockPickupZone pickup, BlockLevelRuntime level)
    {
        if (canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            Undo.AddComponent<UnityEngine.UI.GraphicRaycaster>(canvas.gameObject);
        var root = Rect(
            "BlockBuildingHUD",
            canvas.transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero
        );
        var hud = root.gameObject.AddComponent<BlockBuildingHUD>();
        hud.strength = strength;
        hud.carrier = carrier;
        hud.training = training;
        hud.pickup = pickup;
        hud.level = level;
        var panel = Rect(
            "StatsPanel",
            root,
            new Vector2(0, 1),
            new Vector2(0, 1),
            new Vector2(0, 1),
            new Vector2(24, -24),
            new Vector2(560, 180)
        );
        var background = panel.gameObject.AddComponent<UnityEngine.UI.Image>();
        background.color = new Color(0.03f, 0.07f, 0.1f, 0.85f);
        background.raycastTarget = false;
        hud.statsText = Text(
            "StrengthText",
            panel,
            "Сила: 0   Уровень: 1\nКубики: 0 / 1",
            new Vector2(16, -12),
            new Vector2(530, 76),
            27
        );
        hud.hintText = Text(
            "Hint",
            panel,
            "E - взять кубик   F - жим лёжа",
            new Vector2(16, -132),
            new Vector2(530, 44),
            22
        );
        var track = Rect(
            "StrengthProgress",
            panel,
            new Vector2(0, 1),
            new Vector2(0, 1),
            new Vector2(0, 1),
            new Vector2(16, -100),
            new Vector2(526, 18)
        );
        track.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.13f, 0.2f, 0.25f);
        var fill = Rect("Fill", track, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        fill.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.2f, 0.9f, 0.6f);
        var slider = track.gameObject.AddComponent<UnityEngine.UI.Slider>();
        slider.fillRect = fill;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.interactable = false;
        slider.transition = UnityEngine.UI.Selectable.Transition.None;
        hud.progress = slider;
        track.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        fill.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        // Place controls away from the existing bottom-left joystick.
        var controls = Rect(
            "BuildingControls",
            root,
            new Vector2(1, 0),
            new Vector2(1, 0),
            new Vector2(1, 0),
            new Vector2(-28, 180),
            new Vector2(450, 72)
        );
        hud.pickupButton = Button(controls, "Pickup", "Взять [E]", Vector2.zero);
        hud.trainButton = Button(controls, "Train", "Жим [F]", new Vector2(230, 0));
        var flash = Rect(
            "LevelFlash",
            panel,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero
        );
        hud.levelFlash = flash.gameObject.AddComponent<UnityEngine.UI.Image>();
        hud.levelFlash.color = Color.clear;
        hud.levelFlash.raycastTarget = false;
    }
}
