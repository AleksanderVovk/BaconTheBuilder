using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StockAudioSetup
{
    private const string Folder = "Assets/BlockBuilding/Audio/Natural/";
    [MenuItem("Tools/Block Building/Install Stock Audio")]
    public static string Install()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play mode first.");
        AssetDatabase.Refresh();
        var session = UnityEngine.Object.FindFirstObjectByType<BuildLevelSession>();
        var placement = session.runtime.GetComponent<BlockPlacementAudio>();
        placement.stone = new[]
        {
            Clip("impactGeneric_light_002"),
            Clip("impactGeneric_light_003")
        };
        placement.wood = new[]
        {
            Clip("impactWood_medium_000"),
            Clip("impactWood_medium_001")
        };
        placement.glass = new[]
        {
            Clip("impactGlass_light_000"),
            Clip("impactGlass_light_001")
        };
        placement.soft = new[]
        {
            Clip("impactSoft_medium_001")
        };
        placement.volume = .3f;
        if (System.IO.Directory.Exists("Assets/BlockBuilding/Audio/MinecraftPlacement"))
            MinecraftPlacementAudioSetup.Apply(placement);
        var source = session.runtime.carrier.player.GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0;
        source.volume = .6f;
        var controller = session.runtime.carrier.player.GetComponent<PlayerController>();
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("_jumpUpClip").objectReferenceValue = Clip("cloth3");
        serialized.FindProperty("_audioSource").objectReferenceValue = source;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        var strength = session.runtime.carrier.strength;
        strength.levelAudio = source;
        strength.levelUpClip = Clip("impactBell_heavy_000");
        strength.extendLevels = true;
        var audio = session.runtime.GetComponent<StockGameAudio>();
        if (audio == null)
            audio = session.runtime.gameObject.AddComponent<StockGameAudio>();
        audio.session = session;
        if (audio.effects == null)
        {
            audio.effects = session.runtime.gameObject.AddComponent<AudioSource>();
            audio.effects.playOnAwake = false;
            audio.effects.spatialBlend = 0;
            audio.effects.volume = .65f;
        }

        if (audio.uiVoice == null)
        {
            audio.uiVoice = session.runtime.gameObject.AddComponent<AudioSource>();
            audio.uiVoice.playOnAwake = false;
            audio.uiVoice.spatialBlend = 0;
            audio.uiVoice.volume = .65f;
        }

        audio.clips = new[]
        {
            Clip("metalClick"),
            Clip("handleSmallLeather2"),
            Clip("metalLatch"),
            Clip("cloth1"),
            Clip("impactMetal_light_002"),
            Clip("impactBell_heavy_002"),
            Clip("handleCoins2")
        };
        audio.buttons = session.gameplayHUD.GetComponentInParent<Canvas>().GetComponentsInChildren<UnityEngine.UI.Button>(true);
        if (System.IO.File.Exists("Assets/Sounds/ClickSfx - Creator Store.ogg"))
            CreatorStoreAudioSetup.Apply(audio);
        foreach (var obj in new UnityEngine.Object[]
        {
            audio,
            placement,
            strength,
            source,
            controller
        }

        )
            EditorUtility.SetDirty(obj);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(session.gameObject.scene);
        EditorSceneManager.SaveScene(session.gameObject.scene);
        return "Downloaded Kenney clips assigned; extended strength progression enabled.";
    }

    private static AudioClip Clip(string name)
    {
        string path = Folder + name + ".ogg";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (AudioImporter)AssetImporter.GetAtPath(path);
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
}
