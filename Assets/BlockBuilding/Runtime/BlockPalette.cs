using UnityEngine;

// Numeric IDs are stable: they are stored in JSON. Do not reorder this enum.
public enum BlockType
{
    Cobblestone = 0,
    OakPlanks = 1,
    OakLog = 2,
    RoofBricks = 3,
    Leaves = 4,
    Glass = 5,
    Dirt = 6,
    Grass = 7,
    Sandstone = 8,
    Slate = 9,
    Thatch = 10,
    BirchPlanks = 11,
    BirchLog = 12,
    StoneBricks = 13,
    Ocean = 14,
    Land = 15,
    Snow = 16,
    Lava = 17,
    FlagRed = 18,
    FlagGold = 19,
    Basalt = 20,
    Sand = 21,
    Ice = 22,
    CooledLava = 23,
    ShallowWater = 24
}

public static class BlockPalette
{
    public const int Count = 25;
    public const int TileSize = 32;
    public const int Padding = 2;
    public const int Stride = TileSize + 2 * Padding;
    public const int Columns = 8;
    public const int AtlasSize = Stride * Columns;
    public static int Tile(BlockType type, int face)
    {
        switch (type)
        {
            case BlockType.OakPlanks:
                return 1;
            case BlockType.OakLog:
                return face == 2 || face == 3 ? 3 : 2;
            case BlockType.RoofBricks:
                return 4;
            case BlockType.Leaves:
                return 5;
            case BlockType.Glass:
                return 6;
            case BlockType.Dirt:
                return 7;
            case BlockType.Grass:
                return face == 2 ? 9 : face == 3 ? 7 : 8;
            case BlockType.Sandstone:
                return 22;
            case BlockType.Slate:
                return 11;
            case BlockType.Thatch:
                return 12;
            case BlockType.BirchPlanks:
                return 13;
            case BlockType.BirchLog:
                return face == 2 || face == 3 ? 3 : 14;
            case BlockType.StoneBricks:
                return 15;
            case BlockType.Ocean:
                return 16;
            case BlockType.Land:
                return 17;
            case BlockType.Snow:
                return 18;
            case BlockType.Lava:
                return 19;
            case BlockType.FlagRed:
                return 20;
            case BlockType.FlagGold:
                return 21;
            case BlockType.Basalt:
                return 23;
            case BlockType.Sand:
                return 28;
            case BlockType.Ice:
                return 26;
            case BlockType.CooledLava:
                return 27;
            case BlockType.ShallowWater:
                return 24;
            default:
                return 0;
        }
    }

    public static Color32 Tint(BlockType type)
    {
        return new Color32(255, 255, 255, type == BlockType.Glass ? (byte)0 : (byte)255);
    }

    public static Rect TileUV(int tile)
    {
        int x = tile % Columns, y = Columns - 1 - tile / Columns;
        return new Rect(
            (x * Stride + Padding + 0.5f) / AtlasSize,
            (y * Stride + Padding + 0.5f) / AtlasSize,
            (TileSize - 1f) / AtlasSize,
            (TileSize - 1f) / AtlasSize
        );
    }

    public static bool HidesFace(BlockType current, BlockType neighbour)
    {
        // Stone next to glass must keep its visible face. Adjacent glass panes merge.
        return neighbour != BlockType.Glass || current == BlockType.Glass;
    }
}
