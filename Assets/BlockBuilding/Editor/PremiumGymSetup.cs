using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PremiumGymSetup
{
    [MenuItem("Tools/Block Building/Install Premium Gyms")]
    public static string Install()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play mode first.");
        var session = UnityEngine.Object.FindFirstObjectByType<BuildLevelSession>();
        var training = session.training;
        var hub = training.GetComponent<GymEquipmentHub>();
        if (hub == null)
            hub = training.gameObject.AddComponent<GymEquipmentHub>();
        hub.training = training;
        hub.session = session;
        hub.paymentsConfigured = false;
        hub.equipment = new GymEquipment[6];
        var standard = training.GetComponent<GymEquipment>();
        if (standard == null)
            standard = training.gameObject.AddComponent<GymEquipment>();
        standard.equipmentId = "standard";
        standard.title = "Стандартный";
        standard.multiplier = 1;
        standard.pose = training.transform.Find("LyingPose");
        standard.bar = training.transform.Find("Barbell");
        hub.equipment[0] = standard;
        string[] models =
        {
            "Industrial",
            "Neon",
            "Frost",
            "Royal",
            "Cosmic"
        };
        string[] names =
        {
            "Индустриальный",
            "Неоновый",
            "Ледяной",
            "Королевский",
            "Космический"
        };
        int[] multipliers =
        {
            2,
            4,
            6,
            8,
            10
        };
        Vector3[] positions =
        {
            new Vector3(5, 0, 0),
            new Vector3(10, 0, 0),
            new Vector3(0, 0, -5),
            new Vector3(5, 0, -5),
            new Vector3(10, 0, -5)
        };
        Directory.CreateDirectory("Assets/BlockBuilding/Models/PremiumGyms/Materials");
        for (int i = 0; i < 5; i++)
        {
            string path = "Assets/BlockBuilding/Models/PremiumGyms/" + models[i] + ".fbx";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.importAnimation = false;
            importer.isReadable = false;
            importer.SaveAndReimport();
            var existing = training.transform.Find("Premium_" + models[i]);
            GameObject root;
            if (existing == null)
            {
                root = new GameObject("Premium_" + models[i]);
                root.transform.SetParent(training.transform, false);
            }
            else
                root = existing.gameObject;
            root.transform.localPosition = positions[i];
            var station = root.GetComponent<GymEquipment>();
            if (station == null)
                station = root.AddComponent<GymEquipment>();
            station.equipmentId = "gym_" + models[i].ToLowerInvariant();
            station.title = names[i];
            station.multiplier = multipliers[i];
            station.productId = station.equipmentId + "_x" + multipliers[i];
            if (root.transform.Find("Model") == null)
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                model.name = "Model";
                model.transform.SetParent(root.transform, false);
                foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
                {
                    var materials = renderer.sharedMaterials;
                    for (int j = 0; j < materials.Length; j++)
                    {
                        var source = materials[j];
                        string matPath = "Assets/BlockBuilding/Models/PremiumGyms/Materials/"
                            + source.name.Replace(" (Instance)", "")
                            + ".mat";
                        var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                        if (material == null)
                        {
                            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                            material.color = source.color;
                            material.SetFloat("_Metallic", 0.35f);
                            material.SetFloat("_Smoothness", 0.45f);
                            if (source.name.StartsWith("Glow_"))
                            {
                                material.EnableKeyword("_EMISSION");
                                material.SetColor("_EmissionColor", source.color * 1.8f);
                            }

                            AssetDatabase.CreateAsset(material, matPath);
                        }

                        materials[j] = material;
                    }

                    renderer.sharedMaterials = materials;
                }

                foreach (var node in model.GetComponentsInChildren<Transform>())
                    if (node.name.StartsWith("Barbell", StringComparison.Ordinal))
                        station.bar = node;
            }

            if (station.bar == null)
                foreach (var node in root.GetComponentsInChildren<Transform>())
                    if (node.name.StartsWith("Barbell", StringComparison.Ordinal))
                        station.bar = node;
            var pose = root.transform.Find("LyingPose");
            if (pose == null)
            {
                pose = new GameObject("LyingPose").transform;
                pose.SetParent(root.transform, false);
            }

            pose.localPosition = standard.pose.localPosition;
            pose.localRotation = standard.pose.localRotation;
            station.pose = pose;
            var label = root.transform.Find("GymLabel");
            if (label == null)
            {
                label = new GameObject("GymLabel").transform;
                label.SetParent(root.transform, false);
            }

            label.localPosition = new Vector3(0, 2.7f, 0);
            label.localRotation = Quaternion.identity;
            var text = label.GetComponent<TextMeshPro>();
            if (text == null)
                text = label.gameObject.AddComponent<TextMeshPro>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/BlockBuilding/UI/BlockUIFont.asset");
            text.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/BlockBuilding/UI/BlockUIText.mat");
            text.text = names[i] + "\nx" + multipliers[i] + " / Donate";
            text.fontSize = 3;
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(4, 1);
            text.raycastTarget = false;
            hub.equipment[i + 1] = station;
            EditorUtility.SetDirty(station);
        }

        var interaction = session.runtime.GetComponent<MobileBuildInteraction>();
        interaction.interaction.GetComponent<RectTransform>().sizeDelta = new Vector2(340, 76);
        interaction.caption.fontSizeMax = 22;
        EditorUtility.SetDirty(standard);
        EditorUtility.SetDirty(hub);
        EditorUtility.SetDirty(training);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(session.gameObject.scene);
        EditorSceneManager.SaveScene(session.gameObject.scene);
        return "Six stations configured; five imported Blender models; payments disabled pending platform catalog.";
    }
}
