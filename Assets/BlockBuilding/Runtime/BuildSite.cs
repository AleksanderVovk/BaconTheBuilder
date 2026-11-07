using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildSite : MonoBehaviour
{
    [Header("Renderer"), Tooltip("Chunk renderer for this structure.")]
    public BuildSiteBuilder builder;

    [Header("Ghost"), Tooltip("One reusable visual for the next cell.")]
    public Transform ghost;

    [Header("Typed preview"), Tooltip("Carrier providing cached typed ghost meshes.")]
    public PlayerBlockCarrier visualSource;

    private readonly List<BlockCell> ordered = new List<BlockCell>();
    private float size;
    private int index;
    private bool activeSite;
    public event Action Changed;
    public event Action<BlockType, Vector3> BlockPlaced;
    public Vector3Int LastBuiltCell { get; private set; }

    public bool IsComplete
    {
        get
        {
            return index >= ordered.Count;
        }
    }

    public int BuiltCount
    {
        get
        {
            return index;
        }
    }

    public int FoundationCount { get; private set; }

    public bool IsActiveSite
    {
        get
        {
            return activeSite;
        }
    }

    public float BlockSize
    {
        get
        {
            return size;
        }
    }

    public int TotalCount
    {
        get
        {
            return ordered.Count;
        }
    }

    public Vector3 NextWorldPosition
    {
        get
        {
            return IsComplete
                ? transform.position
                : transform.TransformPoint(((Vector3)ordered[index].position + Vector3.one * 0.5f) * size);
        }
    }

    public void Initialize(BlockStructure data, float cellSize)
    {
        size = cellSize;
        ordered.AddRange(data.blocks);
        ordered.Sort(BlockLevelDefinition.Compare);
        FoundationCount = BuildFoundation.Count(data);
        builder.Initialize(ordered, size);
        while (index < FoundationCount)
        {
            LastBuiltCell = ordered[index].position;
            builder.AddBlock(LastBuiltCell);
            index++;
        }

        if (FoundationCount > 0)
            builder.Complete();
        UpdateGhost();
    }

    // Loading a checkpoint must not emit placement/reward events.
    public void RestoreBuiltCount(int count)
    {
        if (index != FoundationCount)
            throw new InvalidOperationException("Restore requires a freshly loaded site.");
        int target = Mathf.Clamp(count, FoundationCount, ordered.Count);
        while (index < target)
        {
            LastBuiltCell = ordered[index].position;
            builder.AddBlock(LastBuiltCell);
            index++;
        }

        if (target > 0)
            builder.Complete();
        UpdateGhost();
    }

    public bool TryGetPendingType(int offset, out BlockType type)
    {
        int at = index + offset;
        type = BlockType.Cobblestone;
        if (offset < 0 || at >= ordered.Count)
            return false;
        type = ordered[at].type;
        return true;
    }

    public float HorizontalDistanceToNextBlockSquared(Vector3 worldPosition)
    {
        if (IsComplete)
            return float.PositiveInfinity;
        Vector3 p = transform.InverseTransformPoint(worldPosition);
        Vector3 min = (Vector3)ordered[index].position * size;
        float dx = Mathf.Max(0, Mathf.Max(min.x - p.x, p.x - min.x - size));
        float dz = Mathf.Max(0, Mathf.Max(min.z - p.z, p.z - min.z - size));
        // Work in the rotated local grid, then convert the offset back to world
        // units. The range is measured from the cell's surface, not its center.
        Vector3 offset = transform.TransformVector(new Vector3(dx, 0, dz));
        return offset.sqrMagnitude;
    }

    public void SetActiveSite(bool value)
    {
        activeSite = value;
        UpdateGhost();
    }

    public bool TryBuild(PlayerBlockCarrier carrier)
    {
        if (!activeSite || IsComplete)
            return false;
        BlockType placedType = ordered[index].type;
        Vector3 placedPosition = NextWorldPosition;
        if (!carrier.TryConsume(placedType))
            return false;
        return PlaceNext();
    }

    public bool TryBuildGranted()
    {
        if (!activeSite || IsComplete)
            return false;
        return PlaceNext();
    }

    private bool PlaceNext()
    {
        BlockType placedType = ordered[index].type;
        Vector3 placedPosition = NextWorldPosition;
        LastBuiltCell = ordered[index].position;
        builder.AddBlock(LastBuiltCell);
        index++;
        if (IsComplete)
            builder.Complete();
        UpdateGhost();
        BlockPlaced?.Invoke(placedType, placedPosition);
        Changed?.Invoke();
        return true;
    }

    private void UpdateGhost()
    {
        if (ghost == null)
            return;
        ghost.gameObject.SetActive(activeSite && !IsComplete);
        if (!IsComplete)
        {
            if (visualSource != null && visualSource.useAtlas)
                ghost.GetComponent<MeshFilter>().sharedMesh = visualSource.GetVisualMesh(ordered[index].type);
            ghost.position = NextWorldPosition;
            ghost.localScale = Vector3.one * size * 1.02f;
        }
    }
}
