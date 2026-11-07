using UnityEngine;

// Integrates with the existing controller through the session's reset event.
[DefaultExecutionOrder(1000)]
public class BuildPlayerSafety : MonoBehaviour
{
    [Header("Construction"), Tooltip("Existing runtime; only placed cells are considered solid.")]
    public BlockLevelRuntime runtime;

    [Header("Session"), Tooltip("Existing session and controller reset hook.")]
    public BuildLevelSession session;

    [Header("Player"), Tooltip("Existing player root. Its controller is never replaced.")]
    public Transform player;

    [Header("Capsule"), Tooltip("Existing CharacterController; optional for Transform-only controllers.")]
    public CharacterController character;

    [Header("Clearance"), Tooltip("Space above built surfaces and below a new ceiling."), Min(0.02f)]
    public float clearance = 0.15f;

    [Header("Fallback height"), Tooltip("Player height when there is no CharacterController."), Min(0.2f)]
    public float fallbackHeight = 1.8f;

    [Header("Fallback radius"), Tooltip("Player radius when there is no CharacterController."), Min(0.05f)]
    public float fallbackRadius = 0.5f;

    [Header("Fallback feet"), Tooltip("Local feet height relative to the player pivot, without CharacterController.")]
    public float fallbackFeetOffset = 0;

    [Header("Return button"), Tooltip("Normal gameplay button; unavailable until a block has been placed.")]
    public UnityEngine.UI.Button returnButton;

    [Header("Keyboard"), Tooltip("Old Input Manager shortcut for the same return action.")]
    public KeyCode returnKey = KeyCode.T;

    [Header("Stuck check"), Tooltip("Check existing construction for a trapped player at this interval, in real seconds."), Min(.25f)]
    public float stuckCheckInterval = 2f;

    [Header("Fall rescue"), Tooltip("Teleport to the existing spawn when this far below it."), Min(3)]
    public float fallDepth = 12f;

    private float nextStuckCheck;
    private bool placementCheckPending;
    public int LiftCount { get; private set; }
    public int ReturnCount { get; private set; }

    public bool CanReturn
    {
        get
        {
            return Playable && runtime.LastPlacedSite != null;
        }
    }

    private bool Playable
    {
        get
        {
            return runtime != null
                && player != null
                && (session == null
                    || !session.MenuOpen
                    && !session.SettingsOpen
                    && !session.AdminOverlayOpen
                    && (!session.Completed
                        || session.IsExploringCompleted))
                && runtime.carrier != null
                && !runtime.carrier.IsTraining;
        }
    }

    private void OnEnable()
    {
        nextStuckCheck = 0;
        placementCheckPending = false;
        if (runtime != null)
        {
            runtime.BlockPlaced += Placed;
            runtime.Changed += Refresh;
        }

        if (session != null)
            session.Changed += Refresh;
        if (runtime != null && runtime.carrier != null)
            runtime.carrier.Changed += Refresh;
        if (returnButton != null)
            returnButton.onClick.AddListener(ReturnToLastBlock);
        Refresh();
    }

    private void OnDisable()
    {
        placementCheckPending = false;
        if (runtime != null)
        {
            runtime.BlockPlaced -= Placed;
            runtime.Changed -= Refresh;
        }

        if (session != null)
            session.Changed -= Refresh;
        if (runtime != null && runtime.carrier != null)
            runtime.carrier.Changed -= Refresh;
        if (returnButton != null)
            returnButton.onClick.RemoveListener(ReturnToLastBlock);
    }

    private void Update()
    {
        if (CanReturn && Input.GetKeyDown(returnKey))
            ReturnToLastBlock();
    }

    private void LateUpdate()
    {
        if (!Playable)
        {
            placementCheckPending = false;
            return;
        }

        Vector3 position = player.position;
        if (session != null
            && (position.y < session.spawn.y - fallDepth
                || float.IsNaN(position.x)
                || float.IsNaN(position.y)
                || float.IsNaN(position.z)
                || float.IsInfinity(position.x)
                || float.IsInfinity(position.y)
                || float.IsInfinity(position.z)))
        {
            MovePlayer(session.spawn);
            placementCheckPending = false;
            return;
        }

        bool nearNewBlock = placementCheckPending;
        placementCheckPending = false;
        if (!nearNewBlock && Time.unscaledTime < nextStuckCheck)
            return;
        nextStuckCheck = Time.unscaledTime + Mathf.Max(.25f, stuckCheckInterval);
        LiftOutOfBlocks(nearNewBlock);
    }

