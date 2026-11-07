using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using TMPro;
using BigInteger = System.Numerics.BigInteger;

public static class ActivityChestBigNumberRegression
{
    private static void Check(bool ok, string message)
    {
        if (!ok)
            throw new InvalidOperationException(message);
    }

    private static object Call(object target, string method, params object[] args)
    {
        return target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    }

    private static void Set(object target, string field, object value)
    {
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }

    public static string Run()
    {
        if (!EditorApplication.isPlaying)
            throw new InvalidOperationException("Run in Play Mode.");
        var root = new GameObject("ChestBigNumberRegression");
        root.SetActive(false);
        root.transform.position = new Vector3(10000, 10000, 10000);
        string key = "BlockBuilding.QA.Chest." + Guid.NewGuid().ToString("N");
        var oldSaves = YG.YG2.saves;
        var sdk = typeof(YG.YG2).GetField("_SDKEnabled", BindingFlags.Static | BindingFlags.NonPublic);
        bool sdkBefore = YG.YG2.isSDKEnabled;
        try
        {
            sdk.SetValue(null, false);
            YG.YG2.saves = new YG.SavesYG();
            var strength = root.AddComponent<StrengthUpgradeSystem>();
            strength.saveKey = key + ".strength";
            strength.Initialize();
            var carrier = root.AddComponent<PlayerBlockCarrier>();
            carrier.player = root.transform;
            var runtime = root.AddComponent<BlockLevelRuntime>();
            runtime.autoLoadOnStart = false;
            runtime.carrier = carrier;
            var session = root.AddComponent<BuildLevelSession>();
            session.runtime = runtime;
            typeof(BuildLevelSession).GetProperty("MenuOpen").SetValue(session, false);
            PlayerPrefs.SetString(key, "{\"progress\":99,\"pending\":2147483647,\"revision\":9223372036854775807}");
            var chest = root.AddComponent<ActivityChestReward>();
            chest.saveKey = key;
            chest.session = session;
            chest.strength = strength;
            Check(chest.Pending == int.MaxValue && chest.Progress == 99, "Legacy chest save migration failed.");
            chest.RecordBlock();
            Check(chest.Pending == (BigInteger)int.MaxValue + 1 && chest.Progress == 0, "Pending gifts overflowed.");
            BigInteger huge = BigInteger.Pow(10, 10000);
            strength.ApplySavedStrength(huge);
            var ui = root.AddComponent<ActivityChestInteraction>();
            ui.reward = chest;
            var toastGO = new GameObject("Toast", typeof(RectTransform), typeof(CanvasGroup));
            toastGO.transform.SetParent(root.transform, false);
            ui.toast = toastGO.GetComponent<CanvasGroup>();
            var textGO = new GameObject("ToastText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(toastGO.transform, false);
            ui.toastText = textGO.GetComponent<TMP_Text>();
            var animation = root.AddComponent<ActivityChestAnimation>();
            animation.reward = chest;
            var lid = new GameObject("Lid");
            lid.transform.SetParent(root.transform, false);
            animation.lid = lid.transform;
            Call(animation, "Start");
            BigInteger notified = -1;
            int events = 0;
            chest.Claimed += amount =>
            {
                notified = amount;
                events++;
                Call(ui, "ShowReward", amount);
            };
            var buttonGO = new GameObject("ClaimButton", typeof(RectTransform), typeof(UnityEngine.UI.Button));
            buttonGO.transform.SetParent(root.transform, false);
            var button = buttonGO.GetComponent<UnityEngine.UI.Button>();
            bool claimed = false;
            button.onClick.AddListener(() => claimed = chest.TryClaim());
            button.onClick.Invoke();
            Check(claimed && strength.Strength == huge + 50, "Full claim failed above the int limit.");
            Check(chest.Pending == int.MaxValue && notified == 50 && events == 1, "Reward/event/pending count incorrect.");
            Check(ui.toastText.text.Contains("+50")
                && ui.toastText.text.Length < 80, "Huge reward notification is not compact.");
            Set(animation, "claimedAt", Time.unscaledTime - .3f);
            Call(animation, "Update");
            Check(
                Quaternion.Angle(animation.lid.localRotation, Quaternion.identity) > 1,
                "Chest animation did not react to the huge reward."
            );
            Check(!chest.TryClaim()
                && chest.Pending == int.MaxValue
                && events == 1, "Double click claimed twice during cooldown.");
            Set(chest, "openedUntil", -1f);
            BigInteger before = strength.Strength;
            Check(chest.TryClaim(), "Second claim failed.");
            Check(strength.Strength == before + 50 && chest.Pending == int.MaxValue - 1, "Second reward incorrect.");
            string saved = PlayerPrefs.GetString(key);
            Check(saved.Contains("pendingBig") && saved.Contains("revisionBig"), "Exact chest counters were not saved.");
            var reloadGO = new GameObject("ReloadedChest");
            reloadGO.transform.SetParent(root.transform, false);
            var reloaded = reloadGO.AddComponent<ActivityChestReward>();
            reloaded.saveKey = key;
            Check(reloaded.Pending == chest.Pending
                && reloaded.Progress == 0
                && reloaded.Openings == 2, "Reload lost pending gifts.");
            var restoredStrengthGO = new GameObject("ReloadedStrength");
            restoredStrengthGO.transform.SetParent(root.transform, false);
            var restoredStrength = restoredStrengthGO.AddComponent<StrengthUpgradeSystem>();
            restoredStrength.saveKey = strength.saveKey;
            restoredStrength.Initialize();
            Check(restoredStrength.Strength == strength.Strength, "Claimed strength did not survive reload.");
            BigInteger bank = BigInteger.Pow(10, 40), revision = BigInteger.Pow(10, 60);
            YG.YG2.saves.activityChestJson = "{\"progress\":17,\"pendingBig\":\""
                + bank
                + "\",\"revisionBig\":\""
                + revision
                + "\"}";
            Call(chest, "SyncCloud");
            Check(chest.Pending == bank && chest.Progress == 17, "Large cloud counters did not load.");
            YG.YG2.saves.activityChestJson = "{\"pending\":0,\"revision\":1}";
            Call(chest, "SyncCloud");
            Check(chest.Pending == bank, "Older cloud save replaced newer chest state.");
            return "PASS: real button -> TryClaim -> fixed +50 above strength 10^10000 -> strength save -> popup and lid animation; double-click guard; repeat opening; int.MaxValue pending and long.MaxValue revision migration; exact local reload; huge cloud counters and stale-save rejection.";
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            YG.YG2.saves = oldSaves;
            sdk.SetValue(null, sdkBefore);
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.DeleteKey(key + ".strength");
            PlayerPrefs.DeleteKey(key + ".strength.Big.v1");
            PlayerPrefs.Save();
        }
    }
}
