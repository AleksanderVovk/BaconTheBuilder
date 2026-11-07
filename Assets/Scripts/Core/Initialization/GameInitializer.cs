using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GameInitializer : MonoBehaviour
{
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private string nextSceneName = "MainScene";

    private AsyncOperation asyncLoad;

    private void Start()
    {
        Debug.Log($"[GameInitializer] Start loading scene: {nextSceneName}");
        StartCoroutine(LoadNextScene());
    }

    private IEnumerator LoadNextScene()
    {
        _ = Environment.IsMobile;
        Debug.Log("[GameInitializer] Environment initialized");

        loadingSlider.value = 0f;

        Debug.Log($"[GameInitializer] Starting async load: {nextSceneName}");
        asyncLoad = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Single);
        asyncLoad.allowSceneActivation = false;

        // Ждём пока загрузится до 0.9
        while (asyncLoad.progress < 0.9f)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress);
            loadingSlider.value = progress;
            Debug.Log($"[GameInitializer] Loading progress: {progress:F2}");

            yield return null;
        }

        Debug.Log("[GameInitializer] Scene loaded to 90%, activating...");
        loadingSlider.value = 1f;

        // Разрешаем активацию
        asyncLoad.allowSceneActivation = true;
        Debug.Log("[GameInitializer] allowSceneActivation = true");

        // Ждём пока asyncLoad.isDone станет true
        while (!asyncLoad.isDone)
        {
            Debug.Log("[GameInitializer] Waiting for activation...");
            yield return null;
        }

        Debug.Log("[GameInitializer] Scene activated successfully!");
    }
}