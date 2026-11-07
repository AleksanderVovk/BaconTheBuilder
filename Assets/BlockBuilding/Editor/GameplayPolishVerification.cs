using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Explicit editor checks. They never run automatically in a player's game.
public static class GameplayPolishVerification
{
    private static void Check(bool value, string message)
    {
        if (!value)
            throw new Exception("Gameplay polish: " + message);
    }

    [MenuItem("Tools/Block Building/Verify Placement Reach")]
    public static void CheckReachGeometry()
    {
        int checkedPositions = 0;
        var go = new GameObject("Temporary reach verification");
        try
        {
            var builder = go.AddComponent<BuildSiteBuilder>();
            builder.createColliders = false;
            var site = go.AddComponent<BuildSite>();
            site.builder = builder;
            var definition = new BlockStructure();
            definition.blocks.Add(new BlockCell { position = new Vector3Int(0, 8, 0) });
            site.Initialize(definition, 1.5f);
            site.SetActiveSite(true);
            var targetObject = new GameObject("Remote legacy pad");
            targetObject.transform.SetParent(go.transform, false);
            targetObject.transform.position = new Vector3(80, 0, 80);
            var target = targetObject.AddComponent<BuildTarget>();
            target.site = site;
            target.radius = 1.25f;
            target.allowDeliveryPoint = false;
            foreach (float yaw in new float[]
            {
                0,
                37,
                90,
                183
            }

            )
            {
                go.transform.rotation = Quaternion.Euler(0, yaw, 0);
                foreach (Vector3 scale in new Vector3[]
                {
                    Vector3.one,
                    new Vector3(1.6f, 1, 0.75f)
                }

                )
                {
                    go.transform.localScale = scale;
                    for (int i = 0; i < 16; i++)
                    {
                        float angle = i * Mathf.PI * 2 / 16;
                        Vector3 direction = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                        float edge = 0.75f / Mathf.Max(Mathf.Abs(direction.x), Mathf.Abs(direction.z));
                        Vector3 boundary = go.transform.TransformPoint(new Vector3(0.75f, 0, 0.75f) + direction * edge);
                        Vector3 outside = boundary + go.transform.TransformVector(direction).normalized * (target.radius * 0.7f);
                        outside.y = 1;
                        Check(target.ContainsPosition(outside), "approach side " + i + " yaw " + yaw);
                        checkedPositions++;
                        target.allowGroundProjection = false;
                        Check(
                            !target.ContainsPosition(outside),
                            "high target requires ground projection when player stays below"
                        );
                        outside.y = site.NextWorldPosition.y;
                        Check(target.ContainsPosition(outside), "approach at target height");
                        target.allowGroundProjection = true;
                    }
                }
            }

            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            go.transform.localScale = Vector3.one;
            target.allowDeliveryPoint = true;
            Check(target.ContainsPosition(target.transform.position + Vector3.up), "legacy pad retained");
            target.allowDeliveryPoint = false;
            Check(!target.ContainsPosition(target.transform.position + Vector3.up), "remote pad can be disabled");
            Check(!target.ContainsPosition(new Vector3(300, 1, 300)), "far player rejected");
            Check(!target.ContainsPosition(new Vector3(0.75f, 100, 0.75f)), "excessive vertical distance rejected");
            site.SetActiveSite(false);
            Check(!target.ContainsPosition(site.NextWorldPosition), "inactive structure rejected");
            Debug.Log("PLACEMENT_GEOMETRY_PASS: "
                + checkedPositions
                + " approach positions, 4 rotations, 2 scales, high layers, pad compatibility and far-player checks.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    private static StrengthTrainingZone training;
    private static Transform lu, ll, lh, ru, rl, rh;
    private static int samples, lastFrame;
    private static float started, worstGap, minPoleDot, minAngle, maxAngle, lowBar, highBar;
    private static Vector3 savedModelPosition;
    private static Quaternion savedModelRotation;
    private static bool savedController, savedAnimator;
    private static System.Numerics.BigInteger beforeStrength;
    public static string BeginTrainingProbe()
    {
        Check(EditorApplication.isPlaying, "enter Play Mode");
        EditorApplication.update -= SampleTraining;
        training = UnityEngine.Object.FindFirstObjectByType<StrengthTrainingZone>();
        if (training.IsTraining)
            training.StopTraining();
        Check(training.carrier.Count == 0, "training probe needs empty hands");
        training.strength.saveProgress = false;
        Warp(training.carrier.player, training.transform.position + Vector3.up);
        savedModelPosition = training.visualModel.localPosition;
        savedModelRotation = training.visualModel.localRotation;
        savedAnimator = training.modelAnimator.enabled;
        savedController = training.movementToPause[0].enabled;
        var a = training.modelAnimator;
        lu = a.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        ll = a.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        lh = a.GetBoneTransform(HumanBodyBones.LeftHand);
        ru = a.GetBoneTransform(HumanBodyBones.RightUpperArm);
        rl = a.GetBoneTransform(HumanBodyBones.RightLowerArm);
        rh = a.GetBoneTransform(HumanBodyBones.RightHand);
        Check(training.TryStartTraining(), "start training");
        beforeStrength = training.strength.Strength;
        samples = 0;
        lastFrame = -1;
        worstGap = 0;
        minPoleDot = 1;
        minAngle = 180;
        maxAngle = 0;
        lowBar = float.PositiveInfinity;
        highBar = float.NegativeInfinity;
        started = Time.time;
        EditorApplication.update += SampleTraining;
        return "Training probe started; observing 180 live frames after pose transition.";
    }

    private static void SampleTraining()
    {
        if (!EditorApplication.isPlaying || training == null)
        {
            EditorApplication.update -= SampleTraining;
            return;
        }

        if (Time.frameCount == lastFrame || Time.time < started + training.poseBlendSeconds + 0.15f)
            return;
        lastFrame = Time.frameCount;
        try
        {
            Check(training.IsTraining, "training stays active");
            SampleArm(lu, ll, lh, training.LeftWristTargetPosition);
            SampleArm(ru, rl, rh, training.RightWristTargetPosition);
            lowBar = Mathf.Min(lowBar, training.barbell.position.y);
            highBar = Mathf.Max(highBar, training.barbell.position.y);
            samples++;
            if (samples < 180 || Time.time - started < 4)
                return;
            EditorApplication.update -= SampleTraining;
            Check(worstGap < 0.008f, "wrists stay on bar; worst gap=" + worstGap);
            Check(minPoleDot > 0.9f, "both elbows remain in the intended bend plane");
            Check(minAngle > 25 && maxAngle < 165, "no reversed/locked elbows");
            Check(highBar - lowBar > 0.1f, "visible full bar travel");
            Check(training.strength.Strength > beforeStrength, "completed reps still award strength");
            training.StopTraining();
            Check(!training.carrier.IsTraining
                && training.modelAnimator.enabled == savedAnimator
                && training.movementToPause[0].enabled == savedController, "restore controls and Animator");
            Check((training.visualModel.localPosition - savedModelPosition).sqrMagnitude < 0.000001f
                && Quaternion.Angle(training.visualModel.localRotation, savedModelRotation) < 0.01f, "restore visual model");
            string report = "TRAINING_PASS: "
                + samples
                + " live frames; worst wrist-target gap="
                + worstGap.ToString("F6")
                + " m; min elbow pole dot="
                + minPoleDot.ToString("F4")
                + "; elbow angles="
                + minAngle.ToString("F2")
                + ".."
                + maxAngle.ToString("F2")
                + " deg; bar Y="
                + lowBar.ToString("F3")
                + ".."
                + highBar.ToString("F3")
                + "; strength awards and state restoration passed.";
            File.WriteAllText("Temp/GameplayPolishTraining.txt", report);
            Debug.Log(report);
        }
        catch (Exception e)
        {
            EditorApplication.update -= SampleTraining;
            training.StopTraining();
            File.WriteAllText("Temp/GameplayPolishTraining.txt", "FAIL: " + e.Message);
            Debug.LogException(e);
        }
    }

    private static void SampleArm(Transform upper, Transform lower, Transform hand, Vector3 grip)
    {
        worstGap = Mathf.Max(worstGap, Vector3.Distance(hand.position, grip));
        Vector3 d = (hand.position - upper.position).normalized;
        Vector3 outwards = training.barbell.right * Mathf.Sign(Vector3.Dot(grip - training.barbell.position, training.barbell.right));
        Vector3 hint = outwards * training.elbowSpread - training.visualModel.forward * training.elbowDrop - training.visualModel.up * training.elbowFootOffset;
        Vector3 actual = Vector3.ProjectOnPlane(lower.position - upper.position, d).normalized;
        float dot = Vector3.Dot(actual, Vector3.ProjectOnPlane(hint, d).normalized);
        minPoleDot = Mathf.Min(minPoleDot, dot);
        float angle = Vector3.Angle(upper.position - lower.position, hand.position - lower.position);
        minAngle = Mathf.Min(minAngle, angle);
        maxAngle = Mathf.Max(maxAngle, angle);
    }

    private static BlockLevelRuntime runtime;
    private static BuildTarget target;
    private static Behaviour movement;
    private static bool previousMovement;
    private static int side, beforeBuild;
    private static float stepTime;
    public static string BeginApproachProbe()
    {
        Check(EditorApplication.isPlaying, "enter Play Mode");
        EditorApplication.update -= SampleApproach;
        runtime = UnityEngine.Object.FindFirstObjectByType<BlockLevelRuntime>();
        Check(runtime.CurrentSite != null && runtime.carrier.Count == 0, "fresh unfinished structure and empty hands");
        target = runtime.CurrentSite.GetComponentInChildren<BuildTarget>();
        movement = runtime.carrier.player.GetComponent("PlayerController") as Behaviour;
        previousMovement = movement != null && movement.enabled;
        if (movement != null)
            movement.enabled = false;
        runtime.carrier.strength.saveProgress = false;
        side = 0;
        PrepareApproach();
        EditorApplication.update += SampleApproach;
        return "Automatic transfer probe started: 8 directions around actual ghosts, then original delivery pad.";
    }

    private static void PrepareApproach()
    {
        Check(runtime.carrier.TryPickup(), "pick queued texture");
        beforeBuild = target.site.BuiltCount;
        Vector3 p;
        if (side == 8)
            p = target.transform.position;
        else
        {
            float a = side * Mathf.PI / 4;
            Vector3 d = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
            float half = target.site.BlockSize * 0.5f / Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.z));
            p = target.site.NextWorldPosition + d * (half + target.radius * 0.7f);
        }

        p.y = target.transform.position.y + 1;
        Warp(runtime.carrier.player, p);
        Check(target.IsInRange, "direction " + side + " is in reach");
        stepTime = Time.time;
    }

    private static void SampleApproach()
    {
        if (!EditorApplication.isPlaying || runtime == null)
        {
            EditorApplication.update -= SampleApproach;
            return;
        }

        try
        {
            if (runtime.carrier.Count != 0)
            {
                Check(Time.time - stepTime < 2, "automatic transfer timed out at side " + side);
                return;
            }

            Check(target.site.BuiltCount == beforeBuild + 1, "exactly one block transferred at side " + side);
            side++;
            if (side <= 8)
            {
                PrepareApproach();
                return;
            }

            EditorApplication.update -= SampleApproach;
            Check(!target.ContainsPosition(new Vector3(150, 1, 150)), "far player rejected");
            Warp(runtime.carrier.player, runtime.GetComponentInChildren<BlockPickupZone>().transform.position + Vector3.up);
            if (movement != null)
                movement.enabled = previousMovement;
            string report = "APPROACH_PASS: actual Update transferred one typed cube from each of 8 directions plus legacy pad; 9 placements, no duplicate consumption; far player rejected.";
            File.WriteAllText("Temp/GameplayPolishApproach.txt", report);
            Debug.Log(report);
        }
        catch (Exception e)
        {
            EditorApplication.update -= SampleApproach;
            if (movement != null)
                movement.enabled = previousMovement;
            File.WriteAllText("Temp/GameplayPolishApproach.txt", "FAIL: " + e.Message);
            Debug.LogException(e);
        }
    }

    public static string SampleRep(float normalizedPhase)
    {
        var t = UnityEngine.Object.FindFirstObjectByType<StrengthTrainingZone>();
        if (!t.IsTraining)
        {
            Warp(t.carrier.player, t.transform.position + Vector3.up);
            t.strength.saveProgress = false;
            Check(t.TryStartTraining(), "start sampled pose");
        }

        Time.timeScale = 0;
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(StrengthTrainingZone).GetField("elapsed", flags).SetValue(t, t.poseBlendSeconds + 1);
        typeof(StrengthTrainingZone).GetField("phase", flags).SetValue(t, normalizedPhase);
        typeof(StrengthTrainingZone).GetMethod("LateUpdate", flags).Invoke(t, null);
        return "Rep phase " + normalizedPhase + "; left grip=" + t.LeftGripPosition + "; right grip=" + t.RightGripPosition;
    }

    private static void Warp(Transform player, Vector3 position)
    {
        var cc = player.GetComponent<CharacterController>();
        bool active = cc != null && cc.enabled;
        if (cc != null)
            cc.enabled = false;
        player.position = position;
        if (cc != null)
            cc.enabled = active;
    }
}
