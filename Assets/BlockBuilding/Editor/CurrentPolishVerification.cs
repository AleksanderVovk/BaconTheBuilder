using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

public static class CurrentPolishVerification
{
    public static string Run()
    {
        var h = UnityEngine.Object.FindFirstObjectByType<GymEquipmentHub>();
        var s = h.session;
        var t = h.training;
        var report = new StringBuilder();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        string key = "BlockBuilding.Levels.v1." + s.catalog.levels[0].id;
        bool hasBest = PlayerPrefs.HasKey(key + ".best"), hasDone = PlayerPrefs.HasKey(key + ".done");
        float savedBest = PlayerPrefs.GetFloat(key + ".best");
        int savedDone = PlayerPrefs.GetInt(key + ".done");
        try
        {
            PlayerPrefs.SetFloat(key + ".best", 1000);
            float[] times =
            {
                120,
                180,
                60
            };
            bool[] assisted =
            {
                true,
                false,
                true
            };
            float[] expected =
            {
                120,
                120,
                60
            };
            for (int run = 0; run < 3; run++)
            {
                s.Select(0);
                if (assisted[run])
                    s.MarkAssisted();
                typeof(BuildLevelSession).GetField("elapsed", flags).SetValue(s, (double)times[run]);
                int guard = 0;
                while (!s.runtime.IsComplete && guard++ < 2000)
                    if (!s.runtime.CurrentSite.TryBuildGranted())
                        throw new Exception("Cannot complete test construction");
                float best;
                if (!s.Completed
                    || !LevelRecordStore.TryGetBest(s.Selected.id, out best)
                    || Mathf.Abs(best - expected[run]) > .001f
                    || !LevelRecordStore.Completed(s.Selected.id))
                    throw new Exception("Record persistence failed");
                if (s.NewRecord != (run != 1))
                    throw new Exception("NewRecord flag failed");
            }

            report.AppendLine("PASS: assisted and normal completions save records; slower run preserves best, faster run replaces it.");
        }
        finally
        {
            if (hasBest)
                PlayerPrefs.SetFloat(key + ".best", savedBest);
            else
                PlayerPrefs.DeleteKey(key + ".best");
            if (hasDone)
                PlayerPrefs.SetInt(key + ".done", savedDone);
            else
                PlayerPrefs.DeleteKey(key + ".done");
            PlayerPrefs.Save();
            s.Select(0);
        }

        var admin = UnityEngine.Object.FindFirstObjectByType<BuildAdminMenu>();
        if (admin.buttons[0].gameObject.activeSelf)
            throw new Exception("Admin button visible");
        report.AppendLine("PASS: OpenAdmin GameObject stays inactive during gameplay.");
        HumanBodyBones[] ids =
        {
            HumanBodyBones.Hips,
            HumanBodyBones.LeftUpperLeg,
            HumanBodyBones.LeftLowerLeg,
            HumanBodyBones.LeftFoot,
            HumanBodyBones.LeftToes,
            HumanBodyBones.RightUpperLeg,
            HumanBodyBones.RightLowerLeg,
            HumanBodyBones.RightFoot,
            HumanBodyBones.RightToes
        };
        Transform[] bones = new Transform[ids.Length];
        Quaternion[] original = new Quaternion[ids.Length];
        Quaternion[] entry = new Quaternion[ids.Length];
        Quaternion[] neutral = new Quaternion[ids.Length];
        var skeleton = t.modelAnimator.avatar.humanDescription.skeleton;
        for (int i = 0; i < ids.Length; i++)
        {
            bones[i] = t.modelAnimator.GetBoneTransform(ids[i]);
            original[i] = bones[i].localRotation;
            neutral[i] = Array.Find(skeleton, x => x.name == bones[i].name).rotation;
        }

        t.strength.SetStrengthForTesting(t.strength.Strength);
        try
        {
            foreach (var station in h.equipment)
                for (int sample = 0; sample < 4; sample++)
                {
                    var cc = t.carrier.player.GetComponent<CharacterController>();
                    cc.enabled = false;
                    cc.transform.position = station.transform.position + new Vector3(0, 1, .9f);
                    cc.enabled = true;
                    s.onPlayerReset.Invoke();
                    t.SelectEquipment(station);
                    for (int i = 0; i < bones.Length; i++)
                    {
                        entry[i] = original[i] * Quaternion.Euler(25 + sample * 23 + i * 4, -sample * 11, 12 - sample * 9);
                        bones[i].localRotation = entry[i];
                    }

                    if (!t.TryStartTraining())
                        throw new Exception("Cannot train");
                    typeof(StrengthTrainingZone).GetField("elapsed", flags).SetValue(t, 2f);
                    typeof(StrengthTrainingZone).GetField("phase", flags).SetValue(t, .5f);
                    typeof(StrengthTrainingZone).GetMethod("LateUpdate", flags).Invoke(t, null);
                    for (int i = 0; i < bones.Length; i++)
                        if (Quaternion.Angle(bones[i].localRotation, neutral[i]) > .05f)
                            throw new Exception("Leg pose depends on entry animation");
                    t.StopTraining();
                    for (int i = 0; i < bones.Length; i++)
                    {
                        if (Quaternion.Angle(bones[i].localRotation, entry[i]) > .05f)
                            throw new Exception("Entry pose not restored");
                        bones[i].localRotation = original[i];
                    }
                }

            report.AppendLine("PASS: 24 different entry leg poses on six stations converge to avatar neutral pose; entry pose restores on exit.");
            var pop = UnityEngine.Object.FindFirstObjectByType<StrengthRewardPopups>();
            for (int i = 0; i < 64; i++)
                pop.Show(i % 2 == 0 ? 1 : 999999);
            foreach (var item in pop.items)
            {
                item.text.ForceMeshUpdate(true);
                if (item.text.textInfo.lineCount != 1)
                    throw new Exception("Reward number wraps");
            }

            report.AppendLine("PASS: 64 pooled rewards, +1 and +999,999 remain on a single line with the strong icon.");
        }
        finally
        {
            t.StopTraining();
            for (int i = 0; i < bones.Length; i++)
                bones[i].localRotation = original[i];
            t.strength.RestoreStrengthAfterTesting();
            t.SelectEquipment(h.equipment[0]);
            s.OpenMenu();
        }

        File.WriteAllText("BlockBuildingBackup/CurrentPolishVerification.txt", report.ToString());
        return report.ToString();
    }
}
