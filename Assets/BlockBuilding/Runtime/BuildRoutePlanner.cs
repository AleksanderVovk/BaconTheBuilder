using System.Collections.Generic;
using UnityEngine;

// Explicit planning step used by the editor. No route search or allocation in Update.
public static class BuildRoutePlanner
{
    private static float Distance(Vector3Int a, Vector3Int b)
    {
        float x = a.x - b.x, z = a.z - b.z;
        return Mathf.Sqrt(x * x + z * z);
    }

    private static float Cost(List<BlockCell> cells, Vector3Int start)
    {
        float cost = 0;
        foreach (var cell in cells)
        {
            cost += Distance(start, cell.position);
            start = cell.position;
        }

        return cost;
    }

    public static float HorizontalLength(BlockStructure site, float size)
    {
        var cells = new List<BlockCell>(site.blocks);
        cells.Sort(BlockLevelDefinition.Compare);
        float total = 0;
        for (int i = 1; i < cells.Count; i++)
            total += Distance(cells[i - 1].position, cells[i].position) * size;
        return total;
    }

    public static void Optimize(BlockLevelDefinition level)
    {
        Vector3 previous = level.pickupPoint;
        foreach (var site in level.sites)
        {
            var original = new List<BlockCell>(site.blocks);
            original.Sort(BlockLevelDefinition.Compare);
            if (original.Count == 0)
                continue;
            Vector3 local = Quaternion.Euler(0, -site.yaw, 0) * (previous - site.origin) / level.blockSize;
            var start = new Vector3Int(Mathf.RoundToInt(local.x), 0, Mathf.RoundToInt(local.z));
            var cursor = start;
            var result = new List<BlockCell>(original.Count);
            for (int at = 0; at < original.Count;)
            {
                int end = at + 1;
                while (end < original.Count && original[end].position.y == original[at].position.y)
                    end++;
                var layer = original.GetRange(at, end - at);
                var best = new List<BlockCell>(layer);
                float bestCost = Cost(best, cursor);
                for (int mode = 0; mode < 8; mode++)
                {
                    List<BlockCell> candidate;
                    if (mode < 4)
                    {
                        bool alongX = mode < 2;
                        candidate = new List<BlockCell>(layer);
                        candidate.Sort((a, b) =>
                        {
                            int rowA = alongX ? a.position.z : a.position.x, rowB = alongX ? b.position.z : b.position.x;
                            int c = rowA.CompareTo(rowB);
                            if (c != 0)
                                return c;
                            int ax = alongX ? a.position.x : a.position.z, bx = alongX ? b.position.x : b.position.z;
                            return (rowA & 1) == 0 ? ax.CompareTo(bx) : bx.CompareTo(ax);
                        });
                        if ((mode & 1) != 0)
                            candidate.Reverse();
                    }
                    else
                        candidate = Nearest(layer, cursor, mode - 4);
                    float cost = Cost(candidate, cursor);
                    if (cost < bestCost)
                    {
                        best = candidate;
                        bestCost = cost;
                    }
                }

                // Uncross a route, including sparse rings and disconnected roof strips.
                for (int pass = 0; pass < 2; pass++)
                {
                    bool changed = false;
                    for (int i = 0; i < best.Count - 1; i++)
                        for (int j = i + 1; j < best.Count; j++)
                        {
                            Vector3Int before = i == 0 ? cursor : best[i - 1].position;
                            float old = Distance(before, best[i].position), next = Distance(before, best[j].position);
                            if (j + 1 < best.Count)
                            {
                                old += Distance(best[j].position, best[j + 1].position);
                                next += Distance(best[i].position, best[j + 1].position);
                            }

                            if (next + 0.001f < old)
                            {
                                best.Reverse(i, j - i + 1);
                                changed = true;
                            }
                        }

                    if (!changed)
                        break;
                }

                result.AddRange(best);
                cursor = best[best.Count - 1].position;
                at = end;
            }

            // Retain authored order if an unusual design cannot benefit.
            if (Cost(result, start) > Cost(original, start))
                result = original;
            for (int i = 0; i < result.Count; i++)
                result[i].order = i;
            site.blocks = result;
            previous = site.origin + Quaternion.Euler(0, site.yaw, 0) * ((Vector3)result[result.Count - 1].position + Vector3.one * 0.5f) * level.blockSize;
        }
    }

    private static List<BlockCell> Nearest(List<BlockCell> layer, Vector3Int start, int mode)
    {
        var left = new List<BlockCell>(layer);
        var result = new List<BlockCell>(layer.Count);
        var cursor = start;
        while (left.Count > 0)
        {
            int best = 0;
            float score = float.PositiveInfinity;
            for (int i = 0; i < left.Count; i++)
            {
                var p = left[i].position;
                float d = Distance(cursor, p);
                // Deterministic directional ties give multiple useful ring/wall walks.
                float tie = mode == 0 ? p.x : mode == 1 ? -p.x : mode == 2 ? p.z : -p.z;
                float value = d + tie * 0.00001f;
                if (value < score)
                {
                    score = value;
                    best = i;
                }
            }

            var cell = left[best];
            result.Add(cell);
            cursor = cell.position;
            left.RemoveAt(best);
        }

        return result;
    }
}
