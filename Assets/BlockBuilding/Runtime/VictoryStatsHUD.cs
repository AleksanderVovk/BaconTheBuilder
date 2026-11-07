using TMPro;
using UnityEngine;

public class VictoryStatsHUD : MonoBehaviour
{
    [Header("Gameplay"), Tooltip("Existing HUD sources.")]
    public BlockBuildingHUD hud;

    [Header("Strength"), Tooltip("Strength number.")]
    public TMP_Text strengthText;

    [Header("Capacity"), Tooltip("Current carried blocks and capacity.")]
    public TMP_Text capacityText;

    [Header("Wins"), Tooltip("Spendable victory balance.")]
    public TMP_Text winsText;
    private void OnEnable()
    {
        if (hud != null)
        {
            hud.strength.Changed += Refresh;
            hud.carrier.Changed += Refresh;
        }

        VictoryEconomy.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (hud != null)
        {
            hud.strength.Changed -= Refresh;
            hud.carrier.Changed -= Refresh;
        }

        VictoryEconomy.Changed -= Refresh;
    }

    private void Refresh()
    {
        if (hud == null || strengthText == null)
            return;
        strengthText.text = StrengthNumber.Format(hud.strength.Strength);
        capacityText.text = hud.carrier.InfiniteBlocksActive
            ? "Вместимость: безлимит"
            : "Вместимость: " + hud.carrier.Count + "/" + hud.carrier.Capacity;
        winsText.text = VictoryEconomy.Balance.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
    }
}
