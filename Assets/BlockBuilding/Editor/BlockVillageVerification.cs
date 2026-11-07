using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BlockVillageVerification
{
    private static void Check(bool valid, string message)
    {
        if (!valid)
            throw new Exception("Village verification: " + message);
    }

    [MenuItem("Tools/Block Building/Verify Textures and House Data")]
    public static void RunData()
    {
        var data = AssetDatabase.LoadAssetAtPath<BuildSiteData>("Assets/BlockBuilding/Generated/LobbyBlockLevel.asset");
        Check(data != null, "level asset");
        data.level.Validate();
        var level = data.level;
        Check(level.version == 2 && level.blockSize == 1.5f && level.sites.Count == 4, "four structures and larger cells");
        var house = level.sites[0];
        Check(house.blocks.Count == 605, "house block count");
        int glass = 0;
        foreach (var cell in house.blocks)
        {
            var p = cell.position;
            Check(!(p.z == 0 && p.x >= 5 && p.x <= 7 && p.y >= 1 && p.y <= 3), "empty doorway");
            Check(!(p.x >= 1 && p.x <= 11 && p.z >= 1 && p.z <= 9 && p.y >= 1 && p.y <= 5), "hollow room");
            if (cell.type == BlockType.Glass)
                glass++;
        }

        Check(glass > 30, "glass windows");
        Check(house.origin.y + level.blockSize > 0, "floor does not z-fight ground");
        for (int i = 1; i < 4; i++)
        {
            bool trunk = false, leaves = false;
            foreach (var cell in level.sites[i].blocks)
            {
                trunk |= cell.type == BlockType.OakLog || cell.type == BlockType.BirchLog;
                leaves |= cell.type == BlockType.Leaves;
            }

            Check(trunk && leaves, "buildable tree " + i);
        }

        var copy = ScriptableObject.CreateInstance<BuildSiteData>();
        try
        {
            copy.LoadJson(data.ToJson());
            Check(copy.level.sites[0].blocks.Count == 605
                && copy.level.sites[0].blocks.Exists(c => c.type == BlockType.Glass), "typed JSON roundtrip");
            copy.level.sites[0].blocks[0].type = (BlockType)99;
            bool rejected = false;
            try
            {
                copy.level.Validate();
            }
            catch (ArgumentException)
            {
                rejected = true;
            }

            Check(rejected, "invalid texture rejected");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(copy);
        }

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/BlockBuilding/Textures/VoxelAtlas.png");
        var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
        Check(texture.width == BlockPalette.AtlasSize
            && texture.height == BlockPalette.AtlasSize
            && importer.filterMode == FilterMode.Point
            && !importer.mipmapEnabled
            && importer.wrapMode == TextureWrapMode.Clamp, "pixel import settings");
        var scratch = new BlockMeshUtility();
        var mesh = new Mesh();
        try
        {
            var occupied = new HashSet<Vector3Int>
            {
                Vector3Int.zero,
                Vector3Int.right
            };
            var types = new Dictionary<Vector3Int, BlockType>
            {
                {
                    Vector3Int.zero,
                    BlockType.Cobblestone
                },
                {
                    Vector3Int.right,
                    BlockType.Glass
                }
            };
            scratch.Begin();
            foreach (var cell in occupied)
                scratch.Cell(cell, 1, occupied, types[cell], types);
            scratch.Apply(mesh);
            Check(mesh.vertexCount == 44, "opaque face stays visible behind glass");
            types[Vector3Int.zero] = BlockType.Glass;
            scratch.Begin();
            foreach (var cell in occupied)
                scratch.Cell(cell, 1, occupied, types[cell], types);
            scratch.Apply(mesh);
            Check(mesh.vertexCount == 40, "internal glass faces removed");
            scratch.Begin();
            scratch.Cell(Vector3Int.zero, 1, null, BlockType.OakLog);
            scratch.Apply(mesh);
            Rect side = BlockPalette.TileUV(2), top = BlockPalette.TileUV(3);
            Vector2[] uv = mesh.uv;
            Check(side.Contains(uv[0]) && top.Contains(uv[8]), "bark side and endgrain top use distinct tiles");
            for (int i = 0; i < uv.Length; i++)
                Check(uv[i].x > 0 && uv[i].x < 1 && uv[i].y > 0 && uv[i].y < 1, "UV inside padded atlas");
            Check(mesh.uv2.Length == 24 && mesh.colors32.Length == 24, "glass attributes present");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mesh);
        }

        foreach (var message in ShaderUtil.GetShaderMessages(data.blockMaterial.shader))
            Check(message.severity.ToString() != "Error", message.message);
        Debug.Log("BLOCK_VILLAGE_DATA_PASS: 605 house cells, 3 trees, hollow room, empty door, typed JSON, atlas UV/gutters, glass culling, shader.");
    }

    public static string PreparePickupCheck()
    {
        Check(EditorApplication.isPlaying, "enter Play Mode first");
        var runtime = UnityEngine.Object.FindFirstObjectByType<BlockLevelRuntime>();
        var carrier = runtime.carrier;
        carrier.strength.saveProgress = false;
        var preview = runtime.transform.Find("DesignPreview");
        Check(preview != null && !preview.gameObject.activeSelf, "editor preview hidden in play");
        Check(carrier.Count == 0 && runtime.CurrentSite.BuiltCount == 0, "fresh play session");
        Check(!runtime.CurrentSite.TryBuild(carrier), "empty hands cannot build");
        Check(carrier.TryPickup(BlockType.OakLog), "test mismatched cargo");
        Check(!runtime.CurrentSite.TryBuild(carrier) && carrier.Count == 1, "wrong texture cannot be consumed");
        carrier.TryConsume();
        var pickup = runtime.GetComponentInChildren<BlockPickupZone>();
        Warp(carrier.player, pickup.transform.position + Vector3.up);
        BlockType queued;
        Check(runtime.TryGetSupplyType(0, out queued), "pending supply exists");
        Check(pickup.TryPickup() && carrier.FirstType == queued, "supply chooses queued texture");
        Check(
            carrier.carryAnchor.GetComponentInChildren<MeshFilter>().sharedMesh.uv2.Length > 0,
            "hand mesh carries atlas attributes"
        );
        Warp(carrier.player, runtime.CurrentSite.GetComponentInChildren<BuildTarget>().transform.position + Vector3.up);
        return "Prepared typed pickup and automatic delivery; wait for a frame then CompletePlayCheck.";
    }

    public static string CompletePlayCheck()
    {
        var runtime = UnityEngine.Object.FindFirstObjectByType<BlockLevelRuntime>();
        var carrier = runtime.carrier;
        Check(carrier.Count == 0
            && runtime.CurrentSite.BuiltCount == 1, "automatic distance delivery consumed exactly one typed cube");
        int transferred = 1;
        var order = new List<string>();
        string previous = runtime.CurrentSite.name;
        while (runtime.CurrentSite != null)
        {
            var site = runtime.CurrentSite;
            if (site.name != previous)
            {
                order.Add(previous);
                previous = site.name;
            }

            if (carrier.Count == 0)
                Check(carrier.TryPickup(), "typed supply queue");
            BlockType expected;
            Check(site.TryGetPendingType(0, out expected) && expected == carrier.FirstType, "FIFO cargo matches next block");
            Check(site.TryBuild(carrier), "place typed cube");
            transferred++;
            Check(transferred <= 1000, "queue terminates");
        }

        order.Add(previous);
        Check(transferred == 784 && order.Count == 4, "house and all 3 trees complete");
        Check(!carrier.TryPickup() && !carrier.HasSupplyForNextSlot, "no surplus pickup after level completion");
        var sites = runtime.GetComponentsInChildren<BuildSite>();
        int chunks = 0;
        foreach (var site in sites)
        {
            Check(site.IsComplete && !site.ghost.gameObject.activeSelf, "completed ghost hidden");
            chunks += site.builder.ChunkCount;
        }

        Check(chunks < 50, "chunked rendering, no object per cell");
        var controller = carrier.player.GetComponent("PlayerController") as Behaviour;
        bool oldController = controller != null && controller.enabled;
        if (controller != null)
            controller.enabled = false;
        var cc = carrier.player.GetComponent<CharacterController>();
        Check(cc != null, "existing CharacterController");
        try
        {
            Warp(carrier.player, new Vector3(0, 1.1f, 10));
            Physics.SyncTransforms();
            for (int i = 0; i < 70; i++)
                cc.Move(new Vector3(0, -0.06f, 0.2f));
            Check(carrier.player.position.z > 22, "player passes through empty doorway into room");
        }
        finally
        {
            if (controller != null)
                controller.enabled = oldController;
        }

        string result = "BLOCK_VILLAGE_PLAY_PASS: typed automatic delivery; FIFO texture matching; 784 blocks; house + 3 trees; "
            + chunks
            + " chunks; walkable doorway; editor preview hidden.";
        File.WriteAllText("Temp/BlockVillagePlayChecks.txt", result);
        Debug.Log(result);
        return result;
    }

    private static void Warp(Transform player, Vector3 position)
    {
        var cc = player.GetComponent<CharacterController>();
        bool enabled = cc != null && cc.enabled;
        if (cc != null)
            cc.enabled = false;
        player.position = position;
        if (cc != null)
            cc.enabled = enabled;
    }
}
