using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class StockAudioVerification
{
    public static string Run()
    {
        var session = UnityEngine.Object.FindFirstObjectByType<BuildLevelSession>();
        session.Select(0);
        var strength = session.runtime.carrier.strength;
        var report = new StringBuilder();
        int levelEvents = 0;
        Action level = () => levelEvents++;
        strength.LeveledUp += level;
        strength.SetStrengthForTesting(169);
        try
        {
            strength.AddStrength(1);
            Check(strength.Level == 8
                && strength.Capacity == 8
                && strength.NextThreshold == 245
                && strength.Progress == 0, "Level 8 starts a new XP interval", report);
            Check(strength.levelAudio.isPlaying
                && strength.levelUpClip.name == "impactBell_heavy_000", "Downloaded level-up clip playing", report);
            strength.SetStrengthForTesting(244);
            strength.AddStrength(1);
            Check(strength.Level == 9
                && strength.Capacity == 9
                && strength.NextThreshold == 345
                && strength.Progress == 0, "Level 9 reached after 8; capacity increases", report);
            strength.SetStrengthForTesting(344);
            strength.AddStrength(1);
            Check(strength.Level == 10
                && strength.Capacity == 10
                && strength.NextThreshold == 470, "Level 10 and increasing cost", report);
            Check(levelEvents == 3, "Level-up event works beyond authored table", report);
            foreach (int value in new[]
            {
                0,
                5,
                15,
                30,
                50,
                80,
                120,
                169,
                170,
                244,
                245,
                344,
                345,
                1000,
                1000000,
                int.MaxValue - 1
            }

            )
            {
                strength.SetStrengthForTesting(value);
                Check(strength.NextThreshold > value && strength.Capacity <= PlayerBlockCarrier.AdminMaximum, null, report);
            }

            strength.SetStrengthForTesting(int.MaxValue);
            Check(strength.NextThreshold > int.MaxValue
                && strength.Progress >= 0
                && strength.Progress < 1, "Strength continues beyond the legacy integer ceiling", report);
            strength.SetStrengthForTesting(245);
            var carrier = session.runtime.carrier;
            carrier.Clear();
            carrier.FillToCapacity();
            Check(carrier.Count == 9, "Carrier uses extended capacity", report);
            var audio = session.runtime.GetComponent<BlockPlacementAudio>();
            int played = audio.PlayedCount;
            Check(session.runtime.CurrentSite.TryBuild(carrier)
                && audio.PlayedCount > played, "Placement starts stock sound", report);
            foreach (var bank in new[]
            {
                audio.stone,
                audio.wood,
                audio.glass,
                audio.soft
            }

            )
                foreach (var clip in bank)
                    Check(clip != null
                        && AssetDatabase.GetAssetPath(clip).StartsWith("Assets/BlockBuilding/Audio/Natural/"), null, report);
            var stock = session.runtime.GetComponent<StockGameAudio>();
            foreach (var clip in stock.clips)
                Check(clip != null && clip.length > 0 && clip.channels == 1, null, report);
            Check(stock.clips.Length == 7, "UI, pickup, training, repetition, completion and return clips assigned", report);
            foreach (var b in stock.buttons)
                if (b.name == "Menu")
                {
                    b.onClick.Invoke();
                    break;
                }

            Check(stock.uiVoice.isPlaying, "Button click plays stock clip", report);
            report.AppendLine("PASS: all audio banks use downloaded clips; short clips import as mono.");
        }
        finally
        {
            strength.LeveledUp -= level;
            strength.RestoreStrengthAfterTesting();
            session.Select(0);
            session.OpenMenu();
        }

        File.WriteAllText("BlockBuildingBackup/StockAudioVerification.txt", report.ToString());
        return report.ToString();
    }

    private static void Check(bool value, string message, StringBuilder report)
    {
        if (!value)
            throw new Exception(message ?? "Boundary/audio reference check failed");
        if (message != null)
            report.AppendLine("PASS: " + message);
    }
}
