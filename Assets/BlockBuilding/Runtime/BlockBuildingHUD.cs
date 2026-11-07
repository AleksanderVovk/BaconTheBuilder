using TMPro;
using UnityEngine;

public class BlockBuildingHUD : MonoBehaviour
{
    [Header("Strength"), Tooltip("Progression source.")]
    public StrengthUpgradeSystem strength;

    [Header("Inventory"), Tooltip("Carried cube source.")]
    public PlayerBlockCarrier carrier;

    [Header("Training"), Tooltip("Bench interaction source.")]
    public StrengthTrainingZone training;

    [Header("Building"), Tooltip("Active building source.")]
    public BlockLevelRuntime level;

    [Header("Pickup"), Tooltip("Supply interaction source.")]
    public BlockPickupZone pickup;

    [Header("Text"), Tooltip("Strength, level and block counter.")]
    public TMP_Text statsText;

    [Header("Hint"), Tooltip("Interaction/status hint text.")]
    public TMP_Text hintText;

    [Header("Progress bar"), Tooltip("Strength progress slider.")]
    public UnityEngine.UI.Slider progress;

    [Header("Pickup button"), Tooltip("Mobile pickup button; callback wired once at enable.")]
    public UnityEngine.UI.Button pickupButton;

    [Header("Train button"), Tooltip("Mobile training toggle.")]
    public UnityEngine.UI.Button trainButton;

    [Header("Level feedback"), Tooltip("Optional UI flash. Does not block touch input.")]
    public UnityEngine.UI.Image levelFlash;

    private float nextHint, flash;
    private void OnEnable()
    {
        if (strength != null)
        {
            strength.Changed += Refresh;
            strength.LeveledUp += LevelUp;
        }

        if (carrier != null)
            carrier.Changed += Refresh;
        if (training != null)
            training.Changed += Refresh;
        if (level != null)
            level.Changed += Refresh;
        if (pickupButton != null)
            pickupButton.onClick.AddListener(Pickup);
        if (trainButton != null)
            trainButton.onClick.AddListener(Train);
        Refresh();
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        if (strength != null)
        {
            strength.Changed -= Refresh;
            strength.LeveledUp -= LevelUp;
        }

        if (carrier != null)
            carrier.Changed -= Refresh;
        if (training != null)
            training.Changed -= Refresh;
        if (level != null)
            level.Changed -= Refresh;
        if (pickupButton != null)
            pickupButton.onClick.RemoveListener(Pickup);
        if (trainButton != null)
            trainButton.onClick.RemoveListener(Train);
    }

    private void Pickup()
    {
        if (pickup != null)
            pickup.PickupFromUI();
    }

    private void Train()
    {
        if (training != null)
        {
            var hub = training.GetComponent<GymEquipmentHub>();
            if (hub != null)
                hub.Interact();
            else
                training.ToggleTraining();
        }
    }

    private void LevelUp()
    {
        flash = 1;
    }

    private void Refresh()
    {
        if (strength == null || carrier == null)
            return;
        if (statsText != null)
            statsText.text = "СИЛА "
                + StrengthNumber.Format(strength.Strength)
                + "   /   УР. " + StrengthNumber.Format(strength.Level) + (strength.NextThreshold < 0
                ? " МАКС."
                : "");
        if (progress != null)
            progress.value = strength.Progress;
        nextHint = 0;
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextHint)
        {
            nextHint = Time.unscaledTime + 0.15f;
            bool canPickup = pickup != null
                && pickup.IsInRange
                && !carrier.IsTraining
                && carrier.Count < carrier.Capacity
                && carrier.HasSupplyForNextSlot;
            bool canTrain = training != null && (training.IsTraining || training.IsInRange && carrier.Count == 0);
            if (pickupButton != null)
            {
                pickupButton.gameObject.SetActive(pickup == null || !pickup.automaticPickup);
                pickupButton.interactable = canPickup;
            }

            if (trainButton != null)
                trainButton.interactable = canTrain;
            if (hintText != null)
            {
                string message = training != null
                    && training.IsTraining
                    ? "Жим лёжа... F / Жим - закончить"
                    : canPickup
                    ? (pickup.automaticPickup
                    ? "Кубики автоматически заполняют руки"
                    : "E / Взять - подобрать кубик")
                    : canTrain
                    ? "F / Жим - начать тренировку"
                    : level != null
                    && level.CurrentSite == null
                    ? "Все постройки завершены!"
                    : carrier.Count > 0
                    ? "Следуйте линии: блоки ставятся при приближении"
                    : "Подойдите к куче: кубики подберутся автоматически";
                if (hintText.text != message)
                    hintText.text = message;
            }
        }

        if (levelFlash != null && flash > 0)
        {
            flash = Mathf.MoveTowards(flash, 0, Time.unscaledDeltaTime * 2);
            levelFlash.color = new Color(0.25f, 1, 0.5f, flash * 0.35f);
        }
    }
}
