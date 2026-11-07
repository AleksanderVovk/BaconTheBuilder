using TMPro;
using UnityEngine;

public class BuildLevelMenu : MonoBehaviour
{
    [Header("Session"), Tooltip("Runtime level state.")]
    public BuildLevelSession session;

    [Header("Menu"), Tooltip("Full-screen level selection panel.")]
    public GameObject menu;

    [Header("Cards"), Tooltip("One card per catalog entry.")]
    public BuildLevelCard[] cards = new BuildLevelCard[0];

    [Header("Resume"), Tooltip("Continue the current unfinished attempt.")]
    public UnityEngine.UI.Button resume;

    [Header("HUD"), Tooltip("Timer and menu button.")]
    public GameObject runHUD;

    [Header("Timer"), Tooltip("Current attempt elapsed time.")]
    public TMP_Text timer;

    [Header("Progress"), Tooltip("Total level block count, including all trees.")]
    public TMP_Text progress;

    [Header("Back"), Tooltip("Open the selection menu and pause the timer.")]
    public UnityEngine.UI.Button back;

    [Header("Result"), Tooltip("Completion overlay.")]
    public GameObject result;

    [Header("Result text"), Tooltip("Elapsed time and record notification.")]
    public TMP_Text resultText;

    [Header("Again"), Tooltip("Return control to explore the completed level.")]
    public UnityEngine.UI.Button again;

    [Header("Levels"), Tooltip("Open menu from the result screen.")]
    public UnityEngine.UI.Button levels;

    [Header("Build bar"), Tooltip("Read-only total construction progress, with the count centered over it.")]
    public UnityEngine.UI.Slider constructionProgress;

    private float nextTimerUpdate;
    private void Awake()
    {
        session.Changed += Refresh;
        for (int i = 0; i < cards.Length; i++)
        {
            int index = i;
            cards[i].button.onClick.AddListener(() => session.Select(index));
        }

        resume.onClick.AddListener(session.Resume);
        back.onClick.AddListener(session.OpenMenu);
        again.onClick.AddListener(session.ContinueExploring);
        levels.onClick.AddListener(session.OpenMenu);
    }

    private void Start()
    {
        Refresh();
        AdaptHUD();
    }

    private void OnRectTransformDimensionsChange()
    {
        AdaptHUD();
    }

    private void AdaptHUD()
    {
        if (timer == null)
            return;
        var panel = (RectTransform)timer.transform.parent;
        float width = ((RectTransform)transform).rect.width;
        bool narrow = width < 1450;
        panel.anchoredPosition = new Vector2(0, narrow ? -260 : -24);
        panel.sizeDelta = new Vector2(narrow ? Mathf.Min(width - 64, 780) : 780, 182);
        if (menu != null)
        {
            var heading = menu.transform.Find("Heading") as RectTransform;
            if (heading != null)
                heading.sizeDelta = new Vector2(Mathf.Max(260, width - 480), 62);
        }
    }

    private void Update()
    {
        if (!session.MenuOpen && !session.Completed && Time.unscaledTime >= nextTimerUpdate)
        {
            nextTimerUpdate = Time.unscaledTime + 0.1f;
            timer.text = LevelRecordStore.Format(session.Elapsed);
        }
    }

    private void Refresh()
    {
        menu.SetActive(session.MenuOpen);
        runHUD.SetActive(!session.MenuOpen && (!session.Completed || session.IsExploringCompleted));
        result.SetActive(session.Completed && !session.MenuOpen && !session.IsExploringCompleted);
        resume.gameObject.SetActive(session.Selected != null && (!session.Completed || session.IsExploringCompleted));
        if (session.MenuOpen)
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].Refresh(session.catalog.levels[i], i + 1);
                if (cards[i].selectionFrame != null)
                    cards[i].selectionFrame.enabled = session.SelectedIndex == i;
            }

        if (session.Selected != null)
        {
            timer.text = LevelRecordStore.Format(session.Elapsed);
            progress.text = session.runtime.BuiltCount + " / " + session.runtime.TotalCount;
            if (constructionProgress != null)
                constructionProgress.SetValueWithoutNotify(session.runtime.TotalCount > 0
                    ? (float)session.runtime.BuiltCount / session.runtime.TotalCount
                    : 0);
            if (session.Completed)
                resultText.text = "ПОСТРОЕНО!\n"
                    + session.Selected.title
                    + "\n<color=#FFC65B>"
                    + LevelRecordStore.Format(session.Elapsed)
                    + "</color>\n" + (session.NewRecord
                    ? "НОВЫЙ РЕКОРД!"
                    : "Забег завершён");
        }
    }

    private void OnDestroy()
    {
        if (session != null)
            session.Changed -= Refresh;
    }
}
