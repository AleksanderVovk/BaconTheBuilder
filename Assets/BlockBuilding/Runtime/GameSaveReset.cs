using UnityEngine;
using UnityEngine.SceneManagement;
using YG;

public class GameSaveReset : MonoBehaviour
{
    [Header("Button"), Tooltip("Small instant reset button in the level menu.")]
    public UnityEngine.UI.Button button;

    private const string PendingKey = "BlockBuilding.ResetPending.v1";
    public static bool IsResetting { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        IsResetting = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void WatchPending()
    {
        YG2.onGetSDKData -= ApplyPending;
        YG2.onGetSDKData += ApplyPending;
        ApplyPending();
    }

    private static void ApplyPending()
    {
        if (PlayerPrefs.GetInt(PendingKey, 0) != 1 || !YG2.isSDKEnabled)
            return;
        ReplacePlatformSave();
        PlayerPrefs.DeleteKey(PendingKey);
        PlayerPrefs.Save();
    }

    private static void ReplacePlatformSave()
    {
        int revision = YG2.saves != null ? YG2.saves.idSave : 0;
        // Do not broadcast onGetSDKData: existing live systems still hold the old run.
        YG2.saves = new SavesYG
        {
            idSave = revision
        };
        if (YG2.isSDKEnabled)
            YG2.SaveProgress();
    }

    private void OnEnable()
    {
        if (button != null)
            button.onClick.AddListener(DeleteAllSaves);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(DeleteAllSaves);
    }

    public void DeleteAllSaves()
    {
        if (IsResetting)
            return;
        IsResetting = true;
        if (button != null)
            button.interactable = false;
        // Prevent old scene shutdown callbacks from putting progress back into storage.
        PlayerPrefs.DeleteAll();
        BuildProgressStore.ClearMemory();
        VictoryEconomy.ClearMemory();
        ReplacePlatformSave();
        if (!YG2.isSDKEnabled)
            PlayerPrefs.SetInt(PendingKey, 1);
        PlayerPrefs.Save();
        SceneManager.sceneLoaded += ResetFinished;
        SceneManager.LoadScene(gameObject.scene.name);
    }

    private static void ResetFinished(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= ResetFinished;
        IsResetting = false;
    }
}
