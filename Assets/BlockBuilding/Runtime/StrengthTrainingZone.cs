using System;
using UnityEngine;

public class StrengthTrainingZone : MonoBehaviour
{
    [Header("Player"), Tooltip("Existing carrier. Training starts with empty hands.")]
    public PlayerBlockCarrier carrier;

    [Header("Progression"), Tooltip("Receives strength after each complete repetition.")]
    public StrengthUpgradeSystem strength;

    [Header("Controls"), Tooltip("Existing movement components paused during training and restored on exit.")]
    public Behaviour[] movementToPause = new Behaviour[0];

    [Header("Pose"), Tooltip("Visual child model only; never assign the player root.")]
    public Transform visualModel;

    [Header("Animator"), Tooltip("Humanoid animator paused while the procedural training pose is active.")]
    public Animator modelAnimator;

    [Header("Bench"), Tooltip("Anchor for the lying pose.")]
    public Transform poseAnchor;

    [Header("Bar"), Tooltip("Animated barbell. Its authored position is restored on exit.")]
    public Transform barbell;

    [Header("Arms"), Tooltip("Solve humanoid arms with separate outward/downward elbow hints.")]
    public bool animateArms = true;

    [Header("Fit the lift"), Tooltip("Limit bar height/travel to what both arms can reach without locking elbows.")]
    public bool fitBarToArms = true;

    [Header("Grip"), Tooltip("Distance from bar center to each wrist, in world units."), Range(0.1f, 0.8f)]
    public float gripHalfWidth = 0.38f;

    [Header("Palm offset"), Tooltip("Wrist-to-palm offset towards the chest normal. Keeps the bar inside the fists, above the wrist pivots."), Range(0, 0.15f)]
    public float palmOffset = 0.08f;

    [Header("Elbows outward"), Tooltip("Outward elbow hint offset from each shoulder."), Range(0.1f, 1)]
    public float elbowSpread = 0.55f;

    [Header("Elbows downward"), Tooltip("Hint offset towards the bench, opposite the chest normal."), Range(0, 0.5f)]
    public float elbowDrop = 0.18f;

    [Header("Elbows towards feet"), Tooltip("Small footward offset keeps the bend plane stable."), Range(0, 0.5f)]
    public float elbowFootOffset = 0.12f;

    [Header("Chest clearance"), Tooltip("Minimum grip height above the shoulder line when fitting travel."), Range(0.05f, 0.4f)]
    public float chestClearance = 0.22f;

    [Header("Maximum reach"), Tooltip("Fraction of total arm length at the top. Below 1 avoids hyperextension."), Range(0.8f, 0.98f)]
    public float maximumReach = 0.94f;

    [Header("Pose transition"), Tooltip("Seconds to smoothly lie down and bring hands to the bar."), Range(0.1f, 1.5f)]
    public float poseBlendSeconds = 0.45f;

    [Header("Range"), Tooltip("Horizontal distance to start training."), Min(0.2f)]
    public float radius = 2;

    [Header("Height"), Tooltip("Vertical tolerance for starting training."), Min(0.2f)]
    public float heightTolerance = 3;

    [Header("Input"), Tooltip("Old Input Manager training toggle.")]
    public KeyCode trainingKey = KeyCode.F;

    [Header("Repetition"), Tooltip("Seconds per complete lower-and-press repetition."), Min(0.5f)]
    public float repetitionSeconds = 1.6f;

    [Header("Reward"), Tooltip("Strength per complete repetition."), Min(1)]
    public int strengthPerRep = 1;

    [Header("Bar movement"), Tooltip("Maximum travel. Auto fit may shorten it for short arms."), Range(0.05f, 1)]
    public float barTravel = 0.4f;

    private bool initialized, previousAnimator;
    private bool[] previousEnabled;
    private float phase, elapsed, leftSide = -1;
    private Vector3 barStart, savedModelPosition, entryModelPosition, entryBarPosition, barTop, barBottom;
    private Quaternion savedModelRotation, entryModelRotation;
    private BenchPressArmIK leftArm, rightArm;
    private BenchPressLegPose legPose;
    public GymEquipment Equipment { get; private set; }

    public Vector3 InteractionPosition
    {
        get
        {
            return Equipment != null ? Equipment.transform.position : transform.position;
        }
    }

    public void SelectEquipment(GymEquipment station)
    {
        if (IsTraining || station == null)
            return;
        Equipment = station;
        poseAnchor = station.pose;
        barbell = station.bar;
        initialized = false;
        Initialize();
        Changed?.Invoke();
    }

    public event Action Changed;
    public event Action<System.Numerics.BigInteger> StrengthEarned;
    public bool IsTraining { get; private set; }
    public float RepetitionSpeed { get; set; } = 1;

    public float NormalizedPhase
    {
        get
        {
            return phase;
        }
    }

    // Imported FBX mesh axes are not the anatomical grip frame.
    private Vector3 GripAxis
    {
        get
        {
            return Equipment != null ? Equipment.transform.right.normalized : transform.right.normalized;
        }
    }

