using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ResultUIPolish
{
    [MenuItem("Tools/Block Building/Center Result Controls")]
    public static string Apply()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Stop Play Mode first.");
        var ui = Object.FindFirstObjectByType<BuildLevelMenu>(FindObjectsInactive.Include);
        var layout = ui.result.GetComponent<CompletionPanelLayout>();
        layout.Layout();
        var audio = Object.FindFirstObjectByType<StockGameAudio>();
        int before = audio.buttons.Length;
        audio.buttons = audio.buttons.Where(b => b != null && b.name != "Jump").ToArray();
        foreach (var r in new[]
        {
            layout.box,
            layout.preview,
            layout.text,
            layout.continueButton,
            layout.menuButton
        }

        )
            EditorUtility.SetDirty(r);
        EditorUtility.SetDirty(layout);
        EditorUtility.SetDirty(audio);
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        EditorSceneManager.SaveScene(ui.gameObject.scene);
        return "Result controls centered in the right column. Jump removed from click audio: "
            + (before - audio.buttons.Length)
            + ". Scene saved.";
    }
}
