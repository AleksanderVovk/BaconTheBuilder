using TMPro;
using UnityEngine;

public class GymVictoryPrice : MonoBehaviour
{
    [Header("Hub"), Tooltip("Existing station ownership.")]
    public GymEquipmentHub hub;

    [Header("Station"), Tooltip("Station being priced.")]
    public GymEquipment station;

    [Header("Price"), Tooltip("Gold caption.")]
    public TMP_Text caption;

    [Header("Cup"), Tooltip("World-space victory icon.")]
    public GameObject cup;

    [Header("Price plate"), Tooltip("Separate background, hidden after unlocking.")]
    public GameObject pricePlate;
    private void OnEnable()
    {
        VictoryEconomy.Changed += Refresh;
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        VictoryEconomy.Changed -= Refresh;
    }

    private void Refresh()
    {
        if (station == null || caption == null)
            return;
        bool owned = hub.Owns(station);
        caption.text = owned ? "" : "Разблокировать " + station.victoryPrice;
        if (cup != null)
            cup.SetActive(!owned);
        if (pricePlate != null)
            pricePlate.SetActive(!owned);
    }
}
