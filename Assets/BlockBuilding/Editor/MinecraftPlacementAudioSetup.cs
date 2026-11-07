using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MinecraftPlacementAudioSetup
{
    [MenuItem("Tools/Block Building/Install Minecraft Placement Audio")]
    public static void Install()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Exit Play mode first.");
        var p = UnityEngine.Object.FindFirstObjectByType<BlockPlacementAudio>();
        Apply(p);
        EditorUtility.SetDirty(p);
        EditorSceneManager.MarkSceneDirty(p.gameObject.scene);
        EditorSceneManager.SaveScene(p.gameObject.scene);
        AssetDatabase.SaveAssets();
    }

    public static void Apply(BlockPlacementAudio p)
    {
        p.stone = Bank("stone");
        p.wood = Bank("wood");
        p.glass = p.stone;
        p.soft = Bank("grass");
        p.sand = Bank("sand");
        p.dirt = Bank("gravel");
        p.snow = Bank("snow");
        p.cloth = Bank("cloth");
        p.volume = .45f;
    }

    private static AudioClip[] Bank(string name)
    {
        var clips = new AudioClip[4];
        for (int i = 0; i < 4; i++)
        {
            string path = "Assets/BlockBuilding/Audio/MinecraftPlacement/" + name + (i + 1) + ".ogg";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.SaveAndReimport();
            clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clips[i] == null)
                throw new System.Exception("Missing " + path);
        }

        return clips;
    }
}
