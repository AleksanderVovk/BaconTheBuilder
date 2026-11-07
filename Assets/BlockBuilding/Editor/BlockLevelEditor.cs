using System;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class BlockLevelEditor : EditorWindow
{
    private BuildSiteData data;
    private int siteIndex, layer, order, gridX = -3, gridZ = -3;
    private Vector3Int coordinate;
    private BlockType selectedType = BlockType.OakPlanks;
    private VisualElement body, grid;
    private Label status;
    [MenuItem("Tools/Block Building/Level Editor")]
    public static void Open()
    {
        GetWindow<BlockLevelEditor>("Block Level Editor");
    }

    public void CreateGUI()
    {
        if (data == null)
            data = AssetDatabase.LoadAssetAtPath<BuildSiteData>("Assets/BlockBuilding/Generated/LobbyBlockLevel.asset");
        rootVisualElement.Clear();
        var scroll = new ScrollView();
        rootVisualElement.Add(scroll);
        var field = new ObjectField("Level asset")
        {
            objectType = typeof(BuildSiteData),
            value = data
        };
        field.RegisterValueChangedCallback(e =>
        {
            data = e.newValue as BuildSiteData;
            siteIndex = 0;
            Rebuild();
        });
        scroll.Add(field);
        scroll.Add(new Button(CreateLevel) { text = "Create Level Asset" });
        body = new VisualElement();
        scroll.Add(body);
        Rebuild();
    }

    private BlockStructure Site
    {
        get
        {
            return data.level.sites[siteIndex];
        }
    }

    private void Rebuild()
    {
        if (body == null)
            return;
        body.Clear();
        if (data == null)
        {
            body.Add(new Label("Select or create a level asset."));
            return;
        }

        if (data.level.sites.Count == 0)
            data.level.sites.Add(new BlockStructure());
        siteIndex = Mathf.Clamp(siteIndex, 0, data.level.sites.Count - 1);
        var siteField = new IntegerField("Structure index")
        {
            value = siteIndex
        };
        siteField.RegisterValueChangedCallback(e =>
        {
            siteIndex = Mathf.Clamp(e.newValue, 0, data.level.sites.Count - 1);
            Rebuild();
        });
        body.Add(siteField);
        body.Add(new Label(Site.id + " | " + Site.blocks.Count + " blocks | structures: " + data.level.sites.Count));
        body.Add(new Button(() => Edit(() =>
        {
            data.level.sites.Add(new BlockStructure { id = "Building" + Guid.NewGuid().ToString("N").Substring(0, 6) });
            siteIndex = data.level.sites.Count - 1;
        })) { text = "Add Structure" });
        body.Add(new Button(() =>
        {
            if (data.level.sites.Count > 1)
                Edit(() =>
                {
                    data.level.sites.RemoveAt(siteIndex);
                    siteIndex = 0;
                });
        }) { text = "Remove Structure" });
        var serialized = new SerializedObject(data);
        var settings = new Foldout
        {
            text = "Level settings / positions / prefab / material",
            value = false
        };
        settings.Add(new PropertyField(serialized.FindProperty("blockPrefab")));
        settings.Add(new PropertyField(serialized.FindProperty("blockMaterial")));
        settings.Add(new PropertyField(serialized.FindProperty("useAtlas")));
        var levelProperty = serialized.FindProperty("level");
        foreach (string property in new[]
        {
            "blockSize",
            "pickupPoint",
            "trainingPoint"
        }

        )
            settings.Add(new PropertyField(levelProperty.FindPropertyRelative(property)));
        var siteProperty = levelProperty.FindPropertyRelative("sites").GetArrayElementAtIndex(siteIndex);
        foreach (string property in new[]
        {
            "id",
            "origin",
            "yaw",
            "deliveryPoint"
        }

        )
            settings.Add(new PropertyField(siteProperty.FindPropertyRelative(property)));
        settings.Bind(serialized);
        body.Add(settings);
        var typeField = new EnumField("Block texture", selectedType);
        typeField.RegisterValueChangedCallback(e => selectedType = (BlockType)e.newValue);
        body.Add(typeField);
        body.Add(new Button(() => Edit(() =>
        {
            var cell = Site.blocks.Find(b => b.position == coordinate);
            if (cell != null)
                cell.type = selectedType;
        })) { text = "Paint Texture at Cell" });
        var coords = new Vector3IntField("Cell x / y / z")
        {
            value = coordinate
        };
        coords.RegisterValueChangedCallback(e =>
        {
            coordinate = e.newValue;
        });
        body.Add(coords);
        var orderField = new IntegerField("Order within layer")
        {
            value = order
        };
        orderField.RegisterValueChangedCallback(e => order = e.newValue);
        body.Add(orderField);
        body.Add(new Button(() => Add(coordinate)) { text = "Add Block" });
        body.Add(new Button(() => Remove(coordinate)) { text = "Remove Block" });
        body.Add(new Button(() =>
        {
            if (EditorUtility.DisplayDialog("Clear structure", "Remove all cells from this structure?", "Clear", "Cancel"))
                Edit(() => Site.blocks.Clear());
        }) { text = "Clear" });
        var layerField = new IntegerField("Paint layer Y")
        {
            value = layer
        };
        layerField.RegisterValueChangedCallback(e =>
        {
            layer = Mathf.Max(0, e.newValue);
            DrawGrid();
        });
        body.Add(layerField);
        var xField = new IntegerField("Grid start X")
        {
            value = gridX
        };
        xField.RegisterValueChangedCallback(e =>
        {
            gridX = e.newValue;
            DrawGrid();
        });
        body.Add(xField);
        var zField = new IntegerField("Grid start Z")
        {
            value = gridZ
        };
        zField.RegisterValueChangedCallback(e =>
        {
            gridZ = e.newValue;
            DrawGrid();
        });
        body.Add(zField);
        body.Add(new Label("Click a cell: add/remove. Each filled cell displays its order."));
        grid = new VisualElement();
        body.Add(grid);
        DrawGrid();
        body.Add(new Button(() => Edit(() => Site.blocks.Sort(BlockLevelDefinition.Compare))) { text = "Sort Bottom → Top" });
        body.Add(new Button(SaveJson) { text = "Save JSON" });
        body.Add(new Button(LoadJson) { text = "Load JSON" });
        body.Add(new Button(() =>
        {
            try
            {
                data.level.Validate();
                AssetDatabase.SaveAssets();
                BlockBuildingInstaller.ApplyLevel(data);
                status.text = "Applied to existing scene. Save the scene to keep changes.";
            }
            catch (Exception e)
            {
                status.text = e.Message;
            }
        }) { text = "Apply Positions / Visuals to Scene" });
        body.Add(new Button(() =>
        {
            try
            {
                data.level.Validate();
                status.text = "Valid. Layers are sorted at runtime; overhangs allowed.";
            }
            catch (Exception e)
            {
                status.text = e.Message;
            }
        }) { text = "Validate" });
        status = new Label();
        body.Add(status);
    }

    private void DrawGrid()
    {
        if (grid == null || data == null)
            return;
        grid.Clear();
        for (int z = 0; z < 9; z++)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            grid.Add(row);
            for (int x = 0; x < 9; x++)
            {
                var p = new Vector3Int(gridX + x, layer, gridZ + z);
                var cell = Site.blocks.Find(b => b.position == p);
                var button = new Button(() =>
                {
                    if (Site.blocks.Exists(b => b.position == p))
                        Remove(p);
                    else
                        Add(p);
                });
                button.text = cell == null ? "+" : cell.order.ToString();
                button.tooltip = p.ToString() + (cell == null ? "" : " / " + cell.type);
                button.style.width = 38;
                button.style.height = 28;
                button.style.backgroundColor = cell == null ? new Color(0.18f, 0.2f, 0.23f) : new Color(0.1f, 0.55f, 0.45f);
                row.Add(button);
            }
        }

        SceneView.RepaintAll();
    }

    private void Edit(Action action)
    {
        Undo.RecordObject(data, "Edit block level");
        action();
        EditorUtility.SetDirty(data);
        Rebuild();
        SceneView.RepaintAll();
    }

    private void Add(Vector3Int p)
    {
        if (p.y < 0 || Site.blocks.Exists(b => b.position == p))
            return;
        Edit(() =>
        {
            Site.blocks.Add(new BlockCell { position = p, order = order, type = selectedType });
            order++;
        });
    }

    private void Remove(Vector3Int p)
    {
        Edit(() => Site.blocks.RemoveAll(b => b.position == p));
    }

    private void CreateLevel()
    {
        string path = EditorUtility.SaveFilePanelInProject("Create block level", "BlockLevel", "asset", "Choose asset path");
        if (string.IsNullOrEmpty(path))
            return;
        data = CreateInstance<BuildSiteData>();
        data.level = BlockLevelDefinition.Pyramid();
        data.blockMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/BlockBuilding/Generated/VoxelAtlas.mat");
        data.useAtlas = data.blockMaterial != null;
        if (data.blockMaterial == null)
            data.blockMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/BlockBuilding/Generated/Blocks.mat");
        data.blockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BlockBuilding/Generated/BlockVisual.prefab");
        AssetDatabase.CreateAsset(data, path);
        siteIndex = 0;
        CreateGUI();
    }

    private void SaveJson()
    {
        string path = EditorUtility.SaveFilePanel("Save readable JSON", Application.dataPath, "BlockLevel", "json");
        if (string.IsNullOrEmpty(path))
            return;
        try
        {
            File.WriteAllText(path, data.ToJson());
            AssetDatabase.Refresh();
            status.text = "Saved: " + path;
        }
        catch (Exception e)
        {
            status.text = e.Message;
        }
    }

    private void LoadJson()
    {
        string path = EditorUtility.OpenFilePanel("Load JSON", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path))
            return;
        try
        {
            string json = File.ReadAllText(path);
            Undo.RecordObject(data, "Load block JSON");
            data.LoadJson(json);
            EditorUtility.SetDirty(data);
            siteIndex = 0;
            Rebuild();
        }
        catch (Exception e)
        {
            status.text = e.Message;
        }
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    private static void DrawPreview(BlockLevelRuntime runtime, GizmoType type)
    {
        if (Application.isPlaying || runtime.data == null)
            return;
        var level = runtime.data.level;
        foreach (var site in level.sites)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.9f, 0.5f);
            Gizmos.matrix = Matrix4x4.TRS(site.origin, Quaternion.Euler(0, site.yaw, 0), Vector3.one);
            int count = Mathf.Min(5000, site.blocks.Count);
            for (int i = 0; i < count; i++)
                Gizmos.DrawWireCube(
                    ((Vector3)site.blocks[i].position + Vector3.one * 0.5f) * level.blockSize,
                    Vector3.one * level.blockSize * 0.97f
                );
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(site.deliveryPoint, 1.5f);
            Handles.Label(site.deliveryPoint + Vector3.up, site.id + " / Delivery");
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
}