    private void Refresh()
    {
        if (returnButton != null)
            returnButton.interactable = CanReturn;
    }

    private Bounds PlayerBounds()
    {
        Vector3 scale = player.lossyScale;
        if (character != null)
        {
            Vector3 capsuleScale = character.transform.lossyScale;
            float radius = Mathf.Max(
                .01f,
                character.radius * Mathf.Max(Mathf.Abs(capsuleScale.x), Mathf.Abs(capsuleScale.z))
            );
            float height = Mathf.Max(character.height * Mathf.Abs(capsuleScale.y), radius * 2);
            return new Bounds(
                character.transform.TransformPoint(character.center),
                new Vector3(radius * 2, height, radius * 2)
            );
        }

        float r = Mathf.Max(.01f, fallbackRadius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)));
        float h = Mathf.Max(fallbackHeight * Mathf.Abs(scale.y), r * 2);
        return new Bounds(
            player.position + Vector3.up * (fallbackFeetOffset * scale.y + h * .5f),
            new Vector3(r * 2, h, r * 2)
        );
    }

    private void Placed(BlockType type, Vector3 position)
    {
        if (!Playable || runtime.LastPlacedSite == null)
            return;
        Bounds block = runtime.LastPlacedSite.builder.CellWorldBounds(runtime.LastPlacedSite.LastBuiltCell);
        Bounds protectedBody = PlayerBounds();
        protectedBody.Expand(Mathf.Max(.02f, clearance) * 2);
        if (protectedBody.Intersects(block))
            placementCheckPending = true;
    }

    private void LiftOutOfBlocks(bool nearNewBlock)
    {
        Bounds body = PlayerBounds(), probe = body;
        float gap = Mathf.Max(.02f, clearance);
        if (nearNewBlock)
            probe.Expand(gap * 2);
        else
        {
            // Ignore normal contact with the floor and controller skin width.
            Vector3 scale = character != null ? character.transform.lossyScale : player.lossyScale;
            float skin = character != null
                ? Mathf.Max(0, character.skinWidth) * Mathf.Max(
                    Mathf.Abs(scale.x),
                    Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z))
                )
                : .03f;
            float inset = Mathf.Max(.03f, skin + .02f);
            Vector3 shrink = new Vector3(
                Mathf.Min(inset * 2, body.size.x * .4f),
                Mathf.Min(inset * 2, body.size.y * .4f),
                Mathf.Min(inset * 2, body.size.z * .4f)
            );
            probe.Expand(-shrink);
        }

        if (!runtime.OverlapsBuiltBlocks(probe))
            return;
        float radius = Mathf.Sqrt(body.extents.x * body.extents.x + body.extents.z * body.extents.z) + gap * 2;
        float top = runtime.HighestBuiltTop(body.center, radius);
        if (float.IsInfinity(top) || float.IsNaN(top))
            return;
        float lift = top + gap - body.min.y;
        if (lift <= .001f)
            return;
        // Clear the entire column in one move, including roofs above the intersecting cell.
        MovePlayer(player.position + Vector3.up * lift);
        LiftCount++;
    }

    public void ReturnToLastBlock()
    {
        if (!CanReturn)
            return;
        Bounds body = PlayerBounds();
        Vector3 target = runtime.LastPlacedPosition;
        // Offset the pivot so the capsule center, including controller.center,
        // is above the last cell. Also account for higher completed structures.
        float top = runtime.HighestBuiltTop(
            target,
            Mathf.Sqrt(body.extents.x * body.extents.x + body.extents.z * body.extents.z) + Mathf.Max(.02f, clearance) * 2
        );
        if (float.IsNegativeInfinity(top))
            return;
        Vector3 centerOffset = body.center - player.position;
        target.y = top + clearance + body.extents.y;
        MovePlayer(target - centerOffset);
        ReturnCount++;
    }

    private void MovePlayer(Vector3 destination)
    {
        // The supporting cell must be solid now, not after the batching delay.
        runtime.FlushBuiltColliders();
        bool restore = character != null && character.enabled;
        if (restore)
            character.enabled = false;
        player.position = destination;
        Physics.SyncTransforms();
        if (restore)
            character.enabled = true;
        if (session != null)
            session.onPlayerReset.Invoke();
    }
}
