using System.IO;
using UnityEditor;
using UnityEngine;

public static class VoxelAtlasPacker
{
    private const string Folder = "Assets/BlockBuilding/Textures/";
    [MenuItem("Tools/Block Building/Repack Extended Atlas")]
    public static Texture2D Rebuild()
    {
        var old = Readable(Folder + "VoxelAtlasSource.png");
        var nature = Readable(Folder + "VoxelNatureSource.png");
        int size = BlockPalette.AtlasSize;
        var atlas = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int tile = 0; tile < BlockPalette.Columns * BlockPalette.Columns; tile++)
        {
            var source = tile >= 16 && tile < 32 ? nature : old;
            int sourceTile = tile < 16 ? tile : tile < 32 ? tile - 16 : 0;
            int sx = sourceTile % 4, sy = 3 - sourceTile / 4;
            int dx = tile % BlockPalette.Columns, dy = BlockPalette.Columns - 1 - tile / BlockPalette.Columns;
            for (int y = -BlockPalette.Padding; y < BlockPalette.TileSize + BlockPalette.Padding; y++)
                for (int x = -BlockPalette.Padding; x < BlockPalette.TileSize + BlockPalette.Padding; x++)
                {
                    int px = Mathf.Clamp(x, 0, BlockPalette.TileSize - 1), py = Mathf.Clamp(y, 0, BlockPalette.TileSize - 1);
                    int u = Mathf.Min(
                        source.width - 1,
                        Mathf.FloorToInt((sx + (px + 0.5f) / BlockPalette.TileSize) * source.width / 4f)
                    );
                    int v = Mathf.Min(
                        source.height - 1,
                        Mathf.FloorToInt((sy + (py + 0.5f) / BlockPalette.TileSize) * source.height / 4f)
                    );
                    atlas.SetPixel(
                        dx * BlockPalette.Stride + BlockPalette.Padding + x,
                        dy * BlockPalette.Stride + BlockPalette.Padding + y,
                        source.GetPixel(u, v)
                    );
                }
        }

        atlas.Apply();
        string path = Folder + "VoxelAtlas.png";
        File.WriteAllBytes(path, atlas.EncodeToPNG());
        Object.DestroyImmediate(atlas);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 512;
        importer.isReadable = false;
        importer.sRGBTexture = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Texture2D Readable(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.isReadable = true;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
