using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

public static class GymGripVerification
{
    public static string Run()
    {
        var hub = UnityEngine.Object.FindFirstObjectByType<GymEquipmentHub>();
        var t = hub.training;
        var report = new StringBuilder();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        t.StopTraining();
        hub.session.Select(0);
        t.strength.SetStrengthForTesting(t.strength.Strength);
        try
        {
            foreach (var station in hub.equipment)
            {
                var cc = t.carrier.player.GetComponent<CharacterController>();
                cc.enabled = false;
                cc.transform.position = station.transform.position + new Vector3(0, 1, 0.9f);
                cc.enabled = true;
                hub.session.onPlayerReset.Invoke();
                t.SelectEquipment(station);
                if (!t.TryStartTraining())
                    throw new Exception("Cannot start " + station.title);
                Vector3 start = t.barbell.position;
                float maxError = 0;
                float upper = 0, lower = 0;
                for (int i = 0; i < 5; i++)
                {
                    typeof(StrengthTrainingZone).GetField("elapsed", flags).SetValue(t, 2f);
                    typeof(StrengthTrainingZone).GetField("phase", flags).SetValue(t, i * 0.25f);
                    typeof(StrengthTrainingZone).GetMethod("LateUpdate", flags).Invoke(t, null);
                    var lh = t.modelAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
                    var rh = t.modelAnimator.GetBoneTransform(HumanBodyBones.RightHand);
                    float error = Mathf.Max(
                        Vector3.Distance(lh.position, t.LeftWristTargetPosition),
                        Vector3.Distance(rh.position, t.RightWristTargetPosition)
                    );
                    maxError = Mathf.Max(error, maxError);
                    if (error > 0.025f)
                        throw new Exception(station.title + " wrist misses grip by " + error);
                    if (Mathf.Abs(Vector3.Dot(lh.position - rh.position, station.transform.right)) < 0.7f)
                        throw new Exception(station.title + " hands not horizontal");
                    Vector3 working = start + station.transform.TransformDirection(station.pressOffset);
                    if (Mathf.Abs(t.barbell.position.x - working.x) > 0.001f
                        || Mathf.Abs(t.barbell.position.z - working.z) > 0.001f)
                        throw new Exception(station.title + " bar moves sideways");
                    if (i == 0)
                        upper = t.barbell.position.y;
                    if (i == 2)
                        lower = t.barbell.position.y;
                }

                if (upper - lower < 0.02f)
                    throw new Exception(station.title + " no vertical press travel");
                t.StopTraining();
                if (Vector3.Distance(start, t.barbell.position) > 0.001f)
                    throw new Exception(station.title + " bar not restored");
                report.AppendLine("PASS "
                    + station.title
                    + ": 5 phases, max wrist error="
                    + maxError.ToString("F5")
                    + ", vertical travel=" + (upper - lower).ToString("F3"));
            }
        }
        finally
        {
            t.StopTraining();
            t.strength.RestoreStrengthAfterTesting();
            hub.session.Select(0);
            t.SelectEquipment(hub.equipment[0]);
            hub.session.OpenMenu();
        }

        File.WriteAllText("BlockBuildingBackup/GymGripVerification.txt", report.ToString());
        return report.ToString();
    }
}
