using UnityEngine;

// Pelvis, torso and legs use the avatar reference pose; the existing controller and Animator are restored on exit.
public sealed class BenchPressLegPose
{
    private readonly Transform[] bones;
    private readonly Quaternion[] neutral, entry;
    private Vector3 neutralHipPosition, entryHipPosition;
    public BenchPressLegPose(Animator animator)
    {
        HumanBodyBones[] ids =
        {
            HumanBodyBones.Hips,
            HumanBodyBones.Spine,
            HumanBodyBones.Chest,
            HumanBodyBones.UpperChest,
            HumanBodyBones.Neck,
            HumanBodyBones.Head,
            HumanBodyBones.LeftUpperLeg,
            HumanBodyBones.LeftLowerLeg,
            HumanBodyBones.LeftFoot,
            HumanBodyBones.LeftToes,
            HumanBodyBones.RightUpperLeg,
            HumanBodyBones.RightLowerLeg,
            HumanBodyBones.RightFoot,
            HumanBodyBones.RightToes
        };
        bones = new Transform[ids.Length];
        neutral = new Quaternion[ids.Length];
        entry = new Quaternion[ids.Length];
        var skeleton = animator.avatar.humanDescription.skeleton;
        for (int i = 0; i < ids.Length; i++)
        {
            bones[i] = animator.GetBoneTransform(ids[i]);
            if (bones[i] == null)
                continue;
            neutral[i] = bones[i].localRotation;
            for (int j = 0; j < skeleton.Length; j++)
                if (skeleton[j].name == bones[i].name)
                {
                    neutral[i] = skeleton[j].rotation;
                    if (i == 0)
                        neutralHipPosition = skeleton[j].position;
                    break;
                }
        }
    }

    public void CaptureEntry()
    {
        if (bones[0] != null)
            entryHipPosition = bones[0].localPosition;
        for (int i = 0; i < bones.Length; i++)
            if (bones[i] != null)
                entry[i] = bones[i].localRotation;
    }

    public void Apply(float blend)
    {
        if (bones[0] != null)
            bones[0].localPosition = Vector3.Lerp(entryHipPosition, neutralHipPosition, blend);
        for (int i = 0; i < bones.Length; i++)
            if (bones[i] != null)
                bones[i].localRotation = Quaternion.Slerp(entry[i], neutral[i], blend);
    }

    public void Restore()
    {
        if (bones[0] != null)
            bones[0].localPosition = entryHipPosition;
        for (int i = 0; i < bones.Length; i++)
            if (bones[i] != null)
                bones[i].localRotation = entry[i];
    }
}
