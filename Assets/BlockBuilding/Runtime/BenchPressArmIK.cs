using UnityEngine;

// Two-link solver with an explicit body-relative elbow hint. Pose is reset before
// solving each frame so repeated reps cannot accumulate forearm/shoulder twist.
public sealed class BenchPressArmIK
{
    private readonly Transform upper, lower, hand;
    private Quaternion upperPose, lowerPose, handPose;
    private float upperLength, lowerLength;
    private Vector3 previousBend;
    public bool IsValid
    {
        get
        {
            return upper != null && lower != null && hand != null && upperLength > 0.001f && lowerLength > 0.001f;
        }
    }

    public Transform Shoulder
    {
        get
        {
            return upper;
        }
    }

    public float Length
    {
        get
        {
            return upperLength + lowerLength;
        }
    }

    public BenchPressArmIK(Transform shoulder, Transform elbow, Transform wrist)
    {
        upper = shoulder;
        lower = elbow;
        hand = wrist;
        CapturePose();
    }

    public void CapturePose()
    {
        if (upper == null || lower == null || hand == null)
            return;
        upperPose = upper.localRotation;
        lowerPose = lower.localRotation;
        handPose = hand.localRotation;
        upperLength = Vector3.Distance(upper.position, lower.position);
        lowerLength = Vector3.Distance(lower.position, hand.position);
        previousBend = Vector3.zero;
    }

    public void RestorePose()
    {
        if (upper != null)
            upper.localRotation = upperPose;
        if (lower != null)
            lower.localRotation = lowerPose;
        if (hand != null)
            hand.localRotation = handPose;
    }

    public void Solve(Vector3 target, Vector3 elbowHint, float weight)
    {
        if (!IsValid)
            return;
        RestorePose();
        Vector3 delta = target - upper.position;
        if (delta.sqrMagnitude < 0.000001f)
            return;
        Vector3 direction = delta.normalized;
        float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(upperLength - lowerLength) + 0.001f, Length * 0.995f);
        Vector3 bend = Vector3.ProjectOnPlane(elbowHint - upper.position, direction);
        if (bend.sqrMagnitude < 0.000001f)
            bend = Vector3.ProjectOnPlane(previousBend, direction);
        if (bend.sqrMagnitude < 0.000001f)
            bend = Vector3.ProjectOnPlane(lower.position - upper.position, direction);
        if (bend.sqrMagnitude < 0.000001f)
            bend = Vector3.Cross(direction, Mathf.Abs(direction.y) < 0.95f ? Vector3.up : Vector3.right);
        bend.Normalize();
        previousBend = bend;
        float along = (upperLength * upperLength - lowerLength * lowerLength + distance * distance) / (2 * distance);
        float height = Mathf.Sqrt(Mathf.Max(0, upperLength * upperLength - along * along));
        Vector3 elbow = upper.position + direction * along + bend * height;
        upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, elbow - upper.position) * upper.rotation;
        Vector3 reachable = upper.position + direction * distance;
        lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, reachable - lower.position) * lower.rotation;
        Quaternion solvedUpper = upper.localRotation, solvedLower = lower.localRotation;
        upper.localRotation = Quaternion.Slerp(upperPose, solvedUpper, weight);
        lower.localRotation = Quaternion.Slerp(lowerPose, solvedLower, weight);
        hand.localRotation = handPose;
    }
}
