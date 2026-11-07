using UnityEngine;

public class BlockPickupZone : MonoBehaviour
{
    [Header("Player"), Tooltip("Existing player carrier; distance checks do not require trigger callbacks.")]
    public PlayerBlockCarrier carrier;

    [Header("Zone"), Tooltip("Horizontal pickup radius in world units."), Min(0.2f)]
    public float radius = 2;

    [Header("Height"), Tooltip("Maximum vertical separation, measured from player pivot."), Min(0.2f)]
    public float heightTolerance = 3;

    [Header("Keyboard"), Tooltip("Old Input Manager pickup key. One cube per press.")]
    public KeyCode pickupKey = KeyCode.E;

    [Header("Automatic pickup"), Tooltip("Fill all available hand slots immediately while inside the supply zone.")]
    public bool automaticPickup = true;
    public bool IsInRange
    {
        get
        {
            if (carrier == null || carrier.player == null)
                return false;
            Vector3 d = carrier.player.position - transform.position;
            return Mathf.Abs(d.y) <= heightTolerance && d.x * d.x + d.z * d.z <= radius * radius;
        }
    }

    private void Update()
    {
        if (automaticPickup)
            TryAutoPickup();
        else if (Input.GetKeyDown(pickupKey))
            TryPickup();
    }

    public int TryAutoPickup()
    {
        if (!isActiveAndEnabled || !automaticPickup || !IsInRange || carrier.IsTraining || carrier.Count >= carrier.Capacity)
            return 0;
        return carrier.FillToCapacity();
    }

    public bool TryPickup()
    {
        return IsInRange && carrier.TryPickup();
    }

    public void PickupFromUI()
    {
        if (automaticPickup)
            TryAutoPickup();
        else
            TryPickup();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
