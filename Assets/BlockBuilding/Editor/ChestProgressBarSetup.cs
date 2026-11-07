using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class ChestProgressBarSetup
{
    [MenuItem("Tools/Block Building/Style Chest Progress")]
    public static string Apply()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Stop Play Mode first.");
        var ui = Object.FindFirstObjectByType<ActivityChestInteraction>();
        var label = ui.worldLabel;
        label.text = "ПОДАРОК";
        label.fontStyle = FontStyles.Bold;
        label.fontSize = 7;
        label.enableAutoSizing = false;
        label.enableWordWrapping = false;
        label.color = Color.white;
        label.enableVertexGradient = true;
        label.colorGradient = new VertexGradient(
            new Color(1, .62f, .90f),
            new Color(1, .62f, .90f),
            new Color(.94f, .12f, .62f),
            new Color(.94f, .12f, .62f)
        );
        label.rectTransform.sizeDelta = new Vector2(10, 1.1f);
        label.transform.localPosition = new Vector3(0, 5.4f, 0);
        const string matPath = "Assets/BlockBuilding/Generated/ActivityChest/PinkHeading.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (material == null)
        {
            material = new Material(label.fontSharedMaterial);
            AssetDatabase.CreateAsset(material, matPath);
        }

        material.SetColor("_FaceColor", Color.white);
        material.SetColor("_OutlineColor", new Color(.08f, .015f, .07f));
        material.SetFloat("_OutlineWidth", .22f);
        label.fontSharedMaterial = material;
        var old = label.transform.Find("ChestProgressCanvas");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);
        var canvas = (RectTransform)new GameObject("ChestProgressCanvas", typeof(RectTransform), typeof(Canvas)).transform;
        canvas.SetParent(label.transform, false);
        canvas.localPosition = new Vector3(0, -.85f, 0);
        canvas.localRotation = Quaternion.identity;
        canvas.localScale = Vector3.one * .01f;
        canvas.sizeDelta = new Vector2(700, 95);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        var source = Object.FindFirstObjectByType<BuildLevelMenu>().constructionProgress;
        var slider = Object.Instantiate(source, canvas);
        slider.name = "ChestProgressBar";
        var rect = (RectTransform)slider.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        foreach (var text in slider.GetComponentsInChildren<TMP_Text>(true))
            Object.DestroyImmediate(text.gameObject);
        slider.onValueChanged = new UnityEngine.UI.Slider.SliderEvent();
        slider.interactable = false;
        slider.transition = UnityEngine.UI.Selectable.Transition.None;
        slider.navigation = new UnityEngine.UI.Navigation
        {
            mode = UnityEngine.UI.Navigation.Mode.None
        };
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.wholeNumbers = false;
        slider.value = 0;
        slider.handleRect = null;
        foreach (var image in slider.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            image.raycastTarget = false;
        var counter = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        counter.transform.SetParent(slider.transform, false);
        counter.rectTransform.anchorMin = Vector2.zero;
        counter.rectTransform.anchorMax = Vector2.one;
        counter.rectTransform.offsetMin = new Vector2(12, 3);
        counter.rectTransform.offsetMax = new Vector2(-12, -3);
        counter.font = label.font;
        counter.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/BlockBuilding/UI/BlockUIText.mat");
        counter.color = Color.white;
        counter.enableVertexGradient = false;
        counter.fontStyle = FontStyles.Bold;
        counter.alignment = TextAlignmentOptions.Center;
        counter.fontSize = 58;
        counter.enableWordWrapping = false;
        counter.enableAutoSizing = true;
        counter.fontSizeMin = 32;
        counter.fontSizeMax = 58;
        counter.raycastTarget = false;
        counter.text = ui.reward.Progress + " / " + ui.reward.blocksPerReward;
        ui.progressBar = slider;
        ui.progressCount = counter;
        label.GetComponent<WorldActivityLabel>().iconCanvas = canvas.GetComponent<Canvas>();
        EditorUtility.SetDirty(ui);
        EditorUtility.SetDirty(label);
        EditorUtility.SetDirty(material);
        EditorUtility.SetDirty(label.GetComponent<WorldActivityLabel>());
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        EditorSceneManager.SaveScene(ui.gameObject.scene);
        return "Pink bold ПОДАРОК; green Roblox progress bar with centered white counter; existing 100-block reward preserved.";
    }
}
