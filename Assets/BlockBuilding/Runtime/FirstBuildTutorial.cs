using TMPro;
using UnityEngine;
using YG;

[DefaultExecutionOrder(600)]
public class FirstBuildTutorial : MonoBehaviour
{
    [Header("Session"), Tooltip("Existing level/menu system.")]
    public BuildLevelSession session;

    [Header("Panel"), Tooltip("Tutorial panel outside the gameplay HUD.")]
    public CanvasGroup panel;

    [Header("Heading"), Tooltip("Current tutorial step.")]
    public TMP_Text heading;

    [Header("Instruction"), Tooltip("Short instruction or congratulations.")]
    public TMP_Text instruction;

    [Header("Pickup label"), Tooltip("World-space pulsing label above the pile.")]
    public TMP_Text pickupLabel;

    [Header("Camera"), Tooltip("Cached gameplay camera.")]
    public Camera view;

    [Header("Pulse"), Tooltip("Maximum text scale; minimum is 1."), Range(1, 1.4f)]
    public float pulseScale = 1.15f;

    [Header("Pulse duration"), Tooltip("Seconds per full pulse."), Min(.2f)]
    public float pulseSeconds = 1.5f;

    [Header("Congratulations"), Tooltip("Seconds before the completed panel fades out."), Min(1)]
    public float congratulationsSeconds = 4;

    private const string Key = "BlockBuilding.FirstBuildTutorial.Done.v1";
    private bool active, picked, finished;
    private float finishedAt;
    public bool InProgress
    {
        get
        {
            return active;
        }
    }

    public bool IsFinished
    {
        get
        {
            return finished;
        }
    }

    private void Start()
    {
        session.runtime.carrier.Changed += Inventory;
        session.runtime.BlockPlaced += Placed;
        YG2.onGetSDKData += CloudLoaded;
        panel.blocksRaycasts = false;
        panel.interactable = false;
        panel.gameObject.SetActive(false);
        if (PlayerPrefs.GetInt(Key, 0) == 1 || YG2.isSDKEnabled && YG2.saves.firstBuildTutorialDone)
        {
            CloudLoaded();
            return;
        }

        active = true;
        session.Select(0);
        Inventory();
    }

    private void CloudLoaded()
    {
        if (YG2.saves == null)
            return;
        if (YG2.saves.firstBuildTutorialDone)
        {
            PlayerPrefs.SetInt(Key, 1);
            PlayerPrefs.Save();
            active = false;
            panel.gameObject.SetActive(false);
            session.runtime.path.Guide(false, Vector3.zero);
        }
        else if (PlayerPrefs.GetInt(Key, 0) == 1 && YG2.isSDKEnabled)
        {
            YG2.saves.firstBuildTutorialDone = true;
            YG2.SaveProgress();
        }
    }

    private void Inventory()
    {
        if (!active)
            return;
        if (session.runtime.carrier.Count > 0)
            picked = true;
        bool carrying = session.runtime.carrier.Count > 0;
        heading.text = carrying ? "ОБУЧЕНИЕ 2/2" : "ОБУЧЕНИЕ 1/2";
        instruction.text = carrying
            ? "Иди поставь блоки\nСледуй за стрелками к месту стройки"
            : "Подбери блоки\nПодойди к кучке: блоки возьмутся автоматически";
    }

    private void Placed(BlockType type, Vector3 point)
    {
        if (!active || !picked)
            return;
        active = false;
        finished = true;
        finishedAt = Time.unscaledTime;
        PlayerPrefs.SetInt(Key, 1);
        PlayerPrefs.Save();
        if (YG2.saves != null)
        {
            YG2.saves.firstBuildTutorialDone = true;
            if (YG2.isSDKEnabled)
                YG2.SaveProgress();
        }

        heading.text = "ОБУЧЕНИЕ ЗАВЕРШЕНО";
        instruction.text = "Поздравляю!\nТы прошёл обучение!";
        session.runtime.path.Guide(false, Vector3.zero);
    }

    private void LateUpdate()
    {
        var rect = (RectTransform)panel.transform;
        float width = Mathf.Min(720, ((RectTransform)rect.parent).rect.width - 32);
        rect.sizeDelta = new Vector2(width, 150);
        heading.rectTransform.sizeDelta = new Vector2(width - 32, 40);
        instruction.rectTransform.sizeDelta = new Vector2(width - 32, 85);
        bool visible = !session.MenuOpen && (active || finished && Time.unscaledTime - finishedAt < congratulationsSeconds);
        if (panel.gameObject.activeSelf != visible)
            panel.gameObject.SetActive(visible);
        if (visible)
            panel.alpha = finished ? Mathf.Clamp01((congratulationsSeconds - (Time.unscaledTime - finishedAt)) / .5f) : 1;
        bool collecting = active && !session.MenuOpen && session.runtime.carrier.Count == 0;
        if (active && !session.MenuOpen)
        {
            Vector3 target = collecting
                ? session.runtime.pickupTransform.position + Vector3.up * .35f
                : session.runtime.CurrentSite != null
                ? session.runtime.CurrentSite.NextWorldPosition
                : session.runtime.pickupTransform.position;
            target.y = Mathf.Max(.3f, target.y);
            session.runtime.path.Guide(true, target);
        }
        else
            session.runtime.path.Guide(false, Vector3.zero);
    }

    private void OnDestroy()
    {
        YG2.onGetSDKData -= CloudLoaded;
        if (session != null)
        {
            session.runtime.carrier.Changed -= Inventory;
            session.runtime.BlockPlaced -= Placed;
            if (session.runtime.path != null)
                session.runtime.path.Guide(false, Vector3.zero);
        }
    }
}
