using UnityEngine;

public class LevelMenuPortal : MonoBehaviour
{
    [Header("Session"), Tooltip("Existing level menu; no scene reload.")]
    public BuildLevelSession session;

    [Header("Player"), Tooltip("Existing player Transform, compatible with any controller.")]
    public Transform player;

    [Header("Approach"), Tooltip("Horizontal distance that opens the menu."), Min(.5f)]
    public float openRadius = 2.8f;

    [Header("Leave distance"), Tooltip("Extra distance to leave before the portal can open again."), Min(.2f)]
    public float exitMargin = 1;

    [Header("Height"), Tooltip("Maximum foot/root height difference from the portal."), Min(1)]
    public float heightTolerance = 4;

    [Header("Floating cubes"), Tooltip("Decorative cubes reused without spawning.")]
    public Transform[] floatingCubes = new Transform[0];

    private Vector3[] cubeOrigins;
    private bool entered;
    private void Awake()
    {
        cubeOrigins = new Vector3[floatingCubes.Length];
        for (int i = 0; i < floatingCubes.Length; i++)
            if (floatingCubes[i] != null)
                cubeOrigins[i] = floatingCubes[i].localPosition;
    }

    private void Update()
    {
        CheckApproach();
        for (int i = 0; i < floatingCubes.Length; i++)
            if (floatingCubes[i] != null)
            {
                floatingCubes[i].localPosition = cubeOrigins[i] + Vector3.up * (Mathf.Sin(Time.time * 1.6f + i * 2) * .12f);
                floatingCubes[i].Rotate(0, Time.deltaTime * 22, 0, Space.Self);
            }
    }

    public bool CheckApproach()
    {
        if (player == null || session == null)
            return false;
        Vector3 d = player.position - transform.position;
        float sqr = d.x * d.x + d.z * d.z;
        float leave = openRadius + exitMargin;
        if (sqr > leave * leave || Mathf.Abs(d.y) > heightTolerance)
        {
            entered = false;
            return false;
        }

        if (entered || session.SettingsOpen || session.MenuOpen || session.AdminOverlayOpen || sqr > openRadius * openRadius)
            return false;
        entered = true;
        session.OpenMenu();
        return true;
    }
}
