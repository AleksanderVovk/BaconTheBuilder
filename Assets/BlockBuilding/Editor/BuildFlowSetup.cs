using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildFlowSetup
{
    [MenuItem("Tools/Block Building/Optimize All Level Routes")]
    public static string OptimizeRoutes()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play mode before editing level routes.");
        var session = UnityEngine.Object.FindFirstObjectByType<BuildLevelSession>();
        var report = new StringBuilder();
        foreach (var entry in session.catalog.levels)
        {
            float before = 0, after = 0;
            foreach (var site in entry.data.level.sites)
                before += BuildRoutePlanner.HorizontalLength(site, entry.data.level.blockSize);
            Undo.RecordObject(entry.data, "Optimize building walk");
            BuildRoutePlanner.Optimize(entry.data.level);
            entry.data.level.Validate();
            foreach (var site in entry.data.level.sites)
                after += BuildRoutePlanner.HorizontalLength(site, entry.data.level.blockSize);
            EditorUtility.SetDirty(entry.data);
            File.WriteAllText("Assets/BlockBuilding/Generated/Campaign/" + entry.id + ".json", entry.data.ToJson());
            report.AppendLine(entry.id
                + ": "
                + before.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)
                + "m -> "
                + after.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)
                + "m");
        }

        Directory.CreateDirectory("BlockBuildingBackup");
        File.WriteAllText("BlockBuildingBackup/BuildRoutes.txt", report.ToString());
        AssetDatabase.SaveAssets();
        return report.ToString();
    }

    [MenuItem("Tools/Block Building/Install Automatic Build Flow")]
    public static void Install()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play mode first.");
        var session = UnityEngine.Object.FindFirstObjectByType<BuildLevelSession>();
        var ui = UnityEngine.Object.FindFirstObjectByType<BuildLevelMenu>();
        var pickup = session.runtime.pickupTransform.GetComponent<BlockPickupZone>();
        pickup.automaticPickup = true;
        EditorUtility.SetDirty(pickup);
        var audio = session.runtime.GetComponent<BlockPlacementAudio>();
        if (audio == null)
            audio = session.runtime.gameObject.AddComponent<BlockPlacementAudio>();
        audio.runtime = session.runtime;
        const string folder = "Assets/BlockBuilding/Audio/";
        audio.stone = new[]
        {
            Clip(folder + "PlaceStone1.wav"),
            Clip(folder + "PlaceStone2.wav")
        };
        audio.wood = new[]
        {
            Clip(folder + "PlaceWood1.wav"),
            Clip(folder + "PlaceWood2.wav")
        };
        audio.glass = new[]
        {
            Clip(folder + "PlaceGlass.wav")
        };
        audio.soft = new[]
        {
            Clip(folder + "PlaceSoft.wav")
        };
        EditorUtility.SetDirty(audio);
        ui.result.GetComponent<UnityEngine.UI.Image>().color = new Color(0.025f, 0.045f, 0.075f, 1);
        var result = ui.result.transform;
        var box = result.Find("ResultBox");
        var imageTransform = box.Find("BuildingPreview");
        if (imageTransform == null)
        {
            var go = new GameObject("BuildingPreview", typeof(RectTransform));
            go.transform.SetParent(box, false);
            imageTransform = go.transform;
        }

        var image = imageTransform.GetComponent<UnityEngine.UI.RawImage>();
        if (image == null)
            image = imageTransform.gameObject.AddComponent<UnityEngine.UI.RawImage>();
        image.raycastTarget = false;
        image.color = Color.white;
        var preview = result.GetComponent<CompletedBuildingPreview>();
        if (preview == null)
            preview = result.gameObject.AddComponent<CompletedBuildingPreview>();
        preview.runtime = session.runtime;
        preview.image = image;
        var layout = result.GetComponent<CompletionPanelLayout>();
        if (layout == null)
            layout = result.gameObject.AddComponent<CompletionPanelLayout>();
        layout.box = (RectTransform)box;
        layout.preview = (RectTransform)imageTransform;
        layout.text = ui.resultText.rectTransform;
        layout.continueButton = (RectTransform)ui.again.transform;
        layout.menuButton = (RectTransform)ui.levels.transform;
        ui.again.GetComponentInChildren<TMP_Text>(true).text = "ПРОДОЛЖИТЬ ОСМОТР";
        ui.levels.GetComponentInChildren<TMP_Text>(true).text = "В МЕНЮ УРОВНЕЙ";
        ui.resultText.fontSize = 40;
        ui.resultText.fontSizeMin = 22;
        ui.resultText.fontSizeMax = 40;
        ui.resultText.enableAutoSizing = true;
        foreach (var label in ui.again.GetComponentsInChildren<TMP_Text>(true))
        {
            var r = label.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        foreach (var label in ui.levels.GetComponentsInChildren<TMP_Text>(true))
        {
            var r = label.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        // Invoke the layout once in edit mode too, without rebuilding the rest of the menu.
        layout.SendMessage("OnRectTransformDimensionsChange");
        foreach (var component in new UnityEngine.Object[]
        {
            ui,
            preview,
            layout
        }

        )
            EditorUtility.SetDirty(component);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(session.gameObject.scene);
        EditorSceneManager.SaveScene(session.gameObject.scene);
    }

    private static AudioClip Clip(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (AudioImporter)AssetImporter.GetAtPath(path);
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        settings.preloadAudioData = true;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
}
