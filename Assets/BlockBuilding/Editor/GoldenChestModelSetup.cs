using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class GoldenChestModelSetup
{
    private const string Folder = "Assets/BlockBuilding/Models/ActivityChestGolden";
    [MenuItem("Tools/Block Building/Apply Blender Golden Chest")]
    public static string Apply()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Stop Play Mode first.");
        string path = Folder + "/GoldenActivityChest.fbx";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        importer.importAnimation = false;
        importer.animationType = ModelImporterAnimationType.None;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.SaveAndReimport();
        if (!AssetDatabase.IsValidFolder(Folder + "/Materials"))
            AssetDatabase.CreateFolder(Folder, "Materials");
        var chest = Object.FindFirstObjectByType<ActivityChestReward>();
        var animation = chest.GetComponent<ActivityChestAnimation>();
        var previous = chest.transform.Find("BlenderGoldenChest");
        if (previous != null)
            Object.DestroyImmediate(previous.gameObject);
        var oldPivot = chest.transform.Find("BlenderLidPivot");
        if (oldPivot != null)
            Object.DestroyImmediate(oldPivot.gameObject);
        // Keep the original visual disabled for easy comparison; gameplay components stay in place.
        foreach (Transform child in chest.transform)
            if (child.name.StartsWith("Body") || child.name.StartsWith("TreasureInterior") || child.name == "LidHinge")
                child.gameObject.SetActive(false);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, chest.transform);
        model.name = "BlenderGoldenChest";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = prefab.transform.localRotation;
        model.transform.localScale = prefab.transform.localScale;
        PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        Transform lid = null;
        foreach (var t in model.GetComponentsInChildren<Transform>())
            if (t.name == "ChestLid")
                lid = t;
        if (lid == null)
            throw new System.InvalidOperationException("Blender lid not found.");
        if (chest.transform.InverseTransformPoint(lid.position).z > 0)
            model.transform.localRotation = Quaternion.Euler(0, 180, 0) * model.transform.localRotation;
        foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
        {
            var mats = renderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                var source = mats[i];
                string name = source.name;
                string materialPath = Folder + "/Materials/" + name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(material, materialPath);
                }

                Color color = source.color;
                material.SetColor("_BaseColor", color);
                bool gold = name.Contains("Gold");
                material.SetFloat("_Metallic", gold ? .18f : 0);
                material.SetFloat("_Smoothness", gold ? .3f : .18f);
                if (gold)
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", color * .06f);
                }

                EditorUtility.SetDirty(material);
                mats[i] = material;
            }

            renderer.sharedMaterials = mats;
        }

        var pivot = new GameObject("BlenderLidPivot").transform;
        pivot.SetParent(chest.transform, false);
        pivot.position = lid.position;
        pivot.localRotation = Quaternion.identity;
        lid.SetParent(pivot, true);
        animation.lid = pivot;
        var colliders = chest.GetComponents<BoxCollider>();
        colliders[0].center = new Vector3(0, 1.4f, 0);
        colliders[0].size = new Vector3(3.8f, 2.8f, 2.7f);
        colliders[1].center = new Vector3(0, .1f, 0);
        colliders[1].size = new Vector3(4.65f, .2f, 4.1f);
        chest.GetComponent<ActivityChestInteraction>().worldLabel.transform.localPosition = new Vector3(0, 5.2f, 0);
        animation.celebration.transform.localPosition = new Vector3(0, 1.8f, 0);
        for (int i = 0; i < animation.sparks.Length; i++)
            animation.sparks[i].localPosition = new Vector3((i % 2 == 0 ? -1 : 1) * 2.12f, 1.65f + (i / 2) * .7f, 0);
        EditorUtility.SetDirty(animation);
        EditorUtility.SetDirty(chest);
        EditorUtility.SetDirty(colliders[0]);
        EditorUtility.SetDirty(colliders[1]);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(chest.gameObject.scene);
        EditorSceneManager.SaveScene(chest.gameObject.scene);
        return "Blender FBX applied. Lid hinge local="
            + pivot.localPosition
            + "; gameplay, saves, interaction and reward retained.";
    }
}
