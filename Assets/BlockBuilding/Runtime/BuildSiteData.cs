using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BlockCell
{
    [Header("Grid"), Tooltip("Integer cell coordinates; y is the layer.")]
    public Vector3Int position;

    [Header("Sequence"), Tooltip("Order within this layer. Ties use z then x.")]
    public int order;

    [Header("Texture"), Tooltip("Stable block texture ID. Selected in the level editor.")]
    public BlockType type = BlockType.Cobblestone;
}

[Serializable]
public class BlockStructure
{
    [Header("Identity"), Tooltip("Unique readable structure identifier.")]
    public string id = "Building";

    [Header("Placement"), Tooltip("World position of the bottom corner of cell 0,0,0.")]
    public Vector3 origin;

    [Header("Rotation"), Tooltip("Rotation around the world up axis, in degrees.")]
    public float yaw;

    [Header("Delivery"), Tooltip("World position where the player delivers blocks from ground level.")]
    public Vector3 deliveryPoint = new Vector3(0, 0, -2);

    [Header("Blocks"), Tooltip("Cells. All lower layers finish before higher layers; overhangs are allowed.")]
    public List<BlockCell> blocks = new List<BlockCell>();
}

[Serializable]
public class BlockLevelDefinition
{
    [Header("Format"), Tooltip("JSON schema version. 1 = legacy; 2 = typed atlas cells.")]
    public int version = 2;

    [Header("Grid"), Tooltip("World size of one solid cubic cell.")]
    public float blockSize = 1;

    [Header("Pickup"), Tooltip("World position of the supply pile, at ground level.")]
    public Vector3 pickupPoint = new Vector3(-4, 0, 0);

    [Header("Training"), Tooltip("World position of the bench, at ground level.")]
    public Vector3 trainingPoint = new Vector3(4, 0, 0);

    [Header("Structures"), Tooltip("Structures are completed in this list order.")]
    public List<BlockStructure> sites = new List<BlockStructure>();
    public void Validate()
    {
        if (version != 1 && version != 2)
            throw new ArgumentException("Unsupported JSON version.");
        if (!Finite(blockSize) || blockSize < 0.1f || blockSize > 10)
            throw new ArgumentException("blockSize must be 0.1..10.");
        if (!Finite(pickupPoint) || !Finite(trainingPoint))
            throw new ArgumentException("Invalid zone position.");
        if (sites == null || sites.Count == 0 || sites.Count > 128)
            throw new ArgumentException("Use 1..128 structures.");
        var ids = new HashSet<string>();
        int total = 0;
        foreach (var site in sites)
        {
            if (site == null || string.IsNullOrWhiteSpace(site.id) || !ids.Add(site.id))
                throw new ArgumentException("Structure IDs must be unique and nonempty.");
            if (!Finite(site.origin) || !Finite(site.deliveryPoint) || !Finite(site.yaw))
                throw new ArgumentException("Invalid placement.");
            if (site.blocks == null)
                throw new ArgumentException("Missing blocks array.");
            total += site.blocks.Count;
            if (total > 100000)
                throw new ArgumentException("Level limit: 100000 cells.");
            var occupied = new HashSet<Vector3Int>();
            foreach (var block in site.blocks)
            {
                if (block == null
                    || block.position.y < 0
                    || block.position.y > 2048
                    || block.position.x < -2048
                    || block.position.x > 2048
                    || block.position.z < -2048
                    || block.position.z > 2048)
                    throw new ArgumentException("Coordinates must be within +/-2048; y cannot be negative.");
                if ((int)block.type < 0 || (int)block.type >= BlockPalette.Count)
                    throw new ArgumentException("Unknown block texture ID.");
                if (!occupied.Add(block.position))
                    throw new ArgumentException("Duplicate cell in " + site.id + ": " + block.position);
            }
        }
    }

    private static bool Finite(float n)
    {
        return !float.IsNaN(n) && !float.IsInfinity(n);
    }

    private static bool Finite(Vector3 p)
    {
        return Finite(p.x) && Finite(p.y) && Finite(p.z);
    }

    public static int Compare(BlockCell a, BlockCell b)
    {
        int c = a.position.y.CompareTo(b.position.y);
        if (c == 0)
            c = a.order.CompareTo(b.order);
        if (c == 0)
            c = a.position.z.CompareTo(b.position.z);
        return c == 0 ? a.position.x.CompareTo(b.position.x) : c;
    }

    public static BlockLevelDefinition Pyramid()
    {
        var level = new BlockLevelDefinition();
        var site = new BlockStructure
        {
            id = "Pyramid",
            origin = new Vector3(-2.5f, 0, 5),
            deliveryPoint = new Vector3(0, 0, 3)
        };
        for (int y = 0; y < 3; y++)
            for (int z = y; z < 5 - y; z++)
                for (int x = y; x < 5 - y; x++)
                    site.blocks.Add(new BlockCell { position = new Vector3Int(x, y, z), order = site.blocks.Count });
        level.sites.Add(site);
        return level;
    }
}

[CreateAssetMenu(menuName = "Block Building/Level", fileName = "BlockLevel")]
public class BuildSiteData : ScriptableObject
{
    [Header("Level"), Tooltip("Editable level definition. JSON contains the same fields.")]
    public BlockLevelDefinition level = new BlockLevelDefinition();

    [Header("Visuals"), Tooltip("Optional prefab used ONLY for the carried visual mesh/material. Buildings always use cubic cells.")]
    public GameObject blockPrefab;

    [Header("Material"), Tooltip("One shared material for every building and supply block. Overrides prefab material.")]
    public Material blockMaterial;

    [Header("Atlas"), Tooltip("Use typed atlas cubes in the hand, ghost, pile and chunk meshes.")]
    public bool useAtlas;
    public string ToJson()
    {
        level.Validate();
        return JsonUtility.ToJson(level, true);
    }

    public void LoadJson(string json)
    {
        var candidate = JsonUtility.FromJson<BlockLevelDefinition>(json);
        if (candidate == null)
            throw new ArgumentException("Empty JSON.");
        candidate.Validate();
        level = candidate;
    }
}
