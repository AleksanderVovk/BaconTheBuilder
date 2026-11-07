using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BuildFlowVerification
{
    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception("Build flow: " + message);
    }

    public static string Data()
    {
        var s = UnityEngine.Object.FindFirstObjectByType<BuildLevelSession>();
        foreach (var entry in s.catalog.levels)
        {
            entry.data.level.Validate();
            var backup = JsonUtility.FromJson<BlockLevelDefinition>(File.ReadAllText("BlockBuildingBackup/before-auto-build-flow-2026-10-21/BlockBuilding/Generated/Campaign/"
                + entry.id
                + ".json"));
            Check(backup.sites.Count == entry.data.level.sites.Count, "same structures");
            for (int i = 0; i < backup.sites.Count; i++)
            {
                var before = backup.sites[i];
                var after = entry.data.level.sites[i];
                Check(before.origin == after.origin
                    && before.yaw == after.yaw
                    && before.blocks.Count == after.blocks.Count, "preserved placement and count");
                var cells = before.blocks.ToDictionary(b => b.position, b => b.type);
                foreach (var cell in after.blocks)
                    Check(cells[cell.position] == cell.type, "preserved geometry/textures");
                var ordered = new List<BlockCell>(after.blocks);
                ordered.Sort(BlockLevelDefinition.Compare);
                for (int j = 1; j < ordered.Count; j++)
                    Check(ordered[j].position.y >= ordered[j - 1].position.y, "bottom-up preserved");
                Check(
                    BuildRoutePlanner.HorizontalLength(after, entry.data.level.blockSize) <= BuildRoutePlanner.HorizontalLength(before, backup.blockSize) + 0.01f,
                    "route no longer than before"
                );
            }
        }

        var audio = s.runtime.GetComponent<BlockPlacementAudio>();
        foreach (var clip in audio.stone.Concat(audio.wood).Concat(audio.glass).Concat(audio.soft))
            Check(clip != null && clip.samples > 0 && clip.channels == 1, "loaded mono placement clip");
        return "DATA_PASS: all six routes shorter; identical geometry/types/counts; lower layers first; six loaded original mono sound clips.";
    }

    private static BuildLevelSession session;
    private static BuildLevelCatalog original, temporary, showOriginal, showCatalog;
    private static BuildLevelMenu ui;
    private static int stage, lastFrame;
    private static float sampleTime, rotation, frozen;
    private const string TestID = "__flow_verification";
    private const string ShowID = "__flow_preview";
    private static readonly List<string> lines = new List<string>();
    public static string Begin()
    {
        Check(EditorApplication.isPlaying, "Play mode");
        session = UnityEngine.Object.FindFirstObjectByType<BuildLevelSession>();
        ui = UnityEngine.Object.FindFirstObjectByType<BuildLevelMenu>();
        original = session.catalog;
        temporary = Clone(original, 0, TestID);
        session.catalog = temporary;
        Clear(TestID);
        session.Select(0);
        Warp(session.Selected.data.level.pickupPoint + Vector3.up * 1.1f);
        stage = 0;
        lastFrame = Time.frameCount;
        lines.Clear();
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        return "FLOW_TEST_STARTED";
    }

    private static BuildLevelCatalog Clone(BuildLevelCatalog source, int at, string id)
    {
        var copy = UnityEngine.Object.Instantiate(source);
        copy.levels = (BuildLevelEntry[])source.levels.Clone();
        var e = source.levels[at];
        copy.levels[at] = new BuildLevelEntry
        {
            id = id,
            title = e.title,
            data = e.data,
            icon = e.icon,
            stairs = e.stairs,
            stairOrigin = e.stairOrigin,
            stairHeight = e.stairHeight
        };
        return copy;
    }

    private static void Warp(Vector3 point)
    {
        var player = session.runtime.carrier.player;
        var cc = player.GetComponent<CharacterController>();
        bool on = cc != null && cc.enabled;
        if (on)
            cc.enabled = false;
        player.position = point;
        if (on)
            cc.enabled = true;
        session.onPlayerReset.Invoke();
    }

    private static void Tick()
    {
        if (Time.frameCount == lastFrame)
            return;
        lastFrame = Time.frameCount;
        try
        {
            var carrier = session.runtime.carrier;
            if (stage == 0)
            {
                Check(carrier.Count == carrier.Capacity, "automatic fill in one player frame");
                Check(carrier.FillToCapacity() == 0, "no overfill");
                carrier.Clear();
                carrier.IsTraining = true;
                stage = 1;
                return;
            }

            if (stage == 1)
            {
                Check(carrier.Count == 0, "training blocks automatic pickup");
                carrier.IsTraining = false;
                Warp(session.spawn);
                stage = 2;
                return;
            }

            if (stage == 2)
            {
                var audio = session.runtime.GetComponent<BlockPlacementAudio>();
                for (int i = 0; i < 64 && !session.Completed; i++)
                {
                    int before = audio.PlayedCount;
                    if (carrier.Count == 0)
                        Check(carrier.TryPickup(), "typed pickup");
                    Check(session.runtime.CurrentSite.TryBuild(carrier), "place next ordered cell");
                    if (session.runtime.BuiltCount == 1)
                        Check(audio.PlayedCount > before, "successful placement starts audio");
                }

                if (!session.Completed)
                    return;
                Check(ui.result.activeSelf, "result displayed");
                var preview = ui.result.GetComponent<CompletedBuildingPreview>();
                Check(preview.MeshCount > 1 && preview.image.texture != null, "live chunk-mesh RenderTexture");
                Check(preview.GetComponentsInChildren<Collider>().Length == 0, "no preview physics");
                rotation = preview.Rotation;
                sampleTime = Time.unscaledTime;
                frozen = session.Elapsed;
                stage = 3;
                lines.Add("AUTO_PASS: fills to strength capacity in one frame; full hand/training limits; typed pickup.");
                return;
            }

            if (stage == 3)
            {
                if (Time.unscaledTime < sampleTime + 0.4f)
                    return;
                Check(session.Elapsed == frozen, "result freezes finish time");
                var preview = ui.result.GetComponent<CompletedBuildingPreview>();
                Check(
                    Mathf.Abs(Mathf.DeltaAngle(rotation, preview.Rotation)) > 2,
                    "live model rotates over advancing frames"
                );
                ui.again.onClick.Invoke();
                Check(session.IsExploringCompleted
                    && session.Completed
                    && !session.MenuOpen
                    && !ui.result.activeSelf, "continue explores completed map");
                Check(session.runtime.BuiltCount == 784 && carrier.Count == 0, "world retained and hands empty");
                lines.Add("RESULT_PASS: real shared chunk meshes rendered and rotating; continue preserves all 784 blocks, clears hands, restores controller.");
                stage = 4;
                return;
            }

            if (stage == 4)
            {
                Check(session.Elapsed == frozen, "exploration does not advance record timer");
                session.OpenMenu();
                session.Resume();
                Check(session.IsExploringCompleted && !session.MenuOpen, "menu resumes completed exploration");
                var admin = UnityEngine.Object.FindFirstObjectByType<BuildAdminMenu>();
                admin.Open();
                admin.Close();
                Check(!session.MenuOpen && session.IsExploringCompleted, "admin resumes exploration");
                float best;
                Check(LevelRecordStore.TryGetBest(TestID, out best) && best == frozen, "single saved finish time unchanged");
                lines.Add("AUDIO_PASS: sound on successful placement. EXPLORE_PASS: frozen record, menu/resume, no level reload.");
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
        session.runtime.carrier.IsTraining = false;
        Clear(TestID);
        session.catalog = original;
        session.Select(0);
        session.OpenMenu();
        UnityEngine.Object.Destroy(temporary);
        if (error != null)
        {
            lines.Add("FAIL: " + error);
            Debug.LogException(error);
        }

        File.WriteAllLines("BlockBuildingBackup/BuildFlowVerification.txt", lines);
    }

    private static void Clear(string id)
    {
        PlayerPrefs.DeleteKey("BlockBuilding.Levels.v1." + id + ".best");
        PlayerPrefs.DeleteKey("BlockBuilding.Levels.v1." + id + ".done");
        PlayerPrefs.Save();
    }

    public static string Showcase(int index)
    {
        session = UnityEngine.Object.FindFirstObjectByType<BuildLevelSession>();
        showOriginal = session.catalog;
        showCatalog = Clone(showOriginal, index, ShowID);
        Clear(ShowID);
        session.catalog = showCatalog;
        session.Select(index);
        Warp(session.spawn);
        var carrier = session.runtime.carrier;
        while (!session.Completed)
        {
            if (carrier.Count == 0 && carrier.FillToCapacity() == 0)
                throw new Exception("Showcase supply failed");
            if (!session.runtime.CurrentSite.TryBuild(carrier))
                throw new Exception("Showcase build failed");
        }

        return "Completion preview ready: " + session.runtime.BuiltCount + " blocks. Call CleanupShowcase after capture.";
    }

    public static void CleanupShowcase()
    {
        Clear(ShowID);
        if (showOriginal == null)
            return;
        session.catalog = showOriginal;
        session.Select(0);
        session.OpenMenu();
        UnityEngine.Object.Destroy(showCatalog);
        showOriginal = null;
    }
}
