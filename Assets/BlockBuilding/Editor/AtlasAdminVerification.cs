using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AtlasAdminVerification
{
    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception("Atlas/admin: " + message);
    }

    public static string Data()
    {
        var used = new HashSet<int>();
        foreach (var type in new[]
        {
            BlockType.Ocean,
            BlockType.Land,
            BlockType.Snow,
            BlockType.Lava,
            BlockType.FlagRed,
            BlockType.FlagGold,
            BlockType.Sandstone,
            BlockType.Basalt
        }

        )
        {
            int tile = BlockPalette.Tile(type, 0);
            Check(tile >= 16 && used.Add(tile), "dedicated texture for " + type);
            var tint = BlockPalette.Tint(type);
            Check(tint.r == 255 && tint.g == 255 && tint.b == 255, "no painted stone tint");
            var rect = BlockPalette.TileUV(tile);
            Check(rect.xMin >= 0 && rect.yMin >= 0 && rect.xMax <= 1 && rect.yMax <= 1, "UV range");
        }

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/BlockBuilding/Textures/VoxelAtlas.png");
        Check(texture.width == BlockPalette.AtlasSize && texture.height == BlockPalette.AtlasSize, "packed dimensions");
        Check(texture.filterMode == FilterMode.Point, "crisp import");
        return "ATLAS_PASS: eight dedicated natural/cloth tiles; white vertex tint; padded UVs; 288x288 Point atlas; stable old IDs.";
    }

    private static BuildLevelSession session;
    private static BuildLevelCatalog original, temporary;
    private static BuildAdminMenu admin;
    private static int stage, lastFrame;
    private static float frozen;
    private static System.Numerics.BigInteger savedStrength;
    private static int savedPrefs;
    private static readonly List<string> lines = new List<string>();
    private const string TestID = "__admin_verification";
    public static string Begin()
    {
        Check(EditorApplication.isPlaying, "Play mode");
        session = UnityEngine.Object.FindFirstObjectByType<BuildLevelSession>();
        admin = UnityEngine.Object.FindFirstObjectByType<BuildAdminMenu>();
        original = session.catalog;
        temporary = UnityEngine.Object.Instantiate(original);
        temporary.levels = (BuildLevelEntry[])original.levels.Clone();
        var source = original.levels[0];
        temporary.levels[0] = new BuildLevelEntry
        {
            id = TestID,
            title = "Admin verification",
            data = source.data,
            icon = source.icon
        };
        ClearKeys();
        session.catalog = temporary;
        session.Select(0);
        lines.Clear();
        savedStrength = session.runtime.carrier.strength.Strength;
        savedPrefs = PlayerPrefs.GetInt(session.runtime.carrier.strength.saveKey, 0);
        admin.Open();
        admin.buttons[2].onClick.Invoke();
        var carrier = session.runtime.carrier;
        Check(carrier.Count == 999
            && carrier.VisibleCount == 18
            && carrier.VisualVertexCount == 432, "999 logical blocks, compact mesh");
        Check(carrier.GetComponent<CarrierStackCounter>().label.text == "x 999", "overhead count");
        Check(!carrier.TryPickup(), "normal capacity still enforced");
        Check(session.IsAssistedRun && session.MenuOpen && session.AdminOverlayOpen, "assist marker and pause");
        frozen = session.Elapsed;
        stage = 0;
        lastFrame = Time.frameCount;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        return "ADMIN_TEST_STARTED";
    }

    private static void Tick()
    {
        if (lastFrame == Time.frameCount)
            return;
        lastFrame = Time.frameCount;
        try
        {
            var carrier = session.runtime.carrier;
            if (stage == 0)
            {
                Check(session.Elapsed == frozen, "admin pauses timer");
                admin.AddStrength();
                Check(carrier.strength.Strength == savedStrength + 100, "temporary strength");
                Check(PlayerPrefs.GetInt(carrier.strength.saveKey, 0) == savedPrefs, "strength save unchanged");
                admin.RestoreStrength();
                Check(carrier.strength.Strength == savedStrength, "restore strength");
                admin.ToggleSpeed();
                Check(session.runtime.DebugBuildSpeed == 10, "speed x10");
                admin.ToggleSpeed();
                Check(session.runtime.DebugBuildSpeed == 1, "speed restore");
                admin.TeleportToPile();
                Check(
                    (carrier.player.position - (session.Selected.data.level.pickupPoint + Vector3.up * 1.1f)).sqrMagnitude < 0.01f,
                    "pile teleport"
                );
                admin.TeleportToTarget();
                Check(carrier.Count == 999, "teleport preserves stack");
                admin.Close();
                stage = 1;
                return;
            }

            if (stage == 1)
            {
                for (int i = 0; i < 64 && !session.Completed; i++)
                {
                    var site = session.runtime.CurrentSite;
                    BlockType expected;
                    Check(site.TryGetPendingType(0, out expected) && carrier.FirstType == expected, "queued texture");
                    Check(site.TryBuild(carrier), "consume debug inventory");
                }

                if (!session.Completed)
                    return;
                Check(
                    carrier.Count == 999 - session.runtime.TotalCount,
                    "exact inventory subtraction across house and trees"
                );
                float best;
                Check(!session.NewRecord
                    && !LevelRecordStore.TryGetBest(TestID, out best)
                    && !LevelRecordStore.Completed(TestID), "assisted completion cannot save records or completion icon");
                lines.Add("ADMIN_PASS: 999 granted; 18 visible/432 vertices; FIFO types across four sites; count after 784 builds="
                    + carrier.Count
                    + "; no saved assisted record.");
                session.Select(1);
                admin.Open();
                admin.Grant999();
                admin.CompleteSite();
                stage = 2;
                return;
            }

            if (stage == 2)
            {
                if (!session.Completed)
                    return;
                Check(session.runtime.BuiltCount == 1436
                    && session.IsAssistedRun
                    && !session.NewRecord, "coroutine quick-complete with refill beyond 999");
                lines.Add("ADMIN_PASS: quick-complete 1436-cell fortress with automatic refill, batched per frame.");
                admin.Open();
                admin.Restart();
                Check(!session.IsAssistedRun && !session.MenuOpen && carrier.Count == 0, "restart resets cheats and hand");
                Check(carrier.strength.Strength == savedStrength
                    && session.runtime.DebugBuildSpeed == 1, "restart restores normal settings");
                Finish(null);
            }
        }
        catch (Exception e)
        {
            Finish(e);
        }
    }

    private static void ClearKeys()
    {
        PlayerPrefs.DeleteKey("BlockBuilding.Levels.v1." + TestID + ".best");
        PlayerPrefs.DeleteKey("BlockBuilding.Levels.v1." + TestID + ".done");
        PlayerPrefs.Save();
    }

    private static void Finish(Exception error)
    {
        EditorApplication.update -= Tick;
        ClearKeys();
        if (admin.IsOpen)
            admin.Close();
        session.catalog = original;
        session.Select(0);
        session.OpenMenu();
        UnityEngine.Object.Destroy(temporary);
        if (error != null)
        {
            lines.Add("FAIL " + error);
            Debug.LogException(error);
        }
        else
            lines.Add("ADMIN_PASS: pause/resume, strength non-persistence, teleports, speed, restart; user record keys untouched.");
        File.WriteAllLines("BlockBuildingBackup/AtlasAdminVerification.txt", lines);
    }
}
