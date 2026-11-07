using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class TrainingPolishVerification
{
    private static TrainingPresentation p;
    private static double next;
    private static int stage;
    private static System.Numerics.BigInteger progressBase;
    private static float startProgress, startTime, normalRate, fastRate;
    public static string Status { get; private set; } = "Not run";

    public static void Begin()
    {
        p = UnityEngine.Object.FindFirstObjectByType<TrainingPresentation>();
        var h = p.training.GetComponent<GymEquipmentHub>();
        h.session.Select(0);
        p.training.SelectEquipment(h.equipment[0]);
        p.training.strength.SetStrengthForTesting(p.training.strength.Strength);
        progressBase = p.training.strength.Strength;
        var cc = p.training.carrier.player.GetComponent<CharacterController>();
        cc.enabled = false;
        cc.transform.position = h.equipment[0].transform.position + new Vector3(0, 1, .9f);
        cc.enabled = true;
        h.session.onPlayerReset.Invoke();
        stage = 0;
        next = Time.unscaledTime + .3;
        Status = "Running";
        EditorApplication.update += Tick;
    }

    private static float Progress
    {
        get
        {
            return (float)(p.training.strength.Strength - progressBase) + p.training.NormalizedPhase;
        }
    }

    private static void Sample()
    {
        startTime = Time.time;
        startProgress = Progress;
        next = Time.unscaledTime + 2;
    }

    private static void Tick()
    {
        try
        {
            if (stage == 3)
                p.RegisterTap();
            if (Time.unscaledTime < next)
                return;
            if (stage == 0)
            {
                if (!p.training.TryStartTraining())
                    throw new Exception("Training start failed");
                stage = 1;
                next = Time.unscaledTime + 1;
                return;
            }

            if (stage == 1)
            {
                if (p.cameraDriver.enabled || p.view.transform.position.y - p.training.InteractionPosition.y < 3)
                    throw new Exception("Overhead camera failed");
                Sample();
                stage = 2;
                return;
            }

            if (stage == 2)
            {
                normalRate = (Progress - startProgress) / (Time.time - startTime);
                Sample();
                stage = 3;
                return;
            }

            if (stage == 3)
            {
                fastRate = (Progress - startProgress) / (Time.time - startTime);
                if (fastRate < normalRate * 1.8f)
                    throw new Exception("Taps did not accelerate repetitions");
                if (p.hint.gameObject.activeSelf)
                    throw new Exception("Three-second hint did not hide");
                var v = p.GetComponent<TrainingEffortVfx>();
                bool effect = v.Active
                    && v.trails[0].emitting
                    && v.trails[1].emitting
                    && v.trails[0].sharedMaterial.shader.isSupported;
                if (!effect)
                    throw new Exception("No local effort VFX");
                foreach (var line in p.streaks)
                    if (line.gameObject.activeSelf)
                        throw new Exception("Legacy screen streak active");
                ScreenCapture.CaptureScreenshot("BlockBuildingBackup/NaturalEffortPlaytest.png");
                stage = 4;
                next = Time.unscaledTime + 3;
                return;
            }

            if (stage == 4)
            {
                if (p.Intensity > .01f || p.training.RepetitionSpeed > 1.01f)
                    throw new Exception("Speed did not decay");
                p.training.StopTraining();
                stage = 5;
                next = Time.unscaledTime + 1;
                return;
            }

            if (p.CameraControlled || !p.cameraDriver.enabled)
                throw new Exception("Camera driver not restored");
            Finish(null);
        }
        catch (Exception e)
        {
            Finish(e);
        }
    }

    private static void Finish(Exception error)
    {
        EditorApplication.update -= Tick;
        p.training.StopTraining();
        p.training.strength.RestoreStrengthAfterTesting();
        p.training.GetComponent<GymEquipmentHub>().session.OpenMenu();
        Status = error == null ? "PASS" : "FAIL: " + error;
        File.WriteAllText("BlockBuildingBackup/TrainingPolishVerification.txt", Status
            + "\nNormal repetitions/s="
            + normalRate
            + "\nTapping repetitions/s="
            + fastRate
            + "\nCamera entry/return, hint timeout, local effort VFX and decay checked.");
        if (error != null)
            Debug.LogException(error);
    }
}
