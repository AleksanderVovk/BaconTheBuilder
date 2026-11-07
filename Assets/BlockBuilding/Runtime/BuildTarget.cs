using UnityEngine;

public class BuildTarget : MonoBehaviour
{
    [Header("References"), Tooltip("Structure receiving blocks.")]
    public BuildSite site;

    [Header("Carrier"), Tooltip("Existing player's carrier.")]
    public PlayerBlockCarrier carrier;

    [Header("Distance"), Tooltip("Horizontal distance to the current ghost center, in world units."), Min(0.2f)]
    public float radius = 3f;

    [Header("Height"), Tooltip("Vertical tolerance near the ghost or ground-level delivery point."), Min(0.2f)]
    public float heightTolerance = 1.8f;

    [Header("Original delivery point"), Tooltip("Also accept cubes around the authored delivery pad.")]
    public bool allowDeliveryPoint = false;

    [Header("Build from ground"), Tooltip("Allow standing below high cells, within their horizontal reach.")]
    public bool allowGroundProjection = true;

    [Header("Rate"), Tooltip("Seconds per transferred block."), Min(0.05f)]
    public float transferInterval = 0.3f;

    private float nextTransfer;
    public bool IsInRange
    {
        get
        {
            return carrier != null && carrier.player != null && ContainsPosition(carrier.player.position);
        }
    }

    public bool ContainsPosition(Vector3 playerPosition)
    {
        if (site == null || site.IsComplete || !site.IsActiveSite)
            return false;
        float reach = Mathf.Max(.2f, radius);
        Vector3 delta = playerPosition - site.NextWorldPosition;
        return delta.x * delta.x + delta.z * delta.z <= reach * reach
            && (allowGroundProjection
                || Mathf.Abs(delta.y) <= Mathf.Max(.2f, heightTolerance));
    }

    private void Update()
    {
        TryTransfer();
    }

    public bool TryTransfer()
    {
        if (Time.time < nextTransfer || carrier == null || !IsInRange)
            return false;
        if (!site.TryBuild(carrier))
            return false;
        nextTransfer = Time.time + Mathf.Max(0.005f, transferInterval / (carrier.level != null
            ? carrier.level.DebugBuildSpeed
            : 1));
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (allowDeliveryPoint)
            Gizmos.DrawWireSphere(transform.position, radius);
        if (site != null && !site.IsComplete)
            Gizmos.DrawWireCube(
                site.NextWorldPosition,
                new Vector3(site.BlockSize + 2 * radius, site.BlockSize + 2 * heightTolerance, site.BlockSize + 2 * radius)
            );
    }
}
