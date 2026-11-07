using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StrengthRewardPopupSetup
{
    [MenuItem("Tools/Block Building/Install Strength Reward Popups")]
    public static void Install()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play mode first.");
        var hub = UnityEngine.Object.FindFirstObjectByType<GymEquipmentHub>();
        var hud = hub.session.gameplayHUD.transform;
        var existing = hud.Find("StrengthRewardPopups");
        var root = existing != null ? (RectTransform)existing : NewRect("StrengthRewardPopups", hud);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        root.SetAsLastSibling();
        var group = root.GetComponent<CanvasGroup>();
        if (group == null)
            group = root.gameObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        var effect = root.GetComponent<StrengthRewardPopups>();
        if (effect == null)
            effect = root.gameObject.AddComponent<StrengthRewardPopups>();
        effect.training = hub.training;
        effect.area = root;
        effect.canvas = hud.GetComponentInParent<Canvas>();
        effect.items = new StrengthRewardPopups.Item[16];
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Graphics/Sprites/strong.png");
        if (sprite == null)
            throw new InvalidOperationException("strong sprite not found");
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/BlockBuilding/UI/BlockUIFont.asset");
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/BlockBuilding/UI/BlockUIText.mat");
        for (int i = 0; i < effect.items.Length; i++)
        {
            var rect = NewRect("Reward" + i, root);
            rect.sizeDelta = new Vector2(320, 86);
            var cg = rect.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = rect.gameObject.AddComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = false;
            var number = NewRect("Amount", rect);
            number.pivot = new Vector2(0, .5f);
            number.sizeDelta = new Vector2(180, 76);
            var text = number.GetComponent<TextMeshProUGUI>();
            if (text == null)
                text = number.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSharedMaterial = material;
            text.fontSize = 54;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            text.text = "+1";
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = true;
            text.fontSizeMin = 28;
            text.fontSizeMax = 54;
            text.color = Color.white;
            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(
                new Color(.9f, 1, .6f),
                new Color(.9f, 1, .6f),
                new Color(.25f, .9f, .15f),
                new Color(.25f, .9f, .15f)
            );
            var icon = NewRect("Strong", rect);
            icon.sizeDelta = new Vector2(58, 58);
            var image = icon.GetComponent<UnityEngine.UI.Image>();
            if (image == null)
                image = icon.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            effect.items[i] = new StrengthRewardPopups.Item
            {
                root = rect,
                group = cg,
                text = text,
                icon = icon
            };
            rect.gameObject.SetActive(false);
        }

        EditorUtility.SetDirty(effect);
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        EditorSceneManager.SaveScene(root.gameObject.scene);
        AssetDatabase.SaveAssets();
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var found = parent.Find(name);
        if (found != null)
            return (RectTransform)found;
        var r = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f);
        return r;
    }
}
