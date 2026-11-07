using UnityEngine;

// A structure's bottom authored layer is present before the timed construction.
public static class BuildFoundation
{
    public static int Count(BlockStructure site)
    {
        int bottom = int.MaxValue, count = 0;
        foreach (var cell in site.blocks)
            bottom = Mathf.Min(bottom, cell.position.y);
        foreach (var cell in site.blocks)
            if (cell.position.y == bottom)
                count++;
        return count;
    }

    public static int RawCount(BuildSiteData data)
    {
        int count = 0;
        if (data != null)
            foreach (var site in data.level.sites)
                count += site.blocks.Count;
        return count;
    }

    public static int WorkCount(BuildSiteData data)
    {
        int count = 0;
        if (data != null)
            foreach (var site in data.level.sites)
                count += site.blocks.Count - Count(site);
        return count;
    }

    // Saved cursors retain the original raw ordering, including old pre-foundation saves.
    public static int WorkInPrefix(BuildSiteData data, int rawPrefix)
    {
        int result = 0;
        if (data != null)
            foreach (var site in data.level.sites)
            {
                int take = Mathf.Clamp(rawPrefix, 0, site.blocks.Count);
                result += Mathf.Max(0, take - Count(site));
                rawPrefix -= take;
            }

        return result;
    }
}
