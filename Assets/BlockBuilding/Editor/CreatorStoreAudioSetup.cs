using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreatorStoreAudioSetup
{
    public static void Install()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Exit Play mode first");
        var audio = UnityEngine.Object.FindFirstObjectByType<StockGameAudio>();
        Apply(audio);
        EditorSceneManager.MarkSceneDirty(audio.gameObject.scene);
        EditorSceneManager.SaveScene(audio.gameObject.scene);
        AssetDatabase.SaveAssets();
    }

    public static void Apply(StockGameAudio audio)
    {
        audio.clips[0] = Clip("ClickSfx");
        audio.clips[5] = Clip("SuccessSfx");
        audio.errorClip = Clip("ErrorSfx");
        audio.purchaseClip = Clip("PurchaseSfx");
        audio.buttons = audio.session.gameplayHUD.GetComponentInParent<Canvas>().GetComponentsInChildren<UnityEngine.UI.Button>(true);
        var strength = audio.session.runtime.carrier.strength;
        strength.levelUpClip = audio.clips[5];
        EditorUtility.SetDirty(strength);
        EditorUtility.SetDirty(audio);
    }

    private static AudioClip Clip(string name)
    {
        string path = "Assets/Sounds/" + name + " - Creator Store.ogg";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (AudioImporter)AssetImporter.GetAtPath(path);
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
}
