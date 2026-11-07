using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildCampaignSetup
{
    private const string Folder = "Assets/BlockBuilding/Generated/Campaign";
    private static readonly string[] Ids =
    {
        "house",
        "fortress",
        "skyscraper",
        "pyramid",
        "volcano",
        "earth"
    };
    private static readonly string[] Titles =
    {
        "Дом и три дерева",
        "Крепость короля",
        "Небоскрёб",
        "Пирамида Хеопса",
        "Вулкан",
        "Планета Земля"
    };
    [MenuItem("Tools/Block Building/Install Six Level Menu")]
    public static string Install()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play mode first.");
        var runtime = UnityEngine.Object.FindFirstObjectByType<BlockLevelRuntime>();
        var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (runtime == null || canvas == null)
            throw new InvalidOperationException("Open the existing Lobby scene.");
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        var catalog = AssetDatabase.LoadAssetAtPath<BuildLevelCatalog>(Folder + "/LevelCatalog.asset");
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<BuildLevelCatalog>();
            AssetDatabase.CreateAsset(catalog, Folder + "/LevelCatalog.asset");
        }

        catalog.levels = new BuildLevelEntry[6];
        var original = AssetDatabase.LoadAssetAtPath<BuildSiteData>("Assets/BlockBuilding/Generated/LobbyBlockLevel.asset");
        for (int i = 0; i < 6; i++)
        {
            BuildSiteData data = original;
            if (i > 0)
            {
                string path = Folder + "/" + Ids[i] + ".asset";
                data = AssetDatabase.LoadAssetAtPath<BuildSiteData>(path);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<BuildSiteData>();
                    AssetDatabase.CreateAsset(data, path);
                }

                data.level = Create(i);
                data.blockMaterial = original.blockMaterial;
                data.blockPrefab = original.blockPrefab;
                data.useAtlas = true;
                EditorUtility.SetDirty(data);
            }

            data.level.Validate();
            File.WriteAllText(Folder + "/" + Ids[i] + ".json", data.ToJson());
            catalog.levels[i] = new BuildLevelEntry
            {
                id = Ids[i],
                title = Titles[i],
                data = data,
                icon = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/" + Ids[i] + "_icon.png"),
                stairs = i == 2,
                stairOrigin = new Vector3(12, 0, 24),
                stairHeight = 63
            };
        }

        for (int i = 1; i < 6; i++)
            if (catalog.levels[i].BlockCount <= catalog.levels[i - 1].BlockCount)
                throw new InvalidOperationException("Counts must increase: " + i);
        runtime.autoLoadOnStart = false;
        runtime.pickupTransform = runtime.transform.Find("PickupPile");
        runtime.trainingTransform = runtime.transform.Find("StrengthBench");
        var markers = runtime.transform.Find("DeliveryMarkers");
        if (markers != null)
            markers.gameObject.SetActive(false);
        var session = runtime.GetComponent<BuildLevelSession>();
        if (session == null)
            session = runtime.gameObject.AddComponent<BuildLevelSession>();
        session.catalog = catalog;
        session.runtime = runtime;
        session.training = UnityEngine.Object.FindFirstObjectByType<StrengthTrainingZone>();
        session.gameplayHUD = canvas.transform.Find("BlockBuildingHUD").gameObject;
        var controls = new List<Behaviour>(session.training.movementToPause);
        controls.Add(session.training);
        controls.Add(runtime.pickupTransform.GetComponent<BlockPickupZone>());
        var existingInput = runtime.carrier.player.GetComponent<InputsManager>();
        if (existingInput != null)
            controls.Add(existingInput);
        session.onPlayerReset = new UnityEngine.Events.UnityEvent();
        session.onMenuOpened = new UnityEngine.Events.UnityEvent();
        var existingController = runtime.carrier.player.GetComponent<PlayerController>();
        if (existingController != null)
            UnityEditor.Events.UnityEventTools.AddPersistentListener(session.onPlayerReset, existingController.ResetForBuildLevel);
        if (existingInput != null)
            UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(session.onMenuOpened, existingInput.ScreenClamped, false);
        session.gameplayBehaviours = controls.Where(b => b != null).Distinct().ToArray();
        var stairObject = runtime.transform.Find("LevelAccessStairs");
        if (stairObject == null)
        {
            stairObject = new GameObject("LevelAccessStairs").transform;
            stairObject.SetParent(runtime.transform, false);
        }

        session.accessStairs = stairObject.GetComponent<LevelAccessStairway>();
        if (session.accessStairs == null)
            session.accessStairs = stairObject.gameObject.AddComponent<LevelAccessStairway>();
        session.accessStairs.gameObject.SetActive(false);
        CreateUI(canvas, session);
        EditorUtility.SetDirty(runtime);
        EditorUtility.SetDirty(session);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(runtime.gameObject.scene);
        EditorSceneManager.SaveScene(runtime.gameObject.scene);
        return string.Join("\n", catalog.levels.Select(e => e.title + ": " + e.BlockCount));
    }

    private static BlockLevelDefinition Create(int index)
    {
        var level = new BlockLevelDefinition
        {
            blockSize = 1.5f,
            pickupPoint = new Vector3(-4, 0, 0),
            trainingPoint = new Vector3(4, 0, 0)
        };
        var site = new BlockStructure
        {
            id = Ids[index],
            origin = new Vector3(0, -1.45f, 45),
            deliveryPoint = new Vector3(0, 0, 12)
        };
        var cells = new Dictionary<Vector3Int, BlockCell>();
        if (index == 1)
        {
            // Broad courtyard, four crenellated towers, open gate and a flag.
            for (int z = -9; z <= 9; z++)
                for (int x = -9; x <= 9; x++)
                    Put(cells, x, 0, z, BlockType.Cobblestone);
            for (int y = 1; y <= 6; y++)
                for (int z = -9; z <= 9; z++)
                    for (int x = -9; x <= 9; x++)
                    {
                        if (Mathf.Abs(x) != 9 && Mathf.Abs(z) != 9)
                            continue;
                        if (z == -9 && Mathf.Abs(x) <= 2 && y <= 4)
                            continue;
                        Put(cells, x, y, z, BlockType.StoneBricks);
                    }

            for (int x = -9; x <= 9; x += 2)
            {
                Put(cells, x, 7, -9, BlockType.Cobblestone);
                Put(cells, x, 7, 9, BlockType.Cobblestone);
            }

            for (int z = -7; z <= 7; z += 2)
            {
                Put(cells, -9, 7, z, BlockType.Cobblestone);
                Put(cells, 9, 7, z, BlockType.Cobblestone);
            }

            foreach (int cx in new[]
            {
                -8,
                8
            }

            )
                foreach (int cz in new[]
                {
                    -8,
                    8
                }

                )
                    for (int y = 1; y <= 10; y++)
                        for (int z = cz - 2; z <= cz + 2; z++)
                            for (int x = cx - 2; x <= cx + 2; x++)
                            {
                                bool edge = Mathf.Abs(x - cx) == 2 || Mathf.Abs(z - cz) == 2;
                                if (y < 9 && !edge)
                                    continue;
                                if (y == 10 && (!edge || (x + z) % 2 != 0))
                                    continue;
                                Put(cells, x, y, z, y == 9 ? BlockType.OakPlanks : BlockType.StoneBricks);
                            }

            for (int y = 10; y <= 16; y++)
                Put(cells, 8, y, 8, BlockType.OakLog);
            for (int y = 13; y <= 16; y++)
                for (int x = 9; x <= 14; x++)
                    Put(cells, x, y, 8, (y == 14 && x <= 11) ? BlockType.FlagGold : BlockType.FlagRed);
            site.origin.z = 34;
            site.deliveryPoint = new Vector3(0, 0, 15);
        }

        if (index == 2)
        {
            for (int y = 0; y <= 36; y++)
                for (int z = -5; z <= 5; z++)
                    for (int x = -5; x <= 5; x++)
                    {
                        bool floor = y % 6 == 0, edge = Mathf.Abs(x) == 5 || Mathf.Abs(z) == 5;
                        if (!floor && !edge)
                            continue;
                        if (z == -5 && Mathf.Abs(x) <= 1 && y >= 1 && y <= 3)
                            continue;
                        Put(cells, x, y, z, floor
                            ? BlockType.StoneBricks
                            : (((Mathf.Abs(x) == 5
                                        && (z == 0
                                            || Mathf.Abs(z) == 5))
                                    || (Mathf.Abs(z) == 5
                                        && x == 0))
                            ? BlockType.Slate
                            : BlockType.Glass));
                    }

            for (int y = 37; y <= 41; y++)
                Put(cells, 0, y, 0, BlockType.FlagGold);
            site.origin.z = 31;
            site.deliveryPoint = new Vector3(0, 0, 17);
        }

        if (index == 3)
        {
            for (int y = 0; y < 13; y++)
                for (int z = -12 + y; z <= 12 - y; z++)
                    for (int x = -12 + y; x <= 12 - y; x++)
                        Put(cells, x, y, z, BlockType.Sandstone);
            // Entrance recess is a real empty gap, rather than a painted door.
            cells.Remove(new Vector3Int(0, 1, -11));
            cells.Remove(new Vector3Int(0, 2, -10));
            site.origin.z = 38;
            site.deliveryPoint = new Vector3(0, 0, 16);
        }

        if (index == 4)
        {
            for (int y = 0; y <= 23; y++)
            {
                float r = 18 - y * 0.53f, inner = Mathf.Max(0, r - 4.5f);
                for (int z = -19; z <= 19; z++)
                    for (int x = -19; x <= 19; x++)
                    {
                        float d = Mathf.Sqrt(x * x + z * z);
                        float uneven = 0.6f * Mathf.Sin(x * 0.8f + z * 0.4f) + 0.4f * Mathf.Cos(z * 1.2f);
                        if (d > r + uneven || (y > 0 && d < inner))
                            continue;
                        float angle = Mathf.Atan2(z, x);
                        bool lava = (Mathf.Abs(angle + 1.4f) < 0.12f || Mathf.Abs(angle - 0.5f) < 0.08f) && d > r - 1.5f;
                        Put(cells, x, y, z, lava || y >= 21 ? BlockType.Lava : BlockType.Basalt);
                    }
            }

            for (int z = -4; z <= 4; z++)
                for (int x = -4; x <= 4; x++)
                    if (x * x + z * z <= 16)
                        Put(cells, x, 20, z, BlockType.Lava);
            site.origin.z = 43;
            site.deliveryPoint = new Vector3(0, 0, 12);
        }

        if (index == 5)
        {
            const float r = 20;
            for (int y = 0; y <= 40; y++)
                for (int z = -20; z <= 20; z++)
                    for (int x = -20; x <= 20; x++)
                    {
                        var p = new Vector3(x, y - 20, z);
                        float d = p.magnitude;
                        if (d > r || d < r - 2.0f)
                            continue;
                        float lat = Mathf.Asin(p.y / Mathf.Max(d, 0.001f)) * Mathf.Rad2Deg;
                        float lon = Mathf.Atan2(p.x, -p.z) * Mathf.Rad2Deg;
                        bool land = Continent(lon, lat, -105, 43, 35, 27)
                            || Continent(lon, lat, -60, -18, 20, 37)
                            || Continent(lon, lat, 15, 5, 25, 34)
                            || Continent(lon, lat, 70, 48, 66, 22)
                            || Continent(lon, lat, 112, 12, 22, 22)
                            || Continent(lon, lat, 135, -26, 23, 17)
                            || Continent(lon, lat, -42, 72, 14, 15);
                        Put(cells, x, y, z, Mathf.Abs(lat) > 73 ? BlockType.Snow : land ? BlockType.Land : BlockType.Ocean);
                    }

            site.origin.z = 48;
            site.deliveryPoint = new Vector3(0, 0, 12);
        }

        site.blocks = cells.Values.OrderBy(b => b.position.y).ThenBy(b => b.position.z).ThenBy(b => b.position.x).ToList();
        for (int i = 0; i < site.blocks.Count; i++)
            site.blocks[i].order = i;
        level.sites.Add(site);
        level.Validate();
        return level;
    }

    private static bool Continent(float lon, float lat, float x, float y, float rx, float ry)
    {
        float dx = Mathf.DeltaAngle(x, lon) / rx, dy = (lat - y) / ry;
        return dx * dx + dy * dy < 1 + 0.16f * Mathf.Sin(lon * 0.35f + lat * 0.6f);
    }

    private static void Put(Dictionary<Vector3Int, BlockCell> cells, int x, int y, int z, BlockType type)
    {
        var p = new Vector3Int(x, y, z);
        cells[p] = new BlockCell
        {
            position = p,
            type = type
        };
    }

    [MenuItem("Tools/Block Building/Render Level Icons")]
    public static void RenderIcons()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<BuildLevelCatalog>(Folder + "/LevelCatalog.asset");
        for (int i = 0; i < catalog.levels.Length; i++)
            catalog.levels[i].icon = RenderIcon(catalog.levels[i]);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
    }

    private static Sprite RenderIcon(BuildLevelEntry entry)
    {
        string path = Folder + "/" + entry.id + "_icon.png";
        var root = new GameObject("Icon render temporary");
        root.hideFlags = HideFlags.HideAndDontSave;
        var meshes = new List<Mesh>();
        var bounds = new Bounds();
        bool hasBounds = false;
        var cameraObject = new GameObject("Icon camera temporary");
        cameraObject.hideFlags = HideFlags.HideAndDontSave;
        var camera = cameraObject.AddComponent<Camera>();
        var rt = new RenderTexture(640, 400, 24, RenderTextureFormat.ARGB32);
        var old = RenderTexture.active;
        try
        {
            foreach (var site in entry.data.level.sites)
            {
                var mesh = new Mesh
                {
                    indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
                };
                meshes.Add(mesh);
                var scratch = new BlockMeshUtility();
                scratch.Begin();
                var occupied = new HashSet<Vector3Int>(site.blocks.Select(b => b.position));
                var types = site.blocks.ToDictionary(b => b.position, b => b.type);
                foreach (var cell in site.blocks)
                    scratch.Cell(cell.position, entry.data.level.blockSize, occupied, cell.type, types);
                scratch.Apply(mesh);
                var go = new GameObject("Icon mesh");
                go.hideFlags = HideFlags.HideAndDontSave;
                go.layer = 31;
                go.transform.SetParent(root.transform, false);
                go.transform.SetPositionAndRotation(site.origin, Quaternion.Euler(0, site.yaw, 0));
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = entry.data.blockMaterial;
                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                    bounds.Encapsulate(renderer.bounds);
            }

            camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0, 0, 0, 0);
            camera.orthographic = true;
            camera.targetTexture = rt;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500;
            Vector3 direction = new Vector3(-1.3f, 0.85f, -1.5f).normalized;
            camera.transform.position = bounds.center + direction * 160;
            camera.transform.LookAt(bounds.center);
            float maxX = 0, maxY = 0;
            var ext = bounds.extents;
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 p = camera.transform.InverseTransformPoint(bounds.center + Vector3.Scale(ext, new Vector3(x, y, z)));
                        maxX = Mathf.Max(maxX, Mathf.Abs(p.x));
                        maxY = Mathf.Max(maxY, Mathf.Abs(p.y));
                    }

            camera.orthographicSize = Mathf.Max(maxY, maxX / (640f / 400)) * 1.08f;
            camera.Render();
            RenderTexture.active = rt;
            var texture = new Texture2D(640, 400, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, 640, 400), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
        finally
        {
            RenderTexture.active = old;
            camera.targetTexture = null;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            foreach (var mesh in meshes)
                UnityEngine.Object.DestroyImmediate(mesh);
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.maxTextureSize = 1024;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static GameObject UI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static RectTransform Place(GameObject go, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = r.pivot = anchor;
        r.anchoredPosition = position;
        r.sizeDelta = size;
        return r;
    }

    private static UnityEngine.UI.Image Panel(GameObject go, Color color, bool raycast = false)
    {
        var image = go.AddComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    private static TMP_FontAsset font;
    private static TMP_Text Text(string name, Transform parent, string value, int size, Vector2 position, Vector2 dimensions)
    {
        var go = UI(name, parent);
        Place(go, new Vector2(0, 1), position, dimensions);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = Color.white;
        text.raycastTarget = false;
        text.enableAutoSizing = true;
        text.fontSizeMin = size * 0.7f;
        text.fontSizeMax = size;
#if UNITY_6000_0_OR_NEWER
        text.textWrappingMode = TextWrappingModes.Normal;
#else
        text.enableWordWrapping=true;
#endif
        text.overflowMode = TextOverflowModes.Truncate;
        return text;
    }

    private static UnityEngine.UI.Button Button(string name, Transform parent, string label, Vector2 position, Vector2 size)
    {
        var go = UI(name, parent);
        Place(go, new Vector2(0, 1), position, size);
        var image = Panel(go, new Color(0.18f, 0.29f, 0.38f), true);
        var button = go.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(1, 0.84f, 0.58f);
        colors.pressedColor = new Color(0.65f, 0.75f, 0.85f);
        button.colors = colors;
        var text = Text("Label", go.transform, label, 24, Vector2.zero, size);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private static void Stretch(GameObject go, Vector2 min, Vector2 max)
    {
        var r = (RectTransform)go.transform;
        r.anchorMin = min;
        r.anchorMax = max;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    private static void CreateUI(Canvas canvas, BuildLevelSession session)
    {
        var old = canvas.transform.Find("BuildLevelInterface");
        if (old != null)
            UnityEngine.Object.DestroyImmediate(old.gameObject);
        font = session.gameplayHUD.GetComponentInChildren<TMP_Text>(true).font;
        var root = UI("BuildLevelInterface", canvas.transform);
        Stretch(root, Vector2.zero, Vector2.one);
        var controller = root.AddComponent<BuildLevelMenu>();
        controller.session = session;
        var menu = UI("LevelMenu", root.transform);
        Stretch(menu, Vector2.zero, Vector2.one);
        Panel(menu, new Color(0.035f, 0.065f, 0.10f, 0.99f), true);
        controller.menu = menu;
        Text("Heading", menu.transform, "ПОСТРОЙ СВОЙ МИР", 44, new Vector2(70, -36), new Vector2(1100, 64));
        var subtitle = Text(
            "Subtitle",
            menu.transform,
            "6 построек  /  Выбери уровень и побей свой рекорд",
            24,
            new Vector2(74, -105),
            new Vector2(1200, 40)
        );
        subtitle.color = new Color(0.61f, 0.72f, 0.8f);
        var grid = UI("Cards", menu.transform);
        var gr = (RectTransform)grid.transform;
        gr.anchorMin = new Vector2(0, 0);
        gr.anchorMax = Vector2.one;
        gr.offsetMin = new Vector2(70, 95);
        gr.offsetMax = new Vector2(-70, -170);
        var layout = grid.AddComponent<UnityEngine.UI.GridLayoutGroup>();
        layout.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 3;
        layout.spacing = new Vector2(24, 24);
        var adaptive = grid.AddComponent<LevelMenuGrid>();
        adaptive.layout = layout;
        controller.cards = new BuildLevelCard[6];
        for (int i = 0; i < 6; i++)
        {
            var go = UI("Level_" + Ids[i], grid.transform);
            var image = Panel(go, new Color(0.085f, 0.135f, 0.185f), true);
            var button = go.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            var card = go.AddComponent<BuildLevelCard>();
            card.button = button;
            var iconBack = UI("IconBackdrop", go.transform);
            var ir = (RectTransform)iconBack.transform;
            ir.anchorMin = new Vector2(0.025f, 0.40f);
            ir.anchorMax = new Vector2(0.975f, 0.98f);
            ir.offsetMin = ir.offsetMax = Vector2.zero;
            Panel(iconBack, new Color(0.16f, 0.24f, 0.30f));
            var icon = UI("Icon", iconBack.transform);
            Stretch(icon, Vector2.zero, Vector2.one);
            card.icon = Panel(icon, Color.black);
            card.icon.preserveAspect = true;
            card.title = Text("Title", go.transform, "", 27, new Vector2(20, -15), new Vector2(530, 42));
            // Anchored bottom text stays inside cards at different screen sizes.
            var tr = (RectTransform)card.title.transform;
            tr.anchorMin = new Vector2(0, 0.26f);
            tr.anchorMax = new Vector2(1, 0.26f);
            tr.offsetMin = new Vector2(20, -2);
            tr.offsetMax = new Vector2(-14, 40);
            card.details = Text("Details", go.transform, "", 21, new Vector2(20, -20), new Vector2(500, 64));
            tr = (RectTransform)card.details.transform;
            tr.anchorMin = new Vector2(0, 0.13f);
            tr.anchorMax = new Vector2(1, 0.13f);
            tr.offsetMin = new Vector2(20, -7);
            tr.offsetMax = new Vector2(-14, 55);
            card.status = Text("Status", go.transform, "", 16, Vector2.zero, new Vector2(530, 28));
            tr = (RectTransform)card.status.transform;
            tr.anchorMin = new Vector2(0, 0);
            tr.anchorMax = new Vector2(1, 0);
            tr.offsetMin = new Vector2(20, 10);
            tr.offsetMax = new Vector2(-14, 38);
            controller.cards[i] = card;
            card.Refresh(session.catalog.levels[i], i + 1);
        }

        Text(
            "Footer",
            menu.transform,
            "Чёрный силуэт - ещё не построено. Заверши уровень, чтобы открыть цветную иконку.",
            20,
            new Vector2(74, -1012),
            new Vector2(1450, 35)
        );
        controller.resume = Button("Resume", menu.transform, "ПРОДОЛЖИТЬ", new Vector2(1510, -45), new Vector2(330, 60));
        Place(controller.resume.gameObject, new Vector2(1, 1), new Vector2(-70, -45), new Vector2(330, 60));
        var footer = (RectTransform)menu.transform.Find("Footer");
        footer.anchorMin = new Vector2(0, 0);
        footer.anchorMax = new Vector2(1, 0);
        footer.offsetMin = new Vector2(74, 20);
        footer.offsetMax = new Vector2(-74, 65);
        var hud = UI("RunHUD", root.transform);
        Stretch(hud, Vector2.zero, Vector2.one);
        controller.runHUD = hud;
        var timerPanel = UI("TimerPanel", hud.transform);
        var rr = Place(timerPanel, new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(660, 105));
        Panel(timerPanel, new Color(0.035f, 0.065f, 0.1f, 0.9f));
        controller.timer = Text("Timer", timerPanel.transform, "00:00.00", 38, new Vector2(0, -5), new Vector2(660, 50));
        controller.timer.alignment = TextAlignmentOptions.Center;
        controller.progress = Text("Progress", timerPanel.transform, "", 22, new Vector2(10, -60), new Vector2(640, 35));
        controller.progress.alignment = TextAlignmentOptions.Center;
        controller.back = Button("Menu", hud.transform, "УРОВНИ  [Esc]", new Vector2(1570, -25), new Vector2(300, 65));
        Place(controller.back.gameObject, new Vector2(1, 1), new Vector2(-28, -25), new Vector2(300, 65));
        var result = UI("Completion", root.transform);
        Stretch(result, Vector2.zero, Vector2.one);
        Panel(result, new Color(0.02f, 0.04f, 0.08f, 0.96f), true);
        controller.result = result;
        var box = UI("ResultBox", result.transform);
        Place(box, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 600));
        controller.resultText = Text("ResultText", box.transform, "", 46, new Vector2(0, 0), new Vector2(900, 380));
        controller.resultText.alignment = TextAlignmentOptions.Center;
        controller.again = Button("Again", box.transform, "ЕЩЁ РАЗ", new Vector2(50, -430), new Vector2(370, 75));
        controller.levels = Button("Levels", box.transform, "ВСЕ УРОВНИ", new Vector2(480, -430), new Vector2(370, 75));
        hud.SetActive(false);
        result.SetActive(false);
        controller.resume.gameObject.SetActive(false);
        // Hide original gameplay widgets on the initial menu without changing the Canvas.
        session.gameplayHUD.SetActive(false);
    }
}
