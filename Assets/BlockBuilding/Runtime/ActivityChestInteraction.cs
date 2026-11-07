using System.Globalization;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(310)]
public class ActivityChestInteraction : MonoBehaviour
{
    [Header("Chest"), Tooltip("Reward source.")]
    public ActivityChestReward reward;

    [Header("View"), Tooltip("Existing gameplay camera.")]
    public Camera view;

    [Header("Button"), Tooltip("Same visual style as the trainer interaction.")]
    public UnityEngine.UI.Button button;

    [Header("Caption"), Tooltip("Interaction caption.")]
    public TMP_Text caption;

    [Header("World label"), Tooltip("Progress and currently available reward.")]
    public TMP_Text worldLabel;

    [Header("Toast"), Tooltip("Reward notification overlay.")]
    public CanvasGroup toast;

    [Header("Toast text"), Tooltip("Strength gain text.")]
    public TMP_Text toastText;

    [Header("Audio"), Tooltip("Existing stock audio setup.")]
    public StockGameAudio audioFeedback;

    [Header("Progress bar"), Tooltip("Read-only green world-space progress bar.")]
    public UnityEngine.UI.Slider progressBar;

    [Header("Progress count"), Tooltip("White centered block counter.")]
    public TMP_Text progressCount;

    private float toastAt = -100;
    private bool subscribed;
    private void Start()
    {
        button.onClick.AddListener(Interact);
        reward.Changed += Refresh;
        reward.Claimed += ShowReward;
        subscribed = true;
        toast.blocksRaycasts = false;
        toast.interactable = false;
        toast.alpha = 0;
        Refresh();
    }

    private static string Number(System.Numerics.BigInteger n)
    {
        return StrengthNumber.Format(n);
    }

    private void Refresh()
    {
        bool ready = reward.Pending > 0;
        worldLabel.text = "ПОДАРОК";
        int count = ready ? reward.blocksPerReward : reward.Progress;
        if (progressBar != null)
            progressBar.SetValueWithoutNotify((float)count / Mathf.Max(1, reward.blocksPerReward));
        if (progressCount != null)
            progressCount.text = count + " / " + reward.blocksPerReward;
        caption.text = ready
            ? "Открыть: +" + Number(reward.RewardAmount) + " силы"
            : "Построй ещё " + (reward.blocksPerReward - reward.Progress) + " блоков";
    }

    private void Interact()
    {
        if (!reward.TryClaim() && audioFeedback != null)
            audioFeedback.Error();
    }

    private void ShowReward(System.Numerics.BigInteger amount)
    {
        toastText.text = "Вы получили +" + Number(amount) + " силы!";
        toastAt = Time.unscaledTime;
        toast.gameObject.SetActive(true);
        toast.alpha = 1;
        if (audioFeedback != null && audioFeedback.clips.Length > 5 && audioFeedback.clips[5] != null)
            audioFeedback.effects.PlayOneShot(audioFeedback.clips[5], .55f);
    }

    private void LateUpdate()
    {
        bool play = !reward.session.MenuOpen
            && !reward.session.SettingsOpen
            && !reward.session.AdminOverlayOpen
            && (!reward.session.Completed
                || reward.session.IsExploringCompleted);
        bool show = play && !reward.session.training.IsTraining && reward.IsInRange;
        if (button.gameObject.activeSelf != show)
            button.gameObject.SetActive(show);
        button.interactable = show && reward.Pending > 0 && !reward.Opening && reward.RewardAmount > 0;
        if (show)
        {
            var r = (RectTransform)button.transform;
            var parent = (RectTransform)r.parent;
            r.sizeDelta = new Vector2(Mathf.Min(340, parent.rect.width * .55f), 76);
            Vector3 screen = view.WorldToScreenPoint(reward.transform.position + Vector3.up * 2.1f);
            Vector2 local;
            if (screen.z <= 0)
                button.gameObject.SetActive(false);
            else
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out local);
                float half = r.rect.width * .5f + 24;
                local.x = Mathf.Clamp(local.x, parent.rect.xMin + half, parent.rect.xMax - half);
                local.y = Mathf.Clamp(local.y, parent.rect.yMin + 300, parent.rect.yMax - 200);
                r.anchoredPosition = local;
            }
        }

        float elapsed = Time.unscaledTime - toastAt;
        toast.alpha = play ? Mathf.Clamp01((3.5f - elapsed) / .7f) : 0;
        var rect = (RectTransform)toast.transform;
        rect.sizeDelta = new Vector2(Mathf.Min(650, ((RectTransform)rect.parent).rect.width * .85f), 100);
        rect.anchoredPosition = new Vector2(0, Mathf.Min(elapsed, 3.5f) * 14);
    }

    private void OnDisable()
    {
        if (button != null)
            button.gameObject.SetActive(false);
        if (toast != null)
            toast.alpha = 0;
    }

    private void OnDestroy()
    {
        if (!subscribed)
            return;
        if (button != null)
            button.onClick.RemoveListener(Interact);
        reward.Changed -= Refresh;
        reward.Claimed -= ShowReward;
    }
}
