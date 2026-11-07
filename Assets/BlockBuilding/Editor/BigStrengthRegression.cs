using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using BigInteger = System.Numerics.BigInteger;

public static class BigStrengthRegression
{
    private static void Check(bool ok, string message)
    {
        if (!ok)
            throw new InvalidOperationException(message);
    }

    public static string Run()
    {
        if (!EditorApplication.isPlaying)
            throw new InvalidOperationException("Run in Play Mode.");
        string key = "BlockBuilding.QA.BigStrength." + Guid.NewGuid().ToString("N");
        var root = new GameObject("BigStrengthRegression");
        root.SetActive(false);
        root.transform.position = new Vector3(10000, 10000, 10000);
        BuildSiteData data = null;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            PlayerPrefs.SetInt(key, 196995971);
            var strength = root.AddComponent<StrengthUpgradeSystem>();
            strength.saveKey = key;
            strength.Initialize();
            Check(strength.Strength == 196995971, "Legacy integer save migration failed.");
            foreach (int n in new[]
            {
                0,
                1,
                2,
                100,
                9999,
                100000
            }

            )
            {
                BigInteger threshold = 170 + (BigInteger)50 * n + (BigInteger)25 * n * (n + 1) / 2;
                strength.SetStrengthForTesting(threshold);
                Check(strength.Level == 8 + n, "Wrong level exactly at extended threshold " + n);
                Check(strength.Progress == 0 && strength.NextThreshold > threshold, "Invalid threshold progress.");
                if (n > 0)
                {
                    strength.SetStrengthForTesting(threshold - 1);
                    Check(strength.Level == 7 + n, "Wrong level immediately below threshold.");
                }
            }

            strength.RestoreStrengthAfterTesting();
            BigInteger huge = BigInteger.Pow(10, 10000);
            strength.ApplySavedStrength(huge);
            strength.AddStrength(1);
            Check(strength.Strength == huge + 1, "Huge strength lost the +1 increment.");
            Check(strength.Capacity == 9999
                && strength.NextThreshold > strength.Strength, "Huge strength capacity/level overflow.");
            Check(!float.IsNaN(strength.Progress) && !float.IsInfinity(strength.Progress), "Progress overflow.");
            var gift = ActivityChestReward.CalculateReward(0);
            Check(gift == 50, "Initial chest reward incorrect.");
            Check(StrengthNumber.Format(strength.Strength).Length < 24, "Huge number not shortened for UI.");
            Check(StrengthNumber.TryParse(PlayerPrefs.GetString(strength.ExactSaveKey), out var exact)
                && exact == huge + 1, "Exact save failed.");
            var secondGO = new GameObject("ReloadedStrength");
            secondGO.transform.SetParent(root.transform, false);
            secondGO.SetActive(false);
            var reloaded = secondGO.AddComponent<StrengthUpgradeSystem>();
            reloaded.saveKey = key;
            reloaded.Initialize();
            Check(reloaded.Strength == huge + 1, "Exact local reload failed.");
            var cloud = new YG.SavesYG();
            strength.WritePlatformSave(cloud);
            var restored = JsonUtility.FromJson<YG.SavesYG>(JsonUtility.ToJson(cloud));
            Check(StrengthNumber.TryParse(restored.gymStrengthBig, out var cloudValue)
                && cloudValue == huge + 1, "Cloud JSON truncated strength.");
            strength.ApplySavedStrength(5);
            Check(strength.Strength == huge + 1, "Stale cloud save reduced local strength.");
            strength.AddStrength(gift);
            Check(strength.Strength == huge + 1 + gift, "Chest addition overflow.");
            var live = UnityEngine.Object.FindFirstObjectByType<BlockLevelRuntime>();
            data = ScriptableObject.CreateInstance<BuildSiteData>();
            data.useAtlas = true;
            data.blockMaterial = live.data.blockMaterial;
            var structure = new BlockStructure
            {
                id = "CapacityRegression",
                origin = root.transform.position
            };
            for (int i = 0; i < 24; i++)
                structure.blocks.Add(new BlockCell { position = new Vector3Int(i, 0, 0), order = i, type = (BlockType)(i % BlockPalette.Count) });
            data.level.sites.Add(structure);
            var carrier = root.AddComponent<PlayerBlockCarrier>();
            carrier.player = root.transform;
            carrier.strength = strength;
            carrier.material = data.blockMaterial;
            carrier.useAtlas = true;
            var runtime = root.AddComponent<BlockLevelRuntime>();
            runtime.autoLoadOnStart = false;
            runtime.createColliders = false;
            runtime.carrier = carrier;
            runtime.cubeMesh = live.cubeMesh;
            runtime.ghostMaterial = live.ghostMaterial;
            runtime.LoadLevel(data);
            foreach (var target in root.GetComponentsInChildren<BuildTarget>(true))
                target.enabled = false;
            root.SetActive(true);
            Check(carrier.FillToCapacity() == 9999 && carrier.Count == 9999, "Pickup limited by remaining 24 cells.");
            Check(carrier.VisibleCount == 18
                && carrier.VisualVertexCount == 432, "Visual stack scales with logical inventory.");
            for (int i = 0; i < 10; i++)
                Check(runtime.CurrentSite.TryBuild(carrier), "Ordinary typed construction failed.");
            Check(carrier.FillToCapacity() == 10 && carrier.Count == 9999, "Refill with shifted ring buffer failed.");
            carrier.SetInfiniteBlocks(true);
            for (int i = 0; i < 7; i++)
                Check(runtime.CurrentSite.TryBuild(carrier), "Infinite construction failed.");
            carrier.SetInfiniteBlocks(false);
            Check(carrier.Count == 9999, "Infinite bonus consumed ordinary blocks.");
            for (int i = 0; i < 7; i++)
                Check(runtime.CurrentSite.TryBuild(carrier), "Post-bonus ordinary construction stuck.");
            Check(runtime.IsComplete && carrier.Count == 9992, "Surplus count corrupted after completing a short level.");
            return "PASS: legacy migration; exact thresholds; 10^10000 + 1; bounded UI/progress; exact local and cloud JSON; fixed chest reward; 9999 refill with only 24 cells left; 18 visible cubes; post-infinite construction; elapsed "
                + timer.ElapsedMilliseconds
                + " ms.";
        }
        finally
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.DeleteKey(key + ".Big.v1");
            PlayerPrefs.Save();
            UnityEngine.Object.DestroyImmediate(root);
            if (data != null)
                UnityEngine.Object.DestroyImmediate(data);
        }
    }
}
