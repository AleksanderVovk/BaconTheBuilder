using TMPro;
using UnityEngine;

[DefaultExecutionOrder(300)]
public class MobileBuildInteraction : MonoBehaviour
{
    [Header("Session"), Tooltip("Existing gameplay session.")]
    public BuildLevelSession session;

    [Header("Training"), Tooltip("Existing bench interaction, including empty-hand rule.")]
    public StrengthTrainingZone training;

    [Header("Controls"), Tooltip("Existing Canvas/Controllers/Mobile root.")]
    public GameObject mobileControls;

    [Header("Interaction"), Tooltip("Existing training button, reused without changing its listener.")]
    public UnityEngine.UI.Button interaction;

    [Header("Caption"), Tooltip("Training button caption.")]
    public TMP_Text caption;

    [Header("View"), Tooltip("Cached gameplay camera; no Camera.main lookup in Update.")]
    public Camera viewCamera;

    [Header("Anchor"), Tooltip("Height above the bench in world units."), Min(0)]
    public float promptHeight = 1.8f;

    private GymEquipmentHub hub;
    private void Start()
    {
        hub = training.GetComponent<GymEquipmentHub>();
    }

    private bool Playable
    {
        get
        {
            return !session.MenuOpen
                && !session.SettingsOpen
                && !session.AdminOverlayOpen
                && (!session.Completed
                    || session.IsExploringCompleted);
        }
    }

    private void LateUpdate()
    {
        bool play = Playable;
        bool moving = play && !training.IsTraining;
        if (mobileControls.activeSelf != moving)
            mobileControls.SetActive(moving);
        bool show = play && training.enabled && (training.IsTraining || training.IsInRange);
        if (interaction.gameObject.activeSelf != show)
            interaction.gameObject.SetActive(show);
        if (!show)
            return;
        bool occupied = training.carrier.Count > 0;
        string text = training.IsTraining
            ? "Закончить тренировку"
            : occupied
            ? "Сначала освободите руки"
            : "Нажмите, чтобы взаимодействовать";
        bool locked = hub != null && hub.Selected != null && !hub.Owns(hub.Selected);
        if (!training.IsTraining && hub != null && hub.Selected != null)
            text = occupied
                ? "Сначала освободите руки"
                : locked
                ? (hub.CanBuy(hub.Selected)
                ? "Разблокировать: " + hub.Price(hub.Selected) + " побед"
                : "Нужно " + hub.Price(hub.Selected) + " побед")
                : "Тренироваться x" + hub.Selected.multiplier;
        if (caption.text != text)
            caption.text = text;
        interaction.interactable = training.IsTraining || !occupied && (hub == null || !hub.PurchasePending);
        var rect = (RectTransform)interaction.transform;
        var parent = (RectTransform)rect.parent;
        Vector2 local = Vector2.zero;
        float width = Mathf.Min(340, parent.rect.width * 0.55f);
        rect.sizeDelta = new Vector2(width, training.IsTraining ? 66 : 76);
        caption.fontSizeMax = 22;
        caption.fontSizeMin = 14;
        caption.enableAutoSizing = true;
        if (training.IsTraining)
        {
            rect.anchoredPosition = new Vector2(0, parent.rect.yMin + 130);
            return;
        }

        if (viewCamera != null && !training.IsTraining)
        {
            Vector3 screen = viewCamera.WorldToScreenPoint(training.InteractionPosition + Vector3.up * promptHeight);
            if (screen.z > 0)
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out local);
        }

        float half = rect.rect.width * 0.5f + 24;
        local.x = Mathf.Clamp(local.x, parent.rect.xMin + half, parent.rect.xMax - half);
        local.y = Mathf.Clamp(local.y, parent.rect.yMin + 340, parent.rect.yMax - 200);
        rect.anchoredPosition = local;
    }

    private void OnDisable()
    {
        if (interaction != null)
            interaction.gameObject.SetActive(false);
    }
}
