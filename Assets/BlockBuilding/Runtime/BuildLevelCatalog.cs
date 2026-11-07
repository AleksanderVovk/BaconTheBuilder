using System;
using UnityEngine;

[Serializable]
public class BuildLevelEntry
{
    [Header("Identity"), Tooltip("Stable PlayerPrefs key. Keep unchanged after release.")]
    public string id = "level";

    [Header("Name"), Tooltip("Menu title.")]
    public string title = "Level";

    [Header("Data"), Tooltip("Editable construction data for this level.")]
    public BuildSiteData data;

    [Header("Icon"), Tooltip("Transparent render of the actual construction.")]
    public Sprite icon;

    [Header("Access"), Tooltip("Create a permanent switchback staircase for this level.")]
    public bool stairs;

    [Header("Stair position"), Tooltip("World origin of the staircase, at ground level.")]
    public Vector3 stairOrigin = new Vector3(18, 0, 18);

    [Header("Stair height"), Tooltip("Height of the top landing, in world units."), Min(0)]
    public float stairHeight = 30;
    public int BlockCount
    {
        get
        {
            return BuildFoundation.WorkCount(data);
        }
    }
}

[CreateAssetMenu(menuName = "Block Building/Level Catalog", fileName = "BuildLevelCatalog")]
public class BuildLevelCatalog : ScriptableObject
{
    [Header("Levels"), Tooltip("Display order; IDs must be unique. No scene changes are required.")]
    public BuildLevelEntry[] levels = new BuildLevelEntry[0];
}
