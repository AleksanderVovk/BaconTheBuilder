using System;
using UnityEngine;

// Startup composition only. Scene references remain editable; no controller lookup in Update.
public class BlockLevelRuntime : MonoBehaviour
{
    [Header("Data"), Tooltip("Level asset shared with the editor tool.")]
    public BuildSiteData data;

    [Header("Player"), Tooltip("Existing player's carrier.")]
    public PlayerBlockCarrier carrier;

    [Header("Guidance"), Tooltip("Line to the current ghost.")]
    public BuildPathLine path;

    [Header("Ghost material"), Tooltip("Shared ghost material. Usually transparent cyan.")]
    public Material ghostMaterial;

    [Header("Cell mesh"), Tooltip("Unit cube mesh for the ghost.")]
    public Mesh cubeMesh;

    [Header("Physics"), Tooltip("Enable chunk MeshColliders; no Rigidbody is added.")]
    public bool createColliders = true;

    [Header("World layer"), Tooltip("Layer used by chunk colliders; include in the controller's ground mask."), Range(0, 31)]
    public int worldLayer = 6;

    [Header("Delivery radius"), Tooltip("Horizontal distance from each delivery point."), Min(0.2f)]
    public float deliveryRadius = 3;

    [Header("Delivery height"), Tooltip("Vertical tolerance from each delivery point."), Min(0.2f)]
    public float deliveryHeightTolerance = 3;

    [Header("Delivery rate"), Tooltip("Seconds per cube transfer."), Min(0.01f)]
    public float transferInterval = 0.3f;

    [Header("Chunk size"), Tooltip("Cells along one chunk edge."), Range(2, 8)]
    public int chunkSize = 8;

    [Header("Collider interval"), Tooltip("Seconds between batched collision mesh updates."), Min(0.05f)]
    public float colliderInterval = 0.35f;

    [Header("Delivery pad"), Tooltip("Keep the original delivery pad as an additional valid interaction point.")]
    public bool allowDeliveryPoint = false;

    [Header("Ground construction"), Tooltip("Allow high cells to be built from their ground projection.")]
    public bool allowGroundProjection = true;

    [Header("Startup"), Tooltip("Disable when a level menu loads the selected asset.")]
    public bool autoLoadOnStart = true;

    [Header("Supply zone"), Tooltip("Existing supply pile, moved with each level.")]
    public Transform pickupTransform;

    [Header("Training zone"), Tooltip("Existing bench, moved with each level.")]
    public Transform trainingTransform;
    public float DebugBuildSpeed { get; set; } = 1;

    private bool paused;
    private BuildSite[] sites;
    public BuildSite LastPlacedSite { get; private set; }
    public Vector3 LastPlacedPosition { get; private set; }

    public float HighestBuiltTop(Vector3 center, float radius)
    {
        float top = float.NegativeInfinity;
        if (sites != null)
            foreach (var site in sites)
                top = Mathf.Max(top, site.builder.HighestBuiltTop(center, radius));
        return top;
    }

    public bool OverlapsBuiltBlocks(Bounds body)
    {
        if (sites != null)
            foreach (var site in sites)
                if (site != null && site.builder.OverlapsBuiltBlocks(body))
                    return true;
        return false;
    }

    public void FlushBuiltColliders()
    {
        if (sites != null)
            foreach (var site in sites)
                site.builder.Complete();
    }

    public int TotalCount { get; private set; }

    public int BuiltCount
    {
        get
        {
            int n = 0;
            if (sites != null)
                foreach (var site in sites)
                    n += site.BuiltCount - site.FoundationCount;
            return n;
        }
    }

    public int RawTotalCount
    {
        get
        {
            return BuildFoundation.RawCount(data);
        }
    }

    public int StoredBuiltCount
    {
        get
        {
            int n = 0;
            if (sites != null)
                foreach (var site in sites)
                {
                    n += site.BuiltCount;
                    if (!site.IsComplete)
                        break;
                }

            return n;
        }
    }

    public bool IsComplete
    {
        get
        {
            return sites != null && TotalCount > 0 && current >= sites.Length;
        }
    }

    public void SetPaused(bool value)
    {
        paused = value;
        if (CurrentSite != null)
            CurrentSite.SetActiveSite(!value);
        if (path != null)
            path.SetTarget(value ? null : CurrentSite);
    }

    public void Unload()
    {
        if (sites != null)
            foreach (var site in sites)
                if (site != null)
                {
                    site.Changed -= SiteChanged;
                    site.BlockPlaced -= ForwardBlockPlaced;
                    site.gameObject.SetActive(false);
                    Destroy(site.gameObject);
                }

        sites = null;
        current = 0;
        TotalCount = 0;
        LastPlacedSite = null;
        LastPlacedPosition = Vector3.zero;
        if (path != null)
            path.SetTarget(null);
    }

    private int current;
    public event Action Changed;
    public event Action<BlockType, Vector3> BlockPlaced;
    public BuildSite CurrentSite
    {
        get
        {
            return sites != null && current < sites.Length ? sites[current] : null;
        }
    }

