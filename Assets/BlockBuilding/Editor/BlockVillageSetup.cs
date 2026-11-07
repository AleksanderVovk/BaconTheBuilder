using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BlockVillageSetup
{
    private const string Generated = "Assets/BlockBuilding/Generated";
    private const string AtlasPath = "Assets/BlockBuilding/Textures/VoxelAtlas.png";
    [MenuItem("Tools/Block Building/Create Large House and Three Trees")]
    public static void InstallVillage()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play Mode first.");
        var runtime = UnityEngine.Object.FindFirstObjectByType<BlockLevelRuntime>();
        if (runtime == null)
            throw new InvalidOperationException("Open the existing connected Lobby scene.");
        var texture = PackAtlas();
        var shader = Shader.Find("BlockBuilding/Pixel Atlas");
        if (shader == null)
            throw new InvalidOperationException("Pixel Atlas shader missing.");
        var material = GetMaterial(Generated + "/VoxelAtlas.mat", shader, texture, false);
        var ghost = GetMaterial(Generated + "/VoxelGhost.mat", shader, texture, true);
        Undo.RecordObject(runtime.data, "Create large house and trees");
        runtime.data.level = CreateVillage();
        runtime.data.useAtlas = true;
        runtime.data.blockMaterial = material;
        Undo.RecordObject(runtime, "Configure typed buildings");
        runtime.ghostMaterial = ghost;
        runtime.deliveryRadius = 2.5f;
        Undo.RecordObject(runtime.carrier, "Configure textured carrier");
        runtime.carrier.useAtlas = true;
        runtime.carrier.material = material;
        runtime.carrier.level = runtime;
        runtime.carrier.visualScale = 0.55f;
        EditorUtility.SetDirty(runtime.data);
        EditorUtility.SetDirty(runtime);
        EditorUtility.SetDirty(runtime.carrier);
        BlockBuildingInstaller.ApplyLevel(runtime.data);
        File.WriteAllText(Generated + "/HouseAndTrees.json", runtime.data.ToJson());
        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(runtime.gameObject.scene);
        Debug.Log("Large textured house and 3 buildable trees configured. Block size: 1.5 m. Save Lobby to keep changes.");
    }

    private static Material GetMaterial(string path, Shader shader, Texture2D texture, bool ghost)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }

        m.shader = shader;
        m.SetTexture("_BaseMap", texture);
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Ghost", ghost ? 1 : 0);
        EditorUtility.SetDirty(m);
        return m;
    }

    private static Texture2D PackAtlas()
    {
        return VoxelAtlasPacker.Rebuild();
    }

    public static BlockLevelDefinition CreateVillage()
    {
        var level = new BlockLevelDefinition
        {
            version = 2,
            blockSize = 1.5f,
            pickupPoint = new Vector3(-4, 0, 0),
            trainingPoint = new Vector3(4, 0, 0)
        };
        var house = new BlockStructure
        {
            id = "LargeHouse",
            origin = new Vector3(-9.75f, -1.45f, 21),
            deliveryPoint = new Vector3(0, 0, 16.75f)
        };
        var cells = new Dictionary<Vector3Int, BlockCell>();
        for (int z = 0; z < 11; z++)
            for (int x = 0; x < 13; x++)
                Put(cells, x, 0, z, BlockType.StoneBricks);
        for (int z = -2; z < 0; z++)
            for (int x = 4; x <= 8; x++)
                Put(cells, x, 0, z, BlockType.Cobblestone);
        for (int y = 1; y <= 5; y++)
            for (int z = 0; z < 11; z++)
                for (int x = 0; x < 13; x++)
                {
                    if (x != 0 && x != 12 && z != 0 && z != 10)
                        continue;
                    if (z == 0 && x >= 5 && x <= 7 && y <= 3)
                        continue; // Truly empty, walkable doorway.
                    bool beam = (x == 0 || x == 12) && (z == 0 || z == 10) || y == 5;
                    bool window = y >= 2
                        && y <= 3
                        && ((z == 0
                                || z == 10)
                            && (x >= 1
                                && x <= 3
                                || x >= 9
                                && x <= 11)
                            || (x == 0
                                || x == 12)
                            && (z >= 2
                                && z <= 4
                                || z >= 7
                                && z <= 8));
                    BlockType type = beam
                        ? BlockType.OakLog
                        : window
                        ? BlockType.Glass
                        : y == 1
                        ? BlockType.Cobblestone
                        : BlockType.OakPlanks;
                    Put(cells, x, y, z, type);
                }

        // Hollow stepped gable roof with a one-cell eave on all sides.
        for (int x = -1; x <= 13; x++)
        {
            int roofY = 6 + Mathf.Min(x + 1, 13 - x) / 2;
            for (int z = -1; z <= 11; z++)
                Put(cells, x, roofY, z, roofY == 9 ? BlockType.Slate : BlockType.RoofBricks);
            if (x < 0 || x > 12)
                continue;
            for (int y = 6; y < roofY; y++)
            {
                BlockType type = (x >= 5 && x <= 7 && y == 7) ? BlockType.Glass : BlockType.OakPlanks;
                Put(cells, x, y, 0, type);
                Put(cells, x, y, 10, type);
            }
        }

        // Modest masonry chimney, all part of the same data-driven structure.
        for (int y = 8; y <= 11; y++)
            Put(cells, 10, y, 7, BlockType.StoneBricks);
        Finish(house, cells);
        level.sites.Add(house);
        level.sites.Add(Tree("OakTree_1", new Vector3(-16.5f, 0, 28), 5, false));
        level.sites.Add(Tree("OakTree_2", new Vector3(16.5f, 0, 31), 6, false));
        level.sites.Add(Tree("BirchTree_3", new Vector3(14.5f, 0, 46), 6, true));
        level.Validate();
        return level;
    }

    private static BlockStructure Tree(string id, Vector3 origin, int height, bool birch)
    {
        var site = new BlockStructure
        {
            id = id,
            origin = origin,
            deliveryPoint = origin + new Vector3(0.75f, 0, -3.25f)
        };
        var cells = new Dictionary<Vector3Int, BlockCell>();
        for (int y = 0; y < height; y++)
            Put(cells, 0, y, 0, birch ? BlockType.BirchLog : BlockType.OakLog);
        for (int y = height - 2; y <= height + 1; y++)
        {
            int r = y >= height ? 1 : 2;
            for (int z = -r; z <= r; z++)
                for (int x = -r; x <= r; x++)
                {
                    if (Mathf.Abs(x) == r && Mathf.Abs(z) == r && r == 2)
                        continue;
                    if (x == 0 && z == 0 && y < height)
                        continue;
                    if (y == height + 1 && Mathf.Abs(x) + Mathf.Abs(z) > 1)
                        continue;
                    Put(cells, x, y, z, BlockType.Leaves);
                }
        }

        Finish(site, cells);
        return site;
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

    private static void Finish(BlockStructure site, Dictionary<Vector3Int, BlockCell> cells)
    {
        site.blocks.AddRange(cells.Values);
        site.blocks.Sort(BlockLevelDefinition.Compare);
        for (int i = 0; i < site.blocks.Count; i++)
            site.blocks[i].order = i;
    }

    public static void RebuildSceneVisuals(BlockLevelRuntime runtime)
    {
        var pickup = runtime.GetComponentInChildren<BlockPickupZone>();
        if (pickup != null)
        {
            var pile = AssetDatabase.LoadAssetAtPath<Mesh>(Generated + "/SupplyPile.asset");
            if (pile != null)
            {
                var scratch = new BlockMeshUtility();
                scratch.Begin();
                var occupied = new HashSet<Vector3Int>();
                var types = new Dictionary<Vector3Int, BlockType>();
                for (int z = 0; z < 3; z++)
                    for (int x = 0; x < 3; x++)
                    {
                        var p = new Vector3Int(x, 0, z);
                        occupied.Add(p);
                        types[p] = (x + z) % 3 == 0
                            ? BlockType.OakPlanks
                            : (x + z) % 3 == 1
                            ? BlockType.Cobblestone
                            : BlockType.RoofBricks;
                    }

                var top = new Vector3Int(1, 1, 1);
                occupied.Add(top);
                types[top] = BlockType.OakLog;
                foreach (var p in occupied)
                    scratch.Cell(p, 0.8f, occupied, types[p], types);
                scratch.Apply(pile);
                EditorUtility.SetDirty(pile);
                pickup.GetComponent<MeshRenderer>().sharedMaterial = runtime.data.blockMaterial;
            }
        }

        var hud = UnityEngine.Object.FindFirstObjectByType<BlockBuildingHUD>();
        if (hud != null && hud.statsText != null)
        {
            var panel = hud.statsText.rectTransform.parent as RectTransform;
            if (panel != null)
                panel.sizeDelta = new Vector2(560, 220);
            hud.statsText.rectTransform.sizeDelta = new Vector2(530, 112);
            if (hud.progress != null)
                ((RectTransform)hud.progress.transform).anchoredPosition = new Vector2(16, -132);
            if (hud.hintText != null)
                hud.hintText.rectTransform.anchoredPosition = new Vector2(16, -165);
        }

        RebuildPreview(runtime);
    }

    private static void RebuildPreview(BlockLevelRuntime runtime)
    {
        var old = runtime.transform.Find("DesignPreview");
        if (old != null)
            Undo.DestroyObjectImmediate(old.gameObject);
        var preview = new GameObject("DesignPreview");
        preview.tag = "EditorOnly";
        preview.transform.SetParent(runtime.transform, false);
        Undo.RegisterCreatedObjectUndo(preview, "Preview textured buildings");
        string folder = Generated + "/DesignPreview";
        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        var scratch = new BlockMeshUtility();
        foreach (var site in runtime.data.level.sites)
        {
            var occupied = new HashSet<Vector3Int>();
            var types = new Dictionary<Vector3Int, BlockType>();
            var chunks = new Dictionary<Vector3Int, List<BlockCell>>();
            foreach (var cell in site.blocks)
            {
                occupied.Add(cell.position);
                types[cell.position] = cell.type;
                var p = cell.position;
                var key = new Vector3Int(Mathf.FloorToInt(p.x / 8f), Mathf.FloorToInt(p.y / 8f), Mathf.FloorToInt(p.z / 8f));
                List<BlockCell> list;
                if (!chunks.TryGetValue(key, out list))
                {
                    list = new List<BlockCell>();
                    chunks[key] = list;
                }

                list.Add(cell);
            }

            var branch = new GameObject(site.id);
            branch.tag = "EditorOnly";
            branch.transform.SetParent(preview.transform, false);
            branch.transform.SetPositionAndRotation(site.origin, Quaternion.Euler(0, site.yaw, 0));
            foreach (var pair in chunks)
            {
                var key = pair.Key;
                string name = site.id + "_" + key.x + "_" + key.y + "_" + key.z;
                string path = folder + "/" + name + ".asset";
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (mesh == null)
                {
                    mesh = new Mesh
                    {
                        name = name
                    };
                    AssetDatabase.CreateAsset(mesh, path);
                }

                scratch.Begin();
                foreach (var cell in pair.Value)
                    scratch.Cell(cell.position, runtime.data.level.blockSize, occupied, cell.type, types);
                scratch.Apply(mesh);
                EditorUtility.SetDirty(mesh);
                var go = new GameObject(name);
                go.tag = "EditorOnly";
                go.transform.SetParent(branch.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = runtime.data.blockMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }
}
