using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MobileBuildSetup
{
    [MenuItem("Tools/Block Building/Configure Mobile Controls")]
    public static string Install()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play mode first.");
        var session = UnityEngine.Object.FindFirstObjectByType<BuildLevelSession>();
        var canvas = session.gameplayHUD.GetComponentInParent<Canvas>();
        var mobile = canvas.transform.Find("Controllers/Mobile");
        var inputs = session.runtime.carrier.player.GetComponent<InputsManager>();
        inputs.forceTouchControls = true;
        var hud = session.gameplayHUD.GetComponent<BlockBuildingHUD>();
        var component = session.runtime.GetComponent<MobileBuildInteraction>();
        if (component == null)
            component = session.runtime.gameObject.AddComponent<MobileBuildInteraction>();
        component.session = session;
        component.training = session.training;
        component.mobileControls = mobile.gameObject;
        component.interaction = hud.trainButton;
        component.caption = hud.trainButton.GetComponentInChildren<TMP_Text>(true);
        component.viewCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        hud.trainButton.transform.SetParent(session.gameplayHUD.transform, false);
        var prompt = (RectTransform)hud.trainButton.transform;
        prompt.anchorMin = prompt.anchorMax = prompt.pivot = new Vector2(0.5f, 0.5f);
        prompt.sizeDelta = new Vector2(530, 110);
        prompt.anchoredPosition = Vector2.zero;
        component.caption.text = "Нажмите, чтобы взаимодействовать";
        component.caption.fontSizeMax = 25;
        var oldIcon = prompt.Find("PixelIcon");
        if (oldIcon != null)
            oldIcon.gameObject.SetActive(false);
        component.caption.rectTransform.offsetMin = new Vector2(16, 8);
        component.caption.rectTransform.offsetMax = new Vector2(-16, -8);
        var zones = mobile.Find("Zones");
        zones.GetComponent<UnityEngine.UI.GridLayoutGroup>().enabled = false;
        foreach (Transform child in zones)
        {
            var r = (RectTransform)child;
            bool left = child.name == "JoysticZone";
            r.anchorMin = new Vector2(left ? 0 : 0.5f, 0);
            r.anchorMax = new Vector2(left ? 0.5f : 1, 1);
            r.offsetMin = r.offsetMax = Vector2.zero;
            if (left)
                r.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        }

        var joystick = (RectTransform)mobile.Find("Joystick");
        Place(joystick, new Vector2(0, 0), new Vector2(180, 230), new Vector2(220, 220));
        joystick.pivot = new Vector2(0.5f, 0.5f);
        var thumb = (RectTransform)joystick.Find("Thumb");
        thumb.anchorMin = thumb.anchorMax = thumb.pivot = new Vector2(0.5f, 0.5f);
        thumb.anchoredPosition = Vector2.zero;
        thumb.sizeDelta = new Vector2(110, 110);
        var serializedJoystick = new SerializedObject(joystick.GetComponent<SimpleInputNamespace.Joystick>());
        serializedJoystick.FindProperty("isDynamicJoystick").boolValue = false;
        serializedJoystick.ApplyModifiedPropertiesWithoutUndo();
        var jump = (RectTransform)mobile.Find("Jump");
        Place(jump, new Vector2(1, 0), new Vector2(-48, 90), new Vector2(160, 160));
        var oldJump = jump.Find("Image");
        if (oldJump != null)
            oldJump.gameObject.SetActive(false);
        var safety = session.runtime.GetComponent<BuildPlayerSafety>();
        Place((RectTransform)safety.returnButton.transform, new Vector2(1, 0), new Vector2(-36, 270), new Vector2(310, 88));
        foreach (var text in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            text.text = Regex.Replace(text.text, @"\s*\[(?:F8|Esc|T|F|E|Space)\]", "");
            EditorUtility.SetDirty(text);
        }

        var admin = canvas.GetComponentInChildren<BuildAdminMenu>(true);
        admin.buttons[1].GetComponentInChildren<TMP_Text>(true).text = "ПРОДОЛЖИТЬ";
        mobile.gameObject.SetActive(false);
        foreach (var c in canvas.GetComponentsInChildren<Component>(true))
            EditorUtility.SetDirty(c);
        EditorUtility.SetDirty(inputs);
        EditorUtility.SetDirty(component);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(session.gameObject.scene);
        EditorSceneManager.SaveScene(session.gameObject.scene);
        return "Existing joystick/jump enabled for touch; contextual bench interaction installed.";
    }

    private static void Place(RectTransform r, Vector2 anchor, Vector2 position, Vector2 size)
    {
        r.anchorMin = r.anchorMax = r.pivot = anchor;
        r.anchoredPosition = position;
        r.sizeDelta = size;
    }
}
