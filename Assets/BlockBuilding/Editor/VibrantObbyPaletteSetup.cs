using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class VibrantObbyPaletteSetup
{
    [MenuItem("Tools/Block Building/Apply Vibrant Obby Palette")]
    public static string Apply()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Stop Play Mode first.");
        if (UniversalRenderPipeline.asset == null)
            throw new System.InvalidOperationException("URP required.");
        var volume = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).First(v => v.isGlobal);
        const string path = "Assets/BlockBuilding/Generated/VibrantLobbyProfile.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            foreach (var original in volume.sharedProfile.components)
            {
                var copy = Object.Instantiate(original);
                copy.name = original.name;
                profile.components.Add(copy);
                AssetDatabase.AddObjectToAsset(copy, profile);
            }
        }

        if (!profile.TryGet<ColorAdjustments>(out var color))
        {
            color = profile.Add<ColorAdjustments>(false);
            AssetDatabase.AddObjectToAsset(color, profile);
        }

        color.active = true;
        color.postExposure.Override(.05f);
        color.saturation.Override(28);
        color.contrast.Override(7);
        color.colorFilter.Override(Color.white);
        volume.sharedProfile = profile;
        volume.weight = 1;
        foreach (var id in AssetDatabase.FindAssets("t:Material", new[] { "Assets/BlockBuilding" }))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id));
            if (material == null || material.shader.name != "BlockBuilding/Pixel Atlas")
                continue;
            material.SetFloat("_Brightness", 1.25f);
            material.SetFloat("_Saturation", 1.18f);
            EditorUtility.SetDirty(material);
        }

        var camera = Camera.main;
        var cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;
        cameraData.volumeLayerMask |= 1 << volume.gameObject.layer;
        EditorUtility.SetDirty(cameraData);
        EditorUtility.SetDirty(volume);
        EditorUtility.SetDirty(color);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(volume.gameObject.scene);
        EditorSceneManager.SaveScene(volume.gameObject.scene);
        return "Brighter block shading and atlas saturation; global saturation +28, contrast +7, exposure +0.05. Original profile preserved.";
    }
}
