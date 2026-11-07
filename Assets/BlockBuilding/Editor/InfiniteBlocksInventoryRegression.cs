using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;

// Isolated regression fixture: never modifies the live carrier, records or saves.
public static class InfiniteBlocksInventoryRegression
{
    private static void Require(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
    }

    public static string Run()
    {
        Require(EditorApplication.isPlaying, "Run this regression in Play Mode.");
        Case(1, 0, 4, false, false);
        Case(4, 1, 15, false, false);
        Case(4, 1, 15, true, false);
        Case(0, 0, 7, false, false);
        Case(4, 1, 0, false, false);
        Case(4, 1, 22, false, false);
        Case(4, 1, 7, false, true);
        return "PASS: ordinary pickup, shifted queue head, mixed materials, next structure, timer expiry, zone disable, empty inventory, no construction, fewer cells than blocks, repeated bonus. All counts conserved.";
    }

    private static void Case(int stored, int normalBefore, int infiniteSteps, bool disable, bool repeat)
    {
        var live = UnityEngine.Object.FindFirstObjectByType<BlockLevelRuntime>();
        var root = new GameObject("InfiniteInventoryRegression");
        root.SetActive(false);
        root.transform.position = new Vector3(10000, 10000, 10000);
        var data = ScriptableObject.CreateInstance<BuildSiteData>();
        try
        {
            data.useAtlas = true;
            data.blockMaterial = live.data.blockMaterial;
            for (int siteIndex = 0; siteIndex < 2; siteIndex++)
            {
                var structure = new BlockStructure
                {
                    id = "Regression" + siteIndex,
                    origin = root.transform.position + Vector3.right * siteIndex * 20
                };
                for (int i = 0; i < 12; i++)
                    structure.blocks.Add(new BlockCell { position = new Vector3Int(i, 0, 0), order = i, type = (BlockType)((siteIndex * 12 + i) % BlockPalette.Count) });
                data.level.sites.Add(structure);
            }

            var carrier = root.AddComponent<PlayerBlockCarrier>();
            carrier.player = root.transform;
            carrier.useAtlas = true;
            carrier.material = data.blockMaterial;
            var runtime = root.AddComponent<BlockLevelRuntime>();
            runtime.autoLoadOnStart = false;
            runtime.createColliders = false;
            runtime.carrier = carrier;
            runtime.cubeMesh = live.cubeMesh;
            runtime.ghostMaterial = live.ghostMaterial;
            var session = root.AddComponent<BuildLevelSession>();
            session.runtime = runtime;
            typeof(BuildLevelSession).GetProperty("MenuOpen").SetValue(session, false);
            var zoneGO = new GameObject("BonusZone");
            zoneGO.transform.SetParent(root.transform, false);
            zoneGO.transform.localPosition = Vector3.right * 1000;
            var zone = zoneGO.AddComponent<InfiniteBlocksZone>();
            zone.session = session;
            zone.carrier = carrier;
            zone.badge = new GameObject("Badge");
            zone.badge.transform.SetParent(root.transform, false);
            runtime.LoadLevel(data);
            foreach (var target in root.GetComponentsInChildren<BuildTarget>(true))
                target.enabled = false;
            root.SetActive(true);
            if (stored == 1)
                Require(carrier.TryPickup(), "Initial normal pickup failed.");
            else if (stored > 0)
                Require(carrier.GrantForTesting(stored) == stored, "Fixture grant failed.");
            for (int i = 0; i < normalBefore; i++)
                Require(runtime.CurrentSite.TryBuild(carrier), "Initial ordinary construction failed.");
            int preserved = carrier.StoredCount;
            Require(zone.Activate(), "Bonus did not activate.");
            for (int i = 0; i < infiniteSteps; i++)
                Require(runtime.CurrentSite.TryBuild(carrier), "Infinite construction failed at " + i);
            Require(carrier.StoredCount == preserved, "Infinite mode consumed ordinary inventory.");
            int changed = 0;
            carrier.Changed += () => changed++;
            if (disable)
                zone.enabled = false;
            else
            {
                typeof(InfiniteBlocksZone).GetField("expiresAt", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(zone, Time.realtimeSinceStartup - 1);
                typeof(InfiniteBlocksZone).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(zone, null);
            }

            Require(!carrier.InfiniteBlocksActive && !zone.Active, "Infinite mode did not expire.");
            Require(carrier.Count == preserved, "Expiry changed ordinary count.");
            Require(changed == 1, "Expiry must refresh inventory once.");
            if (repeat)
            {
                Require(zone.Activate(), "Repeated bonus did not activate.");
                Require(runtime.CurrentSite.TryBuild(carrier), "Repeated infinite construction failed.");
                zone.EndBonus();
                Require(carrier.Count == preserved, "Repeated expiry changed count.");
            }

            if (preserved == 0)
            {
                Require(!carrier.TryConsume(), "Expiry granted a free ordinary block.");
                Require(carrier.TryPickup(), "Pickup after empty bonus failed.");
                preserved = 1;
            }

            int finite = Mathf.Min(preserved, runtime.TotalCount - runtime.BuiltCount);
            for (int i = 0; i < finite; i++)
            {
                BlockType expected;
                Require(runtime.TryGetSupplyType(0, out expected), "Missing pending cell.");
                Require(carrier.FirstType == expected, "Stale material after bonus: "
                    + carrier.FirstType
                    + " vs " + expected);
                Require(runtime.CurrentSite.TryBuild(carrier), "Normal building stuck after bonus at " + i);
                Require(carrier.Count == preserved - i - 1, "Finite building did not consume exactly one block.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(data);
        }
    }
}
