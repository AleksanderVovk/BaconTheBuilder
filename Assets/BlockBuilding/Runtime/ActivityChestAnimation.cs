using UnityEngine;

public class ActivityChestAnimation : MonoBehaviour
{
    [Header("Reward"), Tooltip("Chest activity state.")]
    public ActivityChestReward reward;

    [Header("Lid"), Tooltip("Hinge at the back of the chest.")]
    public Transform lid;

    [Header("Effect"), Tooltip("Pooled celebratory particles.")]
    public ParticleSystem celebration;

    [Header("Decor"), Tooltip("Floating golden cubes.")]
    public Transform[] sparks = new Transform[0];

    private Vector3[] origins;
    private Quaternion closed;
    private float claimedAt = -100;
    private void Start()
    {
        closed = lid.localRotation;
        origins = new Vector3[sparks.Length];
        for (int i = 0; i < sparks.Length; i++)
            origins[i] = sparks[i].localPosition;
        reward.Claimed += Celebrate;
    }

    private void Celebrate(System.Numerics.BigInteger amount)
    {
        claimedAt = Time.unscaledTime;
        if (celebration != null)
            celebration.Play();
    }

    private void Update()
    {
        float elapsed = Time.unscaledTime - claimedAt;
        float angle = elapsed < .45f
            ? Mathf.SmoothStep(0, 108, elapsed / .45f)
            : elapsed < 1.55f
            ? 108
            : elapsed < 2.2f
            ? Mathf.SmoothStep(108, 0, (elapsed - 1.55f) / .65f)
            : reward.Pending > 0
            ? 2 + Mathf.Sin(Time.time * 5) * 2
            : Mathf.Sin(Time.time * 1.5f) * .7f;
        lid.localRotation = closed * Quaternion.Euler(-angle, 0, 0);
        for (int i = 0; i < sparks.Length; i++)
        {
            sparks[i].localPosition = origins[i] + Vector3.up * (Mathf.Sin(Time.time * 2 + i * 1.7f) * .15f);
            sparks[i].Rotate(0, Time.deltaTime * 30, 0, Space.Self);
        }
    }

    private void OnDestroy()
    {
        if (reward != null)
            reward.Claimed -= Celebrate;
    }
}
