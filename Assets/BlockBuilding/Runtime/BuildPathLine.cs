using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BuildPathLine : MonoBehaviour
{
    [Header("Start"), Tooltip("Existing player transform.")]
    public Transform source;

    [Header("Offset"), Tooltip("Starting height above the player's feet.")]
    public Vector3 sourceOffset = new Vector3(0, -.95f, 0);

    [Header("Line"), Tooltip("Two-point direction indicator with the Arrow material.")]
    public LineRenderer line;

    [Header("Inventory"), Tooltip("Show the arrow only while carrying blocks.")]
    public PlayerBlockCarrier carrier;

    [Header("Clearance"), Tooltip("Minimum height above the player's current foot level."), Min(.03f)]
    public float clearance = .08f;

    [Header("Ground surfaces"), Tooltip("Ground and podium only; walls do not alter the straight direction line.")]
    public Renderer[] groundSurfaces = new Renderer[0];

    private BuildSite current;
    private bool guided;
    private Vector3 guidePoint;
    private Collider playerCollider;
    public void Guide(bool active, Vector3 point)
    {
        guided = active;
        guidePoint = point;
    }

    public void SetTarget(BuildSite site)
    {
        current = site;
    }

    private void Awake()
    {
        if (line == null)
            line = GetComponent<LineRenderer>();
        if (source != null)
            playerCollider = source.GetComponent<Collider>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.enabled = false;
    }

    private void LateUpdate()
    {
        bool visible = carrier != null
            && carrier.Count > 0
            && source != null
            && (guided
                || current != null
                && !current.IsComplete);
        line.enabled = visible;
        if (!visible)
            return;
        Vector3 start = source.position + sourceOffset;
        if (playerCollider != null && playerCollider.enabled)
            start.y = Mathf.Max(start.y, playerCollider.bounds.min.y + clearance);
        Vector3 goal = guided ? guidePoint : current.NextWorldPosition;
        Vector3 direction = goal - start;
        float distance = direction.magnitude;
        if (distance <= .05f)
        {
            line.enabled = false;
            return;
        }

        // Aim at the actual cell center, clamping only targets buried below the floor.
        Vector3 end = goal;
        float y = float.NegativeInfinity;
        foreach (var surface in groundSurfaces)
        {
            if (surface == null || !surface.enabled || !surface.gameObject.activeInHierarchy)
                continue;
            Bounds b = surface.bounds;
            float half = line.widthMultiplier * .5f;
            if (CrossesGround(start, end, b, half))
                y = Mathf.Max(y, b.max.y + clearance);
        }

        start.y = Mathf.Max(start.y, y);
        end.y = Mathf.Max(end.y, y);
        Vector3 delta = end - start;
        Vector3 width = new Vector3(-delta.z, 0, delta.x);
        Vector3 normal = width.sqrMagnitude > .0001f ? Vector3.Cross(width, delta).normalized : source.forward;
        line.alignment = LineAlignment.TransformZ;
        line.transform.rotation = Quaternion.LookRotation(normal);
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }

    private static bool CrossesGround(Vector3 a, Vector3 b, Bounds box, float padding)
    {
        float enter = 0, exit = 1;
        return Slab(a.x, b.x - a.x, box.min.x - padding, box.max.x + padding, ref enter, ref exit)
            && Slab(a.z, b.z - a.z, box.min.z - padding, box.max.z + padding, ref enter, ref exit);
    }

    private static bool Slab(float start, float delta, float min, float max, ref float enter, ref float exit)
    {
        if (Mathf.Abs(delta) < .00001f)
            return start >= min && start <= max;
        float a = (min - start) / delta, b = (max - start) / delta;
        if (a > b)
        {
            float swap = a;
            a = b;
            b = swap;
        }

        enter = Mathf.Max(enter, a);
        exit = Mathf.Min(exit, b);
        return enter <= exit;
    }
}
