using System;
using UnityEngine;

// Completion records are separate from resumable construction checkpoints.
public static class LevelRecordStore
{
    private const string Prefix = "BlockBuilding.Levels.v1.";
    public static bool Completed(string id)
    {
        return PlayerPrefs.GetInt(Prefix + id + ".done", 0) == 1;
    }

    public static bool TryGetBest(string id, out float seconds)
    {
        string key = Prefix + id + ".best";
        seconds = PlayerPrefs.GetFloat(key, -1);
        return PlayerPrefs.HasKey(key) && seconds >= 0 && !float.IsNaN(seconds) && !float.IsInfinity(seconds);
    }

    public static bool Record(string id, float seconds)
    {
        if (string.IsNullOrEmpty(id) || seconds < 0 || float.IsNaN(seconds) || float.IsInfinity(seconds))
            throw new ArgumentException("Invalid completion.");
        float best;
        bool faster = !TryGetBest(id, out best) || seconds < best;
        if (faster)
            PlayerPrefs.SetFloat(Prefix + id + ".best", seconds);
        PlayerPrefs.SetInt(Prefix + id + ".done", 1);
        PlayerPrefs.Save();
        return faster;
    }

    public static string Format(float seconds)
    {
        int hundredths = Mathf.FloorToInt(Mathf.Max(0, seconds) * 100);
        return (hundredths / 6000).ToString("00")
            + ":"
            + ((hundredths / 100) % 60).ToString("00")
            + "." + (hundredths % 100).ToString("00");
    }
}
