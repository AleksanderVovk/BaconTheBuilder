using System.Linq;
using TMPro;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class GameSaveResetSetup
{
    public static string Apply()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Stop Play Mode first.");
        var menu = Object.FindFirstObjectByType<BuildLevelMenu>(FindObjectsInactive.Include);
        if (menu.menu.transform.Find("DeleteSaves") != null)
            return "Reset button already installed";
        var button = Object.Instantiate(menu.resume, menu.menu.transform);
        button.name = "DeleteSaves";
        button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        button.gameObject.SetActive(true);
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-32, 24);
        rect.sizeDelta = new Vector2(270, 44);
        var image = button.GetComponent<UnityEngine.UI.Image>();
        image.color = new Color(.57f, .22f, .25f, 1);
        var text = button.GetComponentInChildren<TMP_Text>(true);
        text.text = "Удалить сохранения";
        text.fontSize = 22;
        text.enableAutoSizing = true;
        text.fontSizeMin = 16;
        text.fontSizeMax = 22;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        var reset = button.gameObject.AddComponent<GameSaveReset>();
        reset.button = button;
        var audio = Object.FindFirstObjectByType<StockGameAudio>(FindObjectsInactive.Include);
        if (audio != null)
        {
            audio.buttons = audio.buttons.Concat(new[] { button }).ToArray();
            EditorUtility.SetDirty(audio);
        }

        var footer = menu.menu.transform.Find("Footer");
        if (footer != null)
        {
            var r = (RectTransform)footer;
            r.anchorMin = new Vector2(0, 0);
            r.anchorMax = new Vector2(1, 0);
            r.pivot = new Vector2(.5f, 0);
            r.offsetMin = new Vector2(48, 24);
            r.offsetMax = new Vector2(-330, 68);
        }

        EditorUtility.SetDirty(reset);
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
        EditorSceneManager.SaveScene(menu.gameObject.scene);
        return "Small instant reset button saved in Lobby; no confirmation; saves untouched during setup.";
    }
}
