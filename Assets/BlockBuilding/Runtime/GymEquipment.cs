using UnityEngine;

public class GymEquipment : MonoBehaviour
{
    [Header("Identity"), Tooltip("Stable save identifier.")]
    public string equipmentId = "standard";

    [Header("Name"), Tooltip("Player-facing station name.")]
    public string title = "Стандартный";

    [Header("Reward"), Tooltip("Strength multiplier for completed repetitions."), Min(1)]
    public int multiplier = 1;

    [Header("Pose"), Tooltip("Lying pose for this model.")]
    public Transform pose;

    [Header("Barbell"), Tooltip("Animated model part.")]
    public Transform bar;

    [Header("Press position"), Tooltip("Bench-local offset from the racked bar to the unobstructed lifting position.")]
    public Vector3 pressOffset = new Vector3(0, 0, .4f);

    [Header("Unrack clearance"), Tooltip("Lift above the rack hooks before moving the bar towards the chest."), Min(0)]
    public float unrackLift = .22f;

    [Header("Victory price"), Tooltip("One-time unlock cost in wins."), Min(0)]
    public int victoryPrice = 1;

    [Header("Product"), Tooltip("Prepared YG2 Donate product ID. Create the matching product in Yandex Games.")]
    public string productId = "";
}
