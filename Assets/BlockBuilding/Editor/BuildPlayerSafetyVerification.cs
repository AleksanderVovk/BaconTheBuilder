using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class BuildPlayerSafetyVerification
{
    private static BuildPlayerSafety safety;
    private static BuildLevelSession session;
    private static StringBuilder report;
    private static double waitUntil;
    private static int stage, frame;
    private static Vector3 expected;
    private static int inventory;
    public static string Status { get; private set; } = "Not run";

    public static void Begin()
    {
        if (!EditorApplication.isPlaying)
            throw new InvalidOperationException("Play mode required.");
        safety = UnityEngine.Object.FindFirstObjectByType<BuildPlayerSafety>();
        session = safety.session;
        report = new StringBuilder();
        session.Select(0);
        Assert(!safety.CanReturn && !safety.returnButton.interactable, "Return disabled before first placement");
        var runtime = safety.runtime;
        runtime.carrier.GrantForTesting(999);
        var site = runtime.CurrentSite;
        // Reproduce the first house roof course using actual campaign data.
        int guard = 0;
        while (site.transform.InverseTransformPoint(site.NextWorldPosition).y < 6 * site.BlockSize && guard++ < 1000)
            Assert(site.TryBuild(runtime.carrier), null);
        Assert(guard > 0 && guard < 1000, "Real house walls built before roof");
        Vector3 cube = site.NextWorldPosition;
        Warp(cube);
        safety.player.GetComponent<PlayerController>().velocity = Vector3.down * 20;
        inventory = runtime.carrier.Count;
        Assert(site.TryBuild(runtime.carrier), "Roof placed through existing CharacterController");
        Assert(safety.LiftCount > 0, "Automatic upward rescue fired");
        Assert(safety.character.bounds.min.y >= cube.y + site.BlockSize * 0.5f + 0.10f, "Capsule feet above new roof");
        Assert(runtime.carrier.Count == inventory - 1, "Only installed cube consumed; carried stack retained");
        Assert(
            safety.player.GetComponent<PlayerController>().velocity == Vector3.zero,
            "Existing controller falling velocity reset"
        );
        runtime.SetPaused(true); // Hold the checkpoint while testing real gravity/controller frames.
        expected = safety.player.position;
        inventory = runtime.carrier.Count;
        frame = Time.frameCount;
        stage = 0;
        waitUntil = EditorApplication.timeSinceStartup + 1;
        Status = "Waiting for real movement frames";
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup < waitUntil)
            return;
        try
        {
            Assert(Time.frameCount > frame, "Play loop advancing");
            if (stage == 0)
            {
                Assert(
                    safety.player.position.y > expected.y - 0.35f,
                    "Player remains supported after collider batching interval"
                );
                Warp(new Vector3(0, 1, 0));
                safety.player.GetComponent<PlayerController>().velocity = Vector3.down * 30;
                safety.returnButton.onClick.Invoke();
                Assert(safety.ReturnCount == 1, "Actual HUD button returns player");
                Assert(
                    Vector2.Distance(new Vector2(safety.player.position.x, safety.player.position.z), new Vector2(expected.x, expected.z)) < 0.01f,
                    "Return aligned above last installed block"
                );
                Assert(safety.player.position.y >= expected.y - 0.02f, "Return leaves headroom above roof");
                Assert(safety.runtime.carrier.Count == inventory, "Teleport preserves inventory");
                Assert(!session.IsAssistedRun, "Normal return does not invalidate records");
                expected = safety.player.position;
                stage = 1;
                waitUntil = EditorApplication.timeSinceStartup + 1;
                frame = Time.frameCount;
                return;
            }

            Assert(safety.player.position.y > expected.y - 0.35f, "Returned player stays on solid roof");
            session.OpenMenu();
            Vector3 before = safety.player.position;
            safety.ReturnToLastBlock();
            Assert(safety.player.position == before, "No return from menu");
            session.Resume();
            session.Select(1);
            Assert(!safety.CanReturn
                && safety.runtime.LastPlacedSite == null, "Last-block checkpoint cleared when changing level");
            session.Select(0);
            session.OpenMenu();
            Status = "PASS";
            Finish();
        }
        catch (Exception e)
        {
            Status = "FAIL: " + e.Message;
            report.AppendLine(Status);
            Finish();
            Debug.LogException(e);
        }
    }

    private static void Warp(Vector3 p)
    {
        bool enabled = safety.character.enabled;
        safety.character.enabled = false;
        safety.player.position = p;
        safety.character.enabled = enabled;
        session.onPlayerReset.Invoke();
        Physics.SyncTransforms();
    }

    private static void Assert(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message ?? "Typed block build failed");
        if (message != null)
            report.AppendLine("PASS: " + message);
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;
        Directory.CreateDirectory("BlockBuildingBackup");
        File.WriteAllText("BlockBuildingBackup/PlayerSafetyVerification.txt", report.ToString() + "\n" + Status);
    }
}
