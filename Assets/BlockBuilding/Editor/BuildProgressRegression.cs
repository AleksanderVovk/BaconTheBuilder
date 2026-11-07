using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using UnityEngine;
using UnityEditor;

public static class BuildProgressRegression
{
    private static void Check(bool ok, string message)
    {
        if (!ok)
            throw new InvalidOperationException(message);
    }

    private static object Call(object obj, string name, params object[] args)
    {
        return obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, args);
    }

    private static void Time(BuildLevelSession s, double value)
    {
        typeof(BuildLevelSession).GetField("elapsed", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(s, value);
    }

    private static void Build(BuildLevelSession s, int count)
    {
        foreach (var target in s.runtime.GetComponentsInChildren<BuildTarget>(true))
            target.enabled = false;
        for (int i = 0; i < count; i++)
            Check(s.runtime.CurrentSite.TryBuildGranted(), "Fixture construction failed.");
    }

    private static BuildSiteData Data(string id, Material material, Vector3 origin)
    {
        var data = ScriptableObject.CreateInstance<BuildSiteData>();
        data.useAtlas = true;
        data.blockMaterial = material;
        for (int j = 0; j < 2; j++)
        {
            var site = new BlockStructure
            {
                id = id + "_" + j,
                origin = origin + Vector3.right * j * 10
            };
            for (int i = 0; i < (j == 0 ? 6 : 5); i++)
                site.blocks.Add(new BlockCell { position = new Vector3Int(i, 0, 0), order = i, type = (BlockType)(i + j) });
            data.level.sites.Add(site);
        }

        return data;
    }

    public static string Run()
    {
        if (!EditorApplication.isPlaying)
            throw new InvalidOperationException("Run in Play Mode.");
        string a = "qa_progress_a_" + Guid.NewGuid().ToString("N"), b = "qa_progress_b_" + Guid.NewGuid().ToString("N");
        var root = new GameObject("BuildProgressRegression");
        root.SetActive(false);
        root.transform.position = new Vector3(10000, 10000, 10000);
        var originalSaves = YG.YG2.saves;
        var sdk = typeof(YG.YG2).GetField("_SDKEnabled", BindingFlags.NonPublic | BindingFlags.Static);
        bool sdkBefore = YG.YG2.isSDKEnabled;
        var lockMode = Cursor.lockState;
        bool cursorVisible = Cursor.visible;
        const string victoryKey = "BlockBuilding.Victories.v1";
        bool hadVictory = PlayerPrefs.HasKey(victoryKey);
        string victoryJson = PlayerPrefs.GetString(victoryKey, "");
        int wins = VictoryEconomy.Balance;
        var victoryField = typeof(VictoryEconomy).GetField("data", BindingFlags.NonPublic | BindingFlags.Static);
        var originalVictory = victoryField.GetValue(null);
        BuildSiteData dataA = null, dataB = null;
        BuildLevelCatalog catalog = null;
        try
        {
            sdk.SetValue(null, false);
            YG.YG2.saves = new YG.SavesYG();
            victoryField.SetValue(
                null,
                JsonUtility.FromJson(JsonUtility.ToJson(originalVictory), originalVictory.GetType())
            );
            var live = UnityEngine.Object.FindFirstObjectByType<BlockLevelRuntime>();
            dataA = Data(a, live.data.blockMaterial, root.transform.position);
            dataB = Data(b, live.data.blockMaterial, root.transform.position);
            catalog = ScriptableObject.CreateInstance<BuildLevelCatalog>();
            catalog.levels = new[]
            {
                new BuildLevelEntry
                {
                    id = a,
                    data = dataA
                },
                new BuildLevelEntry
                {
                    id = b,
                    data = dataB
                }
            };
            var carrier = root.AddComponent<PlayerBlockCarrier>();
            carrier.player = root.transform;
            carrier.material = dataA.blockMaterial;
            carrier.useAtlas = true;
            var runtime = root.AddComponent<BlockLevelRuntime>();
            runtime.autoLoadOnStart = false;
            runtime.carrier = carrier;
            runtime.cubeMesh = live.cubeMesh;
            runtime.ghostMaterial = live.ghostMaterial;
            var session = root.AddComponent<BuildLevelSession>();
            session.catalog = catalog;
            session.runtime = runtime;
            session.spawn = root.transform.position;
            root.SetActive(true);
            Call(session, "Start");
            int placed = 0;
            runtime.BlockPlaced += (type, pos) => placed++;
            session.Select(0);
            Build(session, 3);
            Time(session, 12.5);
            session.OpenMenu();
            BuildProgressStore.Snapshot snapshot;
            string signatureA = BuildProgressStore.Signature(dataA);
            Check(BuildProgressStore.TryGet(a, signatureA, 11, out snapshot)
                && snapshot.builtBlocks == 3
                && snapshot.elapsedSeconds == 12.5, "A checkpoint/timer not saved.");
            session.Select(1);
            Build(session, 2);
            Time(session, 8);
            session.OpenMenu();
            int events = placed;
            session.Select(0);
            Check(runtime.BuiltCount == 3
                && Math.Abs(session.Elapsed - 12.5) < .001, "A did not resume independently of B.");
            Check(placed == events && VictoryEconomy.Balance == wins, "Restore emitted rewards or victories.");
            Check(runtime.LastPlacedSite != null
                && runtime.LastPlacedSite.LastBuiltCell == new Vector3Int(2, 0, 0), "Teleport checkpoint not restored.");
            Check(runtime.GetComponentsInChildren<MeshCollider>().Any(c => c.sharedMesh != null
                && c.sharedMesh.vertexCount > 0), "Saved colliders not restored.");
            session.Restart();
            Check(runtime.BuiltCount == 3, "Restart erased unfinished progress.");
            Build(session, 4);
            Time(session, 14.25);
            Call(session, "OnApplicationPause", true);
            var cache = (IDictionary)typeof(BuildProgressStore).GetField("cache", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            cache.Remove(a);
            session.Select(1);
            session.Select(0);
            Check(runtime.BuiltCount == 7
                && Math.Abs(session.Elapsed - 14.25) < .001, "Pause/reload across multiple sites failed.");
            Check(runtime.CurrentSite.BuiltCount == 1, "Next site cursor not restored.");
            events = placed;
            session.Select(1);
            Check(runtime.BuiltCount == 2 && placed == events, "B cursor/reward suppression failed.");
            session.Select(0);
            Build(session, 4);
            Check(session.Completed
                && VictoryEconomy.Balance == wins + 1
                && LevelRecordStore.Completed(a), "Completion did not award exactly one win/record.");
            Check(!BuildProgressStore.TryPeek(a, out snapshot), "Completed checkpoint was not cleared.");
            session.ContinueExploring();
            Check(runtime.BuiltCount == 11, "Completed world disappeared before inspection.");
            session.Select(0);
            Check(runtime.BuiltCount == 0
                && VictoryEconomy.Balance == wins + 1, "Completed level did not start a fresh run.");
            YG.YG2.saves.buildingProgressJson = "{\"levels\":[{\"levelId\":\""
                + a
                + "\",\"signature\":\""
                + signatureA
                + "\",\"builtBlocks\":3,\"revision\":\"1\"}]}";
            BuildProgressStore.SyncCloud(catalog);
            Check(!BuildProgressStore.TryPeek(a, out snapshot), "Old cloud save resurrected a completed run.");
            string signatureB = BuildProgressStore.Signature(dataB);
            YG.YG2.saves.buildingProgressJson = "{\"levels\":[{\"levelId\":\""
                + b
                + "\",\"signature\":\""
                + signatureB
                + "\",\"builtBlocks\":3,\"elapsedSeconds\":20,\"revision\":\"99999999999999999999999999999\"}]}";
            BuildProgressStore.SyncCloud(catalog);
            events = placed;
            session.Select(1);
            Check(runtime.BuiltCount == 3
                && session.Elapsed == 20
                && placed == events, "New cloud checkpoint failed to restore silently.");
            dataB.level.blockSize = 2;
            Check(
                !BuildProgressStore.TryGet(b, BuildProgressStore.Signature(dataB), 11, out snapshot),
                "Changed geometry accepted an incompatible checkpoint."
            );
            return "PASS: independent levels; exact block cursor and elapsed time; silent mesh/collider restoration across sites; last-block teleport; pause/local reload; unfinished restart preservation; one completion win/record; fresh run after completion; completion tombstone vs old cloud; newer cloud restore; geometry compatibility guard.";
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            foreach (string id in new[]
            {
                a,
                b
            }

            )
            {
                PlayerPrefs.DeleteKey(BuildProgressStore.Prefix + id);
                PlayerPrefs.DeleteKey("BlockBuilding.Levels.v1." + id + ".done");
                PlayerPrefs.DeleteKey("BlockBuilding.Levels.v1." + id + ".best");
                ((IDictionary)typeof(BuildProgressStore).GetField("cache", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)).Remove(id);
            }

            if (hadVictory)
                PlayerPrefs.SetString(victoryKey, victoryJson);
            else
                PlayerPrefs.DeleteKey(victoryKey);
            PlayerPrefs.Save();
            victoryField.SetValue(null, originalVictory);
            YG.YG2.saves = originalSaves;
            sdk.SetValue(null, sdkBefore);
            var changed = typeof(VictoryEconomy).GetField("Changed", BindingFlags.NonPublic | BindingFlags.Static);
            if (changed != null)
                ((Action)changed.GetValue(null))?.Invoke();
            Cursor.lockState = lockMode;
            Cursor.visible = cursorVisible;
            if (catalog != null)
                UnityEngine.Object.DestroyImmediate(catalog);
            if (dataA != null)
                UnityEngine.Object.DestroyImmediate(dataA);
            if (dataB != null)
                UnityEngine.Object.DestroyImmediate(dataB);
        }
    }
}