    public Vector3 LeftGripPosition
    {
        get
        {
            return barbell != null ? barbell.position + GripAxis * (leftSide * gripHalfWidth) : transform.position;
        }
    }

    public Vector3 RightGripPosition
    {
        get
        {
            return barbell != null ? barbell.position - GripAxis * (leftSide * gripHalfWidth) : transform.position;
        }
    }

    public Vector3 LeftWristTargetPosition
    {
        get
        {
            return LeftGripPosition - (visualModel != null ? visualModel.forward : transform.up) * palmOffset;
        }
    }

    public Vector3 RightWristTargetPosition
    {
        get
        {
            return RightGripPosition - (visualModel != null ? visualModel.forward : transform.up) * palmOffset;
        }
    }

    public bool IsInRange
    {
        get
        {
            if (carrier == null || carrier.player == null)
                return false;
            Vector3 d = carrier.player.position - InteractionPosition;
            return Mathf.Abs(d.y) <= heightTolerance && d.x * d.x + d.z * d.z <= radius * radius;
        }
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized)
            return;
        previousEnabled = new bool[movementToPause.Length];
        if (barbell != null)
            barStart = barbell.localPosition;
        if (modelAnimator != null && modelAnimator.isHuman)
        {
            if (legPose == null)
                legPose = new BenchPressLegPose(modelAnimator);
            leftArm = new BenchPressArmIK(
                modelAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm),
                modelAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm),
                modelAnimator.GetBoneTransform(HumanBodyBones.LeftHand)
            );
            rightArm = new BenchPressArmIK(
                modelAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm),
                modelAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm),
                modelAnimator.GetBoneTransform(HumanBodyBones.RightHand)
            );
        }

        initialized = true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(trainingKey))
            ToggleTraining();
        if (!IsTraining)
            return;
        if (!IsInRange)
        {
            StopTraining();
            return;
        }

        float before = elapsed;
        elapsed += Time.deltaTime;
        float activeDelta = Mathf.Max(0, elapsed - Mathf.Max(before, poseBlendSeconds));
        phase += activeDelta * Mathf.Clamp(RepetitionSpeed, 1, 4) / Mathf.Max(0.5f, repetitionSeconds);
        if (phase >= 1)
        {
            int reps = Mathf.FloorToInt(phase);
            phase -= reps;
            var beforeReward = strength.Strength;
            strength.AddStrength((System.Numerics.BigInteger)reps * Mathf.Max(1, strengthPerRep) * Mathf.Max(1, Equipment != null
                ? Equipment.multiplier
                : 1));
            var earned = strength.Strength - beforeReward;
            if (earned > 0)
                StrengthEarned?.Invoke(earned);
        }
    }

    private void LateUpdate()
    {
        if (!IsTraining)
            return;
        float weight = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / Mathf.Max(0.1f, poseBlendSeconds)));
        if (visualModel != null && poseAnchor != null)
        {
            visualModel.position = Vector3.Lerp(entryModelPosition, poseAnchor.position, weight);
            visualModel.rotation = Quaternion.Slerp(entryModelRotation, poseAnchor.rotation, weight);
        }

        if (legPose != null)
            legPose.Apply(weight);
        if (barbell == null)
            return;
        float lower = 0.5f - 0.5f * Mathf.Cos(phase * 2 * Mathf.PI);
        Vector3 press = Vector3.Lerp(barTop, barBottom, lower);
        if (Equipment != null && weight < 1)
        {
            Vector3 lifted = entryBarPosition + transform.up * Equipment.unrackLift;
            barbell.position = weight < .35f
                ? Vector3.Lerp(entryBarPosition, lifted, weight / .35f)
                : Vector3.Lerp(lifted, press, (weight - .35f) / .65f);
        }
        else
            barbell.position = Vector3.Lerp(entryBarPosition, press, weight);
        if (!animateArms
            || visualModel == null
            || leftArm == null
            || rightArm == null
            || !leftArm.IsValid
            || !rightArm.IsValid)
            return;
        Vector3 common = -visualModel.forward * elbowDrop - visualModel.up * elbowFootOffset;
        Vector3 leftHint = leftArm.Shoulder.position + GripAxis * (leftSide * elbowSpread) + common;
        Vector3 rightHint = rightArm.Shoulder.position - GripAxis * (leftSide * elbowSpread) + common;
        leftArm.Solve(LeftWristTargetPosition, leftHint, weight);
        rightArm.Solve(RightWristTargetPosition, rightHint, weight);
    }

    private void FitTrajectory()
    {
        if (barbell == null)
            return;
        Vector3 workingBar = entryBarPosition + (Equipment != null
            ? Equipment.transform.TransformDirection(Equipment.pressOffset)
            : Vector3.zero);
        Vector3 axis = transform.up.normalized;
        barTop = workingBar + axis * barTravel;
        barBottom = workingBar;
        if (!fitBarToArms
            || !animateArms
            || visualModel == null
            || poseAnchor == null
            || leftArm == null
            || rightArm == null
            || !leftArm.IsValid
            || !rightArm.IsValid)
            return;
        Matrix4x4 goal = Matrix4x4.TRS(poseAnchor.position, poseAnchor.rotation, visualModel.lossyScale);
        Vector3 leftShoulder = goal.MultiplyPoint3x4(visualModel.InverseTransformPoint(leftArm.Shoulder.position));
        Vector3 rightShoulder = goal.MultiplyPoint3x4(visualModel.InverseTransformPoint(rightArm.Shoulder.position));
        leftSide = Vector3.Dot(leftShoulder - rightShoulder, GripAxis) >= 0 ? 1 : -1;
        float leftLimit = ReachLimit(
            leftShoulder,
            leftArm.Length,
            workingBar + GripAxis * (leftSide * gripHalfWidth) - poseAnchor.forward * palmOffset,
            axis
        );
        float rightLimit = ReachLimit(
            rightShoulder,
            rightArm.Length,
            workingBar - GripAxis * (leftSide * gripHalfWidth) - poseAnchor.forward * palmOffset,
            axis
        );
        barTop = workingBar + axis * Mathf.Min(leftLimit, rightLimit);
        float shoulderHeight = Vector3.Dot((leftShoulder + rightShoulder) * 0.5f, axis);
        float travel = Mathf.Min(
            barTravel,
            Mathf.Max(0.02f, Vector3.Dot(barTop, axis) - shoulderHeight - chestClearance - palmOffset)
        );
        barBottom = barTop - axis * travel;
    }

    private float ReachLimit(Vector3 shoulder, float length, Vector3 grip, Vector3 axis)
    {
        Vector3 d = grip - shoulder;
        float along = Vector3.Dot(d, axis);
        float radius = length * Mathf.Clamp(maximumReach, 0.8f, 0.98f);
        float sideSquared = Mathf.Max(0, d.sqrMagnitude - along * along);
        return Mathf.Sqrt(Mathf.Max(0.0001f, radius * radius - sideSquared)) - along;
    }

    public void ToggleTraining()
    {
        if (IsTraining)
            StopTraining();
        else
            TryStartTraining();
    }

    public bool TryStartTraining()
    {
        Initialize();
        if (Equipment != null
            && Equipment.multiplier > 1
            && (GetComponent<GymEquipmentHub>() == null
                || !GetComponent<GymEquipmentHub>().Owns(Equipment)))
            return false;
        if (IsTraining || !IsInRange || strength == null || carrier.Count != 0 || carrier.IsTraining)
            return false;
        if (previousEnabled.Length != movementToPause.Length)
            previousEnabled = new bool[movementToPause.Length];
        if (modelAnimator != null)
            previousAnimator = modelAnimator.enabled;
        for (int i = 0; i < movementToPause.Length; i++)
        {
            var component = movementToPause[i];
            previousEnabled[i] = component != null && component.enabled;
            if (CanPause(component))
                component.enabled = false;
        }

        if (visualModel != null)
        {
            savedModelPosition = visualModel.localPosition;
            savedModelRotation = visualModel.localRotation;
            entryModelPosition = visualModel.position;
            entryModelRotation = visualModel.rotation;
        }

        if (legPose != null)
            legPose.CaptureEntry();
        if (leftArm != null)
            leftArm.CapturePose();
        if (rightArm != null)
            rightArm.CapturePose();
        if (modelAnimator != null)
            modelAnimator.enabled = false;
        if (barbell != null)
            entryBarPosition = barbell.position;
        if (legPose != null)
            legPose.Apply(1);
        FitTrajectory();
        if (legPose != null)
            legPose.Apply(0);
        phase = 0;
        elapsed = 0;
        IsTraining = true;
        carrier.IsTraining = true;
        Changed?.Invoke();
        return true;
    }

    private bool CanPause(Behaviour component)
    {
        return component != null
            && component != this
            && component != carrier
            && component != strength
            && component != modelAnimator;
    }

    public void StopTraining()
    {
        if (!IsTraining)
            return;
        IsTraining = false;
        if (carrier != null)
            carrier.IsTraining = false;
        if (visualModel != null)
        {
            visualModel.localPosition = savedModelPosition;
            visualModel.localRotation = savedModelRotation;
        }

        if (legPose != null)
            legPose.Restore();
        if (leftArm != null)
            leftArm.RestorePose();
        if (rightArm != null)
            rightArm.RestorePose();
        if (modelAnimator != null)
            modelAnimator.enabled = previousAnimator;
        for (int i = 0; i < movementToPause.Length; i++)
            if (CanPause(movementToPause[i]))
                movementToPause[i].enabled = previousEnabled[i];
        phase = 0;
        elapsed = 0;
        RepetitionSpeed = 1;
        if (barbell != null)
            barbell.localPosition = barStart;
        Changed?.Invoke();
    }

    private void OnDisable()
    {
        StopTraining();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
