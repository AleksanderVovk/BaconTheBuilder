using System;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerBlockCarrier : MonoBehaviour
{
    [Header("Player"), Tooltip("Existing player transform; any controller can be used.")]
    public Transform player;

    [Header("Attachment"), Tooltip("Hand/head anchor for carried cubes.")]
    public Transform carryAnchor;

    [Header("Strength"), Tooltip("Normal gameplay capacity progression.")]
    public StrengthUpgradeSystem strength;

    [Header("Visual"), Tooltip("Fallback mesh when Use Atlas is disabled.")]
    public Mesh blockMesh;

    [Header("Material"), Tooltip("Shared atlas material.")]
    public Material material;

    [Header("Stack"), Tooltip("Size of a carried cube."), Range(0.1f, 1)]
    public float visualScale = 0.55f;

    [Header("Atlas"), Tooltip("Use typed atlas meshes for blocks.")]
    public bool useAtlas;

    [Header("Supply queue"), Tooltip("Select textures from the next pending cells.")]
    public BlockLevelRuntime level;

    [Header("Visible stack"), Tooltip("Maximum rendered cubes, regardless of the logical inventory size."), Range(1, 27)]
    public int maximumVisible = 18;

    public const int AdminMaximum = 9999;
    private Mesh combined;
    private MeshRenderer view;
    private CombineInstance[][] instances;
    private Mesh[] typeMeshes;
    private BlockType[] carried;
    private int head;
    private bool initialized;
    public event Action Changed;
    private int storedCount;
    public bool InfiniteBlocksActive { get; private set; }

    public int StoredCount
    {
        get
        {
            return storedCount;
        }
    }

    public int Count
    {
        get
        {
            return InfiniteBlocksActive ? Mathf.Max(1, storedCount) : storedCount;
        }

        private set
        {
            storedCount = value;
        }
    }

    public bool IsTraining { get; set; }

    public int Capacity
    {
        get
        {
            return strength != null ? strength.Capacity : 1;
        }
    }

    public int VisibleCount
    {
        get
        {
            return Mathf.Min(InfiniteBlocksActive ? 3 : Count, instances == null ? maximumVisible : instances.Length);
        }
    }

    public float VisualStackHeight
    {
        get
        {
            return Mathf.Ceil(VisibleCount / (VisibleCount > 9 ? 9f : 3f)) * visualScale * 1.05f;
        }
    }

    public int VisualVertexCount
    {
        get
        {
            return combined != null ? combined.vertexCount : 0;
        }
    }

    public BlockType FirstType
    {
        get
        {
            BlockType type;
            if (InfiniteBlocksActive && level != null && level.TryGetSupplyType(0, out type))
                return type;
            return storedCount > 0 ? carried[head] : BlockType.Cobblestone;
        }
    }

    public bool HasSupplyForNextSlot
    {
        get
        {
            return level == null || level.CurrentSite != null;
        }
    }

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (initialized)
            return;
        if (player == null)
            player = transform;
        if (strength != null)
            strength.Initialize();
        if (carryAnchor == null)
        {
            var anchor = new GameObject("BlockCarryAnchor");
            anchor.transform.SetParent(player, false);
            anchor.transform.localPosition = new Vector3(0, 1.8f, 0);
            carryAnchor = anchor.transform;
        }

        if ((!useAtlas && blockMesh == null) || material == null)
            throw new InvalidOperationException("Carrier needs a material and mesh.");
        if (useAtlas)
        {
            typeMeshes = new Mesh[BlockPalette.Count];
            var scratch = new BlockMeshUtility();
            for (int i = 0; i < typeMeshes.Length; i++)
            {
                var mesh = new Mesh
                {
                    name = "Atlas cube " + (BlockType)i
                };
                scratch.Begin();
                scratch.Cell(Vector3Int.zero, 1, null, (BlockType)i);
                scratch.Apply(mesh);
                var vertices = mesh.vertices;
                for (int v = 0; v < vertices.Length; v++)
                    vertices[v] -= Vector3.one * 0.5f;
                mesh.vertices = vertices;
                mesh.RecalculateBounds();
                typeMeshes[i] = mesh;
            }
        }

        int visible = Mathf.Clamp(maximumVisible, 1, 27);
        var go = new GameObject("CarriedBlocks");
        go.transform.SetParent(carryAnchor, false);
        int verticesPerCube = useAtlas ? 24 : blockMesh.vertexCount;
        combined = new Mesh
        {
            name = "Carried cubes",
            indexFormat = verticesPerCube * visible > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
        };
        combined.MarkDynamic();
        go.AddComponent<MeshFilter>().sharedMesh = combined;
        view = go.AddComponent<MeshRenderer>();
        view.sharedMaterial = material;
        view.shadowCastingMode = ShadowCastingMode.Off;
        carried = new BlockType[Mathf.Max(AdminMaximum, strength != null ? strength.MaximumCapacity : 1)];
        instances = new CombineInstance[visible][];
        for (int count = 1; count <= visible; count++)
        {
            instances[count - 1] = new CombineInstance[count];
            for (int i = 0; i < count; i++)
                instances[count - 1][i].transform = Matrix4x4.TRS(new Vector3((i % 3 - (Mathf.Min(count, 3) - 1) * 0.5f) * visualScale * 1.05f, (count > 9
                    ? i / 9
                    : i / 3) * visualScale * 1.05f, count > 9
                    ? ((i / 3) % 3 - 1) * visualScale * 1.05f
                    : 0), Quaternion.identity, Vector3.one * visualScale);
        }

        initialized = true;
        if (strength != null)
            strength.Changed += CapacityChanged;
        Refresh();
    }

    public Mesh GetVisualMesh(BlockType type)
    {
        Initialize();
        return useAtlas ? typeMeshes[(int)type] : blockMesh;
    }

    // One combined-mesh rebuild and one event for the entire automatic refill.
    public int FillToCapacity()
    {
        Initialize();
        if (IsTraining || InfiniteBlocksActive || !HasSupplyForNextSlot)
            return 0;
        int before = Count;
        while (Count < Capacity && Count < carried.Length)
        {
            BlockType type = SupplyTypeForSlot(Count);
            carried[(head + Count) % carried.Length] = type;
            Count++;
        }

        if (Count != before)
        {
            Refresh();
            Changed?.Invoke();
        }

        return Count - before;
    }

    public bool TryPickup()
    {
        Initialize();
        if (!HasSupplyForNextSlot)
            return false;
        return TryPickup(SupplyTypeForSlot(Count));
    }

    private BlockType SupplyTypeForSlot(int offset)
    {
        BlockType type;
        if (level != null && level.TryGetSupplyType(offset, out type))
            return type;
        // Unused surplus is real inventory too; its decorative texture repeats
        // the previous cell rather than limiting pickup to unfinished cells.
        return offset > 0 ? carried[(head + offset - 1) % carried.Length] : BlockType.Cobblestone;
    }

    public bool TryPickup(BlockType type)
    {
        Initialize();
        if (IsTraining
            || InfiniteBlocksActive
            || Count >= Capacity
            || Count >= carried.Length
            || (int)type < 0
            || (int)type >= BlockPalette.Count)
            return false;
        carried[(head + Count) % carried.Length] = type;
        Count++;
        Refresh();
        Changed?.Invoke();
        return true;
    }

    public bool TryConsume(BlockType expected)
    {
        return InfiniteBlocksActive
            ? !IsTraining
            && (int)expected >= 0
            && (int)expected < BlockPalette.Count
            : Count > 0
            && FirstType == expected
            && TryConsume();
    }

    public bool TryConsume()
    {
        if (IsTraining)
            return false;
        if (InfiniteBlocksActive)
            return true;
        if (Count == 0)
            return false;
        head = (head + 1) % carried.Length;
        Count--;
        Refresh();
        Changed?.Invoke();
        return true;
    }

    // Editor/development tooling marks the run assisted before invoking this.
    public int GrantForTesting(int amount)
    {
        Initialize();
        if (IsTraining || level == null || level.CurrentSite == null)
            return 0;
        Count = Mathf.Clamp(amount, 0, AdminMaximum);
        head = 0;
        BlockType last = BlockType.Cobblestone;
        for (int i = 0; i < Count; i++)
        {
            BlockType type;
            if (level.TryGetSupplyType(i, out type))
                last = type;
            carried[i] = last;
        }

        Refresh();
        Changed?.Invoke();
        return Count;
    }

    public void Clear()
    {
        Initialize();
        Count = 0;
        head = 0;
        Refresh();
        Changed?.Invoke();
    }

    private void CapacityChanged()
    {
        Changed?.Invoke();
    }

    private void Refresh()
    {
        combined.Clear();
        view.enabled = Count > 0;
        if (Count == 0)
            return;
        var batch = instances[VisibleCount - 1];
        for (int i = 0; i < batch.Length; i++)
            batch[i].mesh = useAtlas
                ? typeMeshes[(int)(InfiniteBlocksActive
                ? FirstType
                : carried[(head + i) % carried.Length])]
                : blockMesh;
        combined.CombineMeshes(batch, true, true, false);
    }

    public void SetInfiniteBlocks(bool active)
    {
        Initialize();
        if (InfiniteBlocksActive == active)
            return;
        InfiniteBlocksActive = active;
        // Infinite construction advances the pending cells without consuming the
        // ordinary queue. Keep its quantity and ring-buffer head, but remap the
        // visual materials to the remaining construction before finite use resumes.
        if (!active && level != null)
        {
            for (int i = 0; i < storedCount; i++)
            {
                BlockType type;
                if (!level.TryGetSupplyType(i, out type))
                    break;
                carried[(head + i) % carried.Length] = type;
            }
        }

        Refresh();
        Changed?.Invoke();
    }

    public void RefreshInfinitePreview()
    {
        if (InfiniteBlocksActive)
            Refresh();
    }

    private static void Release(Mesh mesh)
    {
        if (Application.isPlaying)
            Destroy(mesh);
        else
            DestroyImmediate(mesh);
    }

    private void OnDestroy()
    {
        if (strength != null)
            strength.Changed -= CapacityChanged;
        if (combined != null)
            Release(combined);
        if (typeMeshes != null)
            foreach (var mesh in typeMeshes)
                if (mesh != null)
                    Release(mesh);
    }
}
