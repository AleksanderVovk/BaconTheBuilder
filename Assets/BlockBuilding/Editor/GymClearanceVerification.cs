using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

public static class GymClearanceVerification
{
    [Serializable]
    public class Sample
    {
        public string station;
        public float y, z;
    }

    [Serializable]
    public class Samples
    {
        public List<Sample> poses = new List<Sample>();
    }

    public static string Run()
    {
        var hub = UnityEngine.Object.FindFirstObjectByType<GymEquipmentHub>();
        var t = hub.training;
        var samples = new Samples();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        t.StopTraining();
        hub.session.Select(0);
        t.strength.SetStrengthForTesting(t.strength.Strength);
        try
        {
            string[] ids =
            {
                "Standard",
                "Industrial",
                "Neon",
                "Frost",
                "Royal",
                "Cosmic"
            };
            for (int n = 0; n < hub.equipment.Length; n++)
            {
                var s = hub.equipment[n];
                var cc = t.carrier.player.GetComponent<CharacterController>();
                cc.enabled = false;
                cc.transform.position = s.transform.position + new Vector3(0, 1, .9f);
                cc.enabled = true;
                hub.session.onPlayerReset.Invoke();
                t.SelectEquipment(s);
                if (!t.TryStartTraining())
                    throw new Exception("Cannot start " + s.title);
                for (int i = 0; i <= 64; i++)
                {
                    float elapsed = i <= 32 ? i * t.poseBlendSeconds / 32 : 2;
                    float phase = i <= 32 ? 0 : (i - 33) / 31f;
                    typeof(StrengthTrainingZone).GetField("elapsed", flags).SetValue(t, elapsed);
                    typeof(StrengthTrainingZone).GetField("phase", flags).SetValue(t, phase);
                    typeof(StrengthTrainingZone).GetMethod("LateUpdate", flags).Invoke(t, null);
                    var p = s.transform.InverseTransformPoint(s.bar.position);
                    samples.poses.Add(new Sample { station = ids[n], y = p.y, z = p.z });
                }

                t.StopTraining();
            }
        }
        finally
        {
            t.StopTraining();
            t.strength.RestoreStrengthAfterTesting();
            t.SelectEquipment(hub.equipment[0]);
            hub.session.OpenMenu();
        }

        File.WriteAllText("BlockBuildingBackup/GymClearancePoses.json", JsonUtility.ToJson(samples, true));
        return "Recorded "
            + samples.poses.Count
            + " model poses (unrack transition and full repetition on all six stations).";
    }
}
