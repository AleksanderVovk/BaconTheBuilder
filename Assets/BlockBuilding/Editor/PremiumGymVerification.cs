using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using YG;

public static class PremiumGymVerification
{
    private static GymEquipmentHub hub;
    private static SavesYG original;
    private static bool sdk;
    private static readonly FieldInfo sdkField = typeof(YG2).GetField("_SDKEnabled", BindingFlags.Static | BindingFlags.NonPublic);
    private static int index, lastFrame;
    private static System.Numerics.BigInteger before;
    private static double deadline;
    private static float repetition;
    private static StringBuilder report;
    public static string Status { get; private set; } = "Not run";

    public static void Begin()
    {
        hub = UnityEngine.Object.FindFirstObjectByType<GymEquipmentHub>();
        report = new StringBuilder();
        original = YG2.saves;
        sdk = YG2.isSDKEnabled;
        sdkField.SetValue(null, false);
        YG2.saves = JsonUtility.FromJson<SavesYG>(JsonUtility.ToJson(original));
        hub.session.Select(0);
        hub.training.strength.SetStrengthForTesting(52);
        repetition = hub.training.repetitionSeconds;
        hub.training.repetitionSeconds = 0.5f;
        YG2.saves.ownedGyms.Clear();
        Check(hub.Owns(hub.equipment[0]), "Standard is free");
        Check(!hub.Owns(hub.equipment[1])
            && !hub.CanBuy(hub.equipment[1]), "Paid equipment locked while catalog is unconfigured");
        YG2.onPurchaseSuccess?.Invoke("unrelated_product");
        Check(YG2.saves.ownedGyms.Count == 0, "Unknown product does not unlock gyms");
        foreach (var station in hub.equipment)
        {
            if (station.multiplier == 1)
                continue;
            YG2.onPurchaseSuccess?.Invoke(station.productId);
            YG2.onPurchaseSuccess?.Invoke(station.productId);
        }

        Check(YG2.saves.ownedGyms.Count == 5, "Successful callbacks grant five permanent unlocks once each");
        Check(
            JsonUtility.FromJson<SavesYG>(JsonUtility.ToJson(YG2.saves)).ownedGyms.Count == 5,
            "YG2 save JSON roundtrip preserves ownership"
        );
        index = 0;
        Prepare();
        EditorApplication.update += Tick;
        Status = "Testing six animated stations";
    }

    private static void Prepare()
    {
        hub.training.StopTraining();
        var station = hub.equipment[index];
        var p = hub.training.carrier.player;
        var cc = p.GetComponent<CharacterController>();
        cc.enabled = false;
        p.position = station.transform.position + new Vector3(0, 1, 0.9f);
        cc.enabled = true;
        hub.session.onPlayerReset.Invoke();
        hub.training.SelectEquipment(station);
        before = hub.training.strength.Strength;
        Check(hub.training.TryStartTraining(), "Training starts: " + station.title);
        deadline = EditorApplication.timeSinceStartup + 8;
        lastFrame = Time.frameCount;
    }

    private static void Tick()
    {
        try
        {
            if (Time.frameCount == lastFrame)
            {
                if (EditorApplication.timeSinceStartup > deadline)
                    throw new Exception("Player loop stalled");
                return;
            }

            if (hub.training.strength.Strength == before)
            {
                if (EditorApplication.timeSinceStartup > deadline)
                    throw new Exception("No repetition reward");
                return;
            }

            Check(hub.training.strength.Strength - before == hub.equipment[index].multiplier, "Completed repetition rewards x"
                + hub.equipment[index].multiplier);
            var ui = hub.session.runtime.GetComponent<MobileBuildInteraction>();
            Check(ui.interaction.GetComponent<RectTransform>().rect.width <= 340, "Compact interaction width");
            Check(ui.interaction.GetComponent<RectTransform>().anchoredPosition.y < 0, "Stop action is below the player");
            hub.training.StopTraining();
            index++;
            if (index < 6)
                Prepare();
            else
                Finish(null);
        }
        catch (Exception e)
        {
            Finish(e);
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
        report.AppendLine("PASS: " + message);
    }

    private static void Finish(Exception error)
    {
        EditorApplication.update -= Tick;
        hub.training.StopTraining();
        hub.training.repetitionSeconds = repetition;
        hub.training.strength.RestoreStrengthAfterTesting();
        YG2.saves = original;
        sdkField.SetValue(null, sdk);
        hub.session.Select(0);
        hub.session.OpenMenu();
        Status = error == null ? "PASS" : "FAIL: " + error;
        File.WriteAllText("BlockBuildingBackup/PremiumGymVerification.txt", report + "\n" + Status);
        if (error != null)
            Debug.LogException(error);
    }
}
