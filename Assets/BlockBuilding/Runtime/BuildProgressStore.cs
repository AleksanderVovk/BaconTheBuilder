using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using YG;
using BigInteger = System.Numerics.BigInteger;

// One compact checkpoint per level. Finished entries are tombstones against stale cloud saves.
public static class BuildProgressStore
{
    [Serializable]
    public class Snapshot
    {
        public string levelId = "", signature = "", revision = "0";
        public int builtBlocks;
        public double elapsedSeconds;
        public bool assisted, finished;
    }

    [Serializable]
    private class Envelope
    {
        public List<Snapshot> levels = new List<Snapshot>();
    }

    public const string Prefix = "BlockBuilding.Progress.v1.";
    private static readonly Dictionary<string, Snapshot> cache = new Dictionary<string, Snapshot>();
    private static bool dirty, cloudDirty;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetMemory()
    {
        cache.Clear();
        dirty = false;
        cloudDirty = false;
    }

    public static void ClearMemory()
    {
        ResetMemory();
    }

    private static BigInteger Revision(Snapshot s)
    {
        BigInteger value;
        return s != null && StrengthNumber.TryParse(s.revision, out value) ? value : BigInteger.Zero;
    }

    private static Snapshot Read(string json)
    {
        Snapshot s = null;
        try
        {
            if (!string.IsNullOrEmpty(json))
                s = JsonUtility.FromJson<Snapshot>(json);
        }
        catch (ArgumentException)
        {
        }

        if (s == null
            || string.IsNullOrEmpty(s.levelId)
            || s.builtBlocks < 0
            || s.builtBlocks > 100000
            || double.IsNaN(s.elapsedSeconds)
            || double.IsInfinity(s.elapsedSeconds)
            || s.elapsedSeconds < 0)
            return null;
        if (s.finished)
        {
            s.builtBlocks = 0;
            s.elapsedSeconds = 0;
        }

        return s;
    }

    private static Snapshot Load(string id)
    {
        if (!cache.TryGetValue(id, out var s))
        {
            s = Read(PlayerPrefs.GetString(Prefix + id, ""));
            if (s != null && s.levelId != id)
                s = null;
            cache[id] = s;
        }

        return s;
    }

    public static bool TryPeek(string id, out Snapshot snapshot)
    {
        var s = Load(id);
        snapshot = null;
        if (s == null || s.finished || s.builtBlocks == 0)
            return false;
        snapshot = new Snapshot
        {
            levelId = s.levelId,
            signature = s.signature,
            revision = s.revision,
            builtBlocks = s.builtBlocks,
            elapsedSeconds = s.elapsedSeconds,
            assisted = s.assisted
        };
        return true;
    }

    public static bool TryGet(string id, string signature, int total, out Snapshot snapshot)
    {
        return TryPeek(id, out snapshot) && snapshot.signature == signature && snapshot.builtBlocks < total;
    }

    public static string Signature(BuildSiteData data)
    {
        // Includes cell order/types: a changed level cannot load incompatible geometry.
        using (var sha = SHA256.Create())
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(JsonUtility.ToJson(data.level))));
    }

    public static void Save(string id, string signature, int built, double seconds, bool assisted)
    {
        if (string.IsNullOrEmpty(id) || built <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds))
            return;
        Put(new Snapshot { levelId = id, signature = signature, builtBlocks = built, elapsedSeconds = Math.Max(0, seconds), assisted = assisted, revision = NextRevision(id) });
    }

    public static void Complete(string id, string signature)
    {
        Put(new Snapshot { levelId = id, signature = signature, finished = true, revision = NextRevision(id) });
    }

    private static string NextRevision(string id)
    {
        return StrengthNumber.Serialize(BigInteger.Max(DateTime.UtcNow.Ticks, Revision(Load(id)) + 1));
    }

    private static void Put(Snapshot s)
    {
        cache[s.levelId] = s;
        PlayerPrefs.SetString(Prefix + s.levelId, JsonUtility.ToJson(s));
        dirty = true;
        cloudDirty = true;
    }

    public static void Flush(bool cloud)
    {
        if (dirty)
        {
            PlayerPrefs.Save();
            dirty = false;
        }

        if (!cloud || !cloudDirty || !YG2.isSDKEnabled || YG2.saves == null)
            return;
        var envelope = new Envelope();
        foreach (var pair in cache)
            if (pair.Value != null)
                envelope.levels.Add(pair.Value);
        YG2.saves.buildingProgressJson = JsonUtility.ToJson(envelope);
        YG2.SaveProgress();
        cloudDirty = false;
    }

    public static void SyncCloud(BuildLevelCatalog catalog)
    {
        if (catalog == null || YG2.saves == null)
            return;
        Envelope remote = null;
        try
        {
            if (!string.IsNullOrEmpty(YG2.saves.buildingProgressJson))
                remote = JsonUtility.FromJson<Envelope>(YG2.saves.buildingProgressJson);
        }
        catch (ArgumentException)
        {
        }

        foreach (var entry in catalog.levels)
        {
            var local = Load(entry.id);
            Snapshot other = null;
            if (remote != null && remote.levels != null)
                foreach (var candidate in remote.levels)
                    if (candidate != null && candidate.levelId == entry.id)
                    {
                        other = Read(JsonUtility.ToJson(candidate));
                        break;
                    }

            if (other != null && (local == null || Revision(other) > Revision(local)))
                Put(other);
            else if (local != null && (other == null || Revision(local) > Revision(other)))
                cloudDirty = true;
        }

        Flush(true);
    }
}
