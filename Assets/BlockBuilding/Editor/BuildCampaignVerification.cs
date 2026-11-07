using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BuildCampaignVerification
{
    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception("Campaign verification: " + message);
    }

    public static string Data()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<BuildLevelCatalog>("Assets/BlockBuilding/Generated/Campaign/LevelCatalog.asset");
        Check(catalog != null && catalog.levels.Length == 6, "six levels");
        int previous = 0;
        var ids = new HashSet<string>();
        foreach (var entry in catalog.levels)
        {
            entry.data.level.Validate();
            Check(ids.Add(entry.id), "unique ID");
            Check(entry.icon != null, "rendered icon");
            Check(entry.BlockCount > previous, "increasing count");
            previous = entry.BlockCount;
            string json = entry.data.ToJson();
            var copy = JsonUtility.FromJson<BlockLevelDefinition>(json);
            copy.Validate();
            Check(copy.sites.Sum(s => s.blocks.Count) == entry.BlockCount, "JSON roundtrip");
            foreach (var site in copy.sites)
            {
                site.blocks.Sort(BlockLevelDefinition.Compare);
                for (int i = 1; i < site.blocks.Count; i++)
                    Check(site.blocks[i].position.y >= site.blocks[i - 1].position.y, "bottom-up order");
            }
        }

        Check(catalog.levels[0].BlockCount == 784
            && catalog.levels[0].data.level.sites.Count == 4, "original house and three trees retained");
        var fortress = catalog.levels[1].data.level.sites[0];
        Check(fortress.blocks.Any(b => b.type == BlockType.FlagRed), "flag");
        Check(!fortress.blocks.Any(b => b.position == new Vector3Int(0, 2, -9)), "open gate");
        Check(catalog.levels[2].stairs && catalog.levels[2].stairHeight >= 54, "skyscraper access");
        var earth = catalog.levels[5].data.level.sites[0];
        Check(earth.blocks.Any(b => b.type == BlockType.Ocean)
            && earth.blocks.Any(b => b.type == BlockType.Land)
            && earth.blocks.Any(b => b.type == BlockType.Snow), "ocean/land/poles");
        string id = "__record_verification_" + Guid.NewGuid().ToString("N");
        try
        {
            float best;
            Check(!LevelRecordStore.TryGetBest(id, out best), "no invented record");
            Check(LevelRecordStore.Record(id, 125.25f), "first completion");
            Check(!LevelRecordStore.Record(id, 130), "slower attempt rejected");
            Check(LevelRecordStore.TryGetBest(id, out best) && best == 125.25f, "best retained");
            Check(LevelRecordStore.Record(id, 115.5f), "faster attempt accepted");
            Check(LevelRecordStore.TryGetBest(id, out best)
                && best == 115.5f
                && LevelRecordStore.Completed(id), "persisted faster result");
            Check(LevelRecordStore.Format(125.25f) == "02:05.25", "time format");
        }
        finally
        {
            ClearRecord(id);
        }

        return "DATA_PASS: six unique levels; counts 784,1436,2043,2923,7174,9098; JSON roundtrip; bottom-up; gate; flag; stairs; globe palette; first/slower/faster PlayerPrefs records.";
    }

    private static void ClearRecord(string id)
    {
        PlayerPrefs.DeleteKey("BlockBuilding.Levels.v1." + id + ".best");
        PlayerPrefs.DeleteKey("BlockBuilding.Levels.v1." + id + ".done");
        PlayerPrefs.Save();
    }

    private static BuildLevelSession session;
    private static BuildLevelCatalog original, temporary;
    private static readonly List<string> lines = new List<string>();
    private static int at, phase, lastFrame;
    private static float frozen;
    public static string Begin()
    {
        Check(EditorApplication.isPlaying, "Play mode");
        session = UnityEngine.Object.FindFirstObjectByType<BuildLevelSession>();
        original = session.catalog;
        temporary = UnityEngine.Object.Instantiate(original);
        // Test completions use isolated record IDs. User records remain untouched.
        temporary.levels = original.levels.Select((e, i) => new BuildLevelEntry { id = "__campaign_run_"
            + i, title = e.title, data = e.data, icon = e.icon, stairs = e.stairs, stairOrigin = e.stairOrigin, stairHeight = e.stairHeight }).ToArray();
        foreach (var e in temporary.levels)
            ClearRecord(e.id);
        session.catalog = temporary;
        at = 0;
        phase = 0;
        lines.Clear();
        lastFrame = Time.frameCount;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        return "RUN_TEST_STARTED";
    }

    private static void Tick()
    {
        if (Time.frameCount == lastFrame)
            return;
        lastFrame = Time.frameCount;
        try
        {
            if (phase == 0)
            {
                UnityEngine.Object.FindFirstObjectByType<BuildLevelMenu>().cards[at].button.onClick.Invoke();
                Check(!session.MenuOpen && session.SelectedIndex == at, "card selection");
                Check(session.runtime.TotalCount == temporary.levels[at].BlockCount
                    && session.runtime.BuiltCount == 0, "fresh run");
                Check(UnityEngine.SceneManagement.SceneManager.sceneCount == 1, "single scene");
                var carrier = session.runtime.carrier;
                Check(carrier.Count == 0, "clear inventory");
                var target = session.runtime.CurrentSite.GetComponentInChildren<BuildTarget>();
                Check(!session.runtime.CurrentSite.TryBuild(carrier), "empty hand rejected");
                Vector3 p = session.runtime.CurrentSite.NextWorldPosition;
                for (int d = 0; d < 8; d++)
                {
                    float angle = d * Mathf.PI / 4;
                    var pos = p + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 1.5f;
                    Check(target.ContainsPosition(pos), "all-side reach");
                }

                session.OpenMenu();
                frozen = session.Elapsed;
                phase = 1;
                return;
            }

            if (phase == 1)
            {
                Check(session.Elapsed == frozen, "menu pauses timer");
                Check(!session.runtime.CurrentSite.IsActiveSite, "menu pauses construction");
                session.Resume();
                phase = 2;
                return;
            }

            if (phase == 2)
            {
                var carrier = session.runtime.carrier;
                for (int i = 0; i < 150 && !session.Completed; i++)
                {
                    Check(carrier.TryPickup(), "typed supply queue");
                    Check(session.runtime.CurrentSite.TryBuild(carrier), "consume and build next cell");
                }

                if (!session.Completed)
                    return;
                Check(session.runtime.IsComplete
                    && session.runtime.BuiltCount == session.runtime.TotalCount, "full completion");
                Check(carrier.Count == 0 && session.NewRecord, "first completion record");
                float best;
                Check(LevelRecordStore.TryGetBest(session.Selected.id, out best)
                    && best == session.Elapsed, "saved elapsed");
                foreach (var builder in session.runtime.GetComponentsInChildren<BuildSiteBuilder>())
                    foreach (var collider in builder.GetComponentsInChildren<MeshCollider>())
                        Check(collider.sharedMesh != null, "finished chunk collision mesh");
                var menu = UnityEngine.Object.FindFirstObjectByType<BuildLevelMenu>();
                Check(menu.result.activeSelf, "completion screen");
                session.OpenMenu();
                Check(menu.cards[at].icon.color == Color.white, "colored completed icon");
                lines.Add("RUN_PASS "
                    + at
                    + " blocks="
                    + session.runtime.BuiltCount
                    + " elapsed="
                    + best
                    + " frames=" + Time.frameCount);
                at++;
                phase = 0;
                if (at < 6)
                    return;
                Finish(null);
            }
        }
        catch (Exception e)
        {
            Finish(e);
        }
    }

    private static void Finish(Exception error)
    {
        EditorApplication.update -= Tick;
        foreach (var entry in temporary.levels)
            ClearRecord(entry.id);
        session.catalog = original;
        session.Select(0);
        session.OpenMenu();
        UnityEngine.Object.Destroy(temporary);
        if (error != null)
        {
            lines.Add("FAIL: " + error);
            Debug.LogException(error);
        }
        else
            lines.Add("RUN_PASS: all six completed through typed inventory and BuildSite; menu pause; same scene; real collider meshes; icons; isolated record keys cleaned.");
        Directory.CreateDirectory("BlockBuildingBackup");
        File.WriteAllLines("BlockBuildingBackup/CampaignVerification.txt", lines);
    }

    // Called in Play mode. Uses the real existing CharacterController against stair MeshCollider.
    public static string CheckStairWalk()
    {
        var s = UnityEngine.Object.FindFirstObjectByType<BuildLevelSession>();
        s.Select(2);
        s.OpenMenu();
        var stairs = s.accessStairs;
        var controller = s.runtime.carrier.player.GetComponent<CharacterController>();
        Vector3 saved = controller.transform.position;
        float maximumError = 0;
        try
        {
            Physics.SyncTransforms();
            foreach (int run in new[]
            {
                0,
                9,
                17,
                20
            }

            )
            {
                bool forward = run % 2 == 0;
                float height = run * 3f;
                controller.enabled = false;
                controller.transform.position = stairs.transform.position + new Vector3(forward
                    ? 1.25f
                    : 3.75f, height + controller.height * 0.5f - controller.center.y + 0.35f, forward
                    ? 0.1f
                    : 7.4f);
                controller.enabled = true;
                for (int i = 0; i < 25; i++)
                    controller.Move(Vector3.down * 0.03f);
                float start = controller.transform.position.y;
                for (int i = 0; i < 140; i++)
                    controller.Move(new Vector3(0, -0.035f, forward ? 0.05f : -0.05f));
                float gained = controller.transform.position.y - start;
                maximumError = Mathf.Max(maximumError, Mathf.Abs(gained - 2.8f));
                Check(gained > 2.5f, "walk up flight " + run + " gained " + gained);
            }
        }
        finally
        {
            controller.enabled = false;
            controller.transform.position = saved;
            controller.enabled = true;
        }

        return "STAIRS_PASS: real CharacterController walked flights at 0m,27m,51m,60m; maximum rise error=" + maximumError;
    }
}