    public bool TryGetSupplyType(int offset, out BlockType type)
    {
        type = BlockType.Cobblestone;
        if (sites == null || offset < 0)
            return false;
        for (int i = current; i < sites.Length; i++)
        {
            int remaining = sites[i].TotalCount - sites[i].BuiltCount;
            if (offset < remaining)
                return sites[i].TryGetPendingType(offset, out type);
            offset -= remaining;
        }

        return false;
    }

    private void Awake()
    {
        var designPreview = transform.Find("DesignPreview");
        if (designPreview != null)
            designPreview.gameObject.SetActive(false);
        if (autoLoadOnStart)
            LoadLevel(data);
    }

    public void LoadLevel(BuildSiteData selected)
    {
        if (selected == null)
            throw new ArgumentNullException("selected");
        selected.level.Validate();
        Unload();
        data = selected;
        current = 0;
        DebugBuildSpeed = 1;
        if (pickupTransform != null)
            pickupTransform.position = data.level.pickupPoint;
        if (trainingTransform != null)
            trainingTransform.position = data.level.trainingPoint;
        if (data == null || carrier == null || cubeMesh == null || data.blockMaterial == null)
        {
            Debug.LogError("BlockLevelRuntime: incomplete setup.", this);
            enabled = false;
            return;
        }

        try
        {
            data.level.Validate();
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message, this);
            enabled = false;
            return;
        }

        carrier.level = this;
        carrier.useAtlas = data.useAtlas;
        carrier.material = data.blockMaterial;
        carrier.Initialize();
        sites = new BuildSite[data.level.sites.Count];
        for (int i = 0; i < sites.Length; i++)
        {
            var definition = data.level.sites[i];
            TotalCount += definition.blocks.Count - BuildFoundation.Count(definition);
            var go = new GameObject(definition.id);
            go.layer = worldLayer;
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(definition.origin, Quaternion.Euler(0, definition.yaw, 0));
            var builder = go.AddComponent<BuildSiteBuilder>();
            builder.material = data.blockMaterial;
            builder.createColliders = createColliders;
            builder.chunkSize = chunkSize;
            builder.colliderInterval = colliderInterval;
            var site = go.AddComponent<BuildSite>();
            site.builder = builder;
            var ghost = new GameObject("NextBlockGhost");
            ghost.transform.SetParent(go.transform, false);
            ghost.AddComponent<MeshFilter>().sharedMesh = cubeMesh;
            var renderer = ghost.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = ghostMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            site.ghost = ghost.transform;
            site.visualSource = carrier;
            site.Initialize(definition, data.level.blockSize);
            site.Changed += SiteChanged;
            site.BlockPlaced += ForwardBlockPlaced;
            sites[i] = site;
            var targetObject = new GameObject("DeliveryPoint");
            targetObject.transform.SetParent(go.transform, false);
            targetObject.transform.position = definition.deliveryPoint;
            var target = targetObject.AddComponent<BuildTarget>();
            target.site = site;
            target.carrier = carrier;
            target.radius = deliveryRadius;
            target.heightTolerance = deliveryHeightTolerance;
            target.transferInterval = transferInterval;
            target.allowDeliveryPoint = allowDeliveryPoint;
            target.allowGroundProjection = allowGroundProjection;
        }

        Advance();
        Changed?.Invoke();
    }

    public void RestoreBuiltCount(int count)
    {
        if (sites == null)
            return;
        int remaining = Mathf.Clamp(count, 0, RawTotalCount);
        foreach (var site in sites)
        {
            int restored = Mathf.Min(remaining, site.TotalCount);
            site.RestoreBuiltCount(restored);
            remaining -= restored;
            if (restored > site.FoundationCount)
            {
                LastPlacedSite = site;
                LastPlacedPosition = site.transform.TransformPoint(((Vector3)site.LastBuiltCell + Vector3.one * .5f) * site.BlockSize);
            }
        }

        Advance();
        Changed?.Invoke();
    }

    private void ForwardBlockPlaced(BlockType type, Vector3 position)
    {
        // BuildSite raises BlockPlaced before Changed advances to another site.
        LastPlacedSite = CurrentSite;
        LastPlacedPosition = position;
        BlockPlaced?.Invoke(type, position);
    }

    private void SiteChanged()
    {
        Advance();
        Changed?.Invoke();
    }

    private void Advance()
    {
        while (current < sites.Length && sites[current].IsComplete)
        {
            sites[current].SetActiveSite(false);
            current++;
        }

        if (current < sites.Length)
            sites[current].SetActiveSite(!paused);
        if (path != null)
            path.SetTarget(paused ? null : CurrentSite);
    }

    private void OnDestroy()
    {
        if (sites != null)
            foreach (var site in sites)
                if (site != null)
                {
                    site.Changed -= SiteChanged;
                    site.BlockPlaced -= ForwardBlockPlaced;
                }
    }
}
