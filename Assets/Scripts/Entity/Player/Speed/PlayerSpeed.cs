using UnityEngine;

[System.Serializable]
public class AdditionalAttributes
{
    public float speed; // additional speed (прибавка к PLAYER_DEFAULT_MOVE_SPEED)
}

public class PlayerSpeed : MonoBehaviour
{
    [Header("Ссылки")]
    public Player player;
    public PlayerMovement playerMovement;
    public PlayerCharacteristic playerCharacteristics;

    [Header("Атрибуты")]
    public AdditionalAttributes additionalAttributes;

    // null = не закреплено (всегда максимум из additionalAttributes.speed)
    // value = закреплено на конкретном additional speed
    private float? _speedOverride;

    [Header("Множители")]
    public bool x2Speed;

    private void Awake()
    {
        if (player == null) player = GetComponent<Player>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (playerCharacteristics == null) playerCharacteristics = GetComponent<PlayerCharacteristic>();
    }

    #region Инициализация

    public void InitRealPlayer()
    {
        //additionalAttributes = YG2.saves.playerData.attributes ?? new AdditionalAttributes();
        additionalAttributes = new AdditionalAttributes();

        // по умолчанию НЕ закрепляем, значит будет максимум
        _speedOverride = null;

        ApplySpeedFromAttributes();
    }

    #endregion


    public void SetMinMoveSpeed()
    {
        _speedOverride = 0.1f;
        ApplySpeedFromAttributes();
    }

    public void ApplySpeedFromAttributes()
    {
        if (playerMovement == null || playerCharacteristics == null)
            return;

        float add = _speedOverride ?? (additionalAttributes != null ? additionalAttributes.speed : 0f);
        float baseSpeed = playerCharacteristics.PLAYER_DEFAULT_MOVE_SPEED + add;

        playerMovement.SetSpeed(x2Speed ? baseSpeed * 2f : baseSpeed);
    }

    public void SetX2Speed(bool value)
    {
        x2Speed = value;
        ApplySpeedFromAttributes();
    }
}
