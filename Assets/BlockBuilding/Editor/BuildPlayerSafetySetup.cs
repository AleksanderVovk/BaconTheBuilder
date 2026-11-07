using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildPlayerSafetySetup
{
    [MenuItem("Tools/Block Building/Install Player Safety")]
    public static void Install()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play mode first.");
        var session = UnityEngine.Object.FindFirstObjectByType<BuildLevelSession>();
        if (session == null || session.gameplayHUD == null)
            throw new InvalidOperationException("Existing session/HUD is required.");
        var safety = session.runtime.GetComponent<BuildPlayerSafety>();
        if (safety == null)
            safety = Undo.AddComponent<BuildPlayerSafety>(session.runtime.gameObject);
        safety.runtime = session.runtime;
        safety.session = session;
        safety.player = session.runtime.carrier.player;
        safety.character = safety.player.GetComponent<CharacterController>();
        var parent = session.gameplayHUD.transform;
        var child = parent.Find("ReturnToLastBlock");
        if (child == null)
        {
            var go = new GameObject(
                "ReturnToLastBlock",
                typeof(RectTransform),
                typeof(UnityEngine.UI.Image),
                typeof(UnityEngine.UI.Button)
            );
            Undo.RegisterCreatedObjectUndo(go, "Add construction return button");
            go.transform.SetParent(parent, false);
            child = go.transform;
        }

        var rect = (RectTransform)child;
        rect.anchorMin = rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-28, 272);
        rect.sizeDelta = new Vector2(450, 72);
        var image = child.GetComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.10f, 0.30f, 0.42f, 0.96f);
        var button = child.GetComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
        button.colors = colors;
        var labelTransform = child.Find("Label");
        if (labelTransform == null)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(child, false);
            labelTransform = go.transform;
        }

        var label = labelTransform.GetComponent<TextMeshProUGUI>();
        var hud = session.gameplayHUD.GetComponent<BlockBuildingHUD>();
        label.font = hud != null && hud.statsText != null ? hud.statsText.font : TMP_Settings.defaultFontAsset;
        label.text = "НА ПОСЛЕДНИЙ БЛОК [T]";
        label.fontSize = 26;
        label.fontSizeMin = 18;
        label.fontSizeMax = 26;
        label.enableAutoSizing = true;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        var lr = label.rectTransform;
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = new Vector2(12, 6);
        lr.offsetMax = new Vector2(-12, -6);
        safety.returnButton = button;
        button.interactable = false;
        EditorUtility.SetDirty(safety);
        EditorUtility.SetDirty(button);
        EditorUtility.SetDirty(label);
        EditorSceneManager.MarkSceneDirty(session.gameObject.scene);
        EditorSceneManager.SaveScene(session.gameObject.scene);
    }
}
