using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Vector3 direction;

    public float currentMoveSpeed;

    public PlayerCharacteristic _playerCharacteristic;

    [SerializeField] private Player _player;

    private void Awake()
    {
        InitializationComponents();
        InitializationSpeed();
    }

    private void InitializationComponents()
    {
        _player = GetComponent<Player>();
        _playerCharacteristic = GetComponent<PlayerCharacteristic>();
    }

    public void InitializationSpeed()
    {
        // Установка стандартной скорости ходьбы игрока
        currentMoveSpeed = _playerCharacteristic.PLAYER_DEFAULT_MOVE_SPEED;

        UpdateAnimationSpeed();
    }

    public void SetSpeed(float speed)
    {
        currentMoveSpeed = speed;

        UpdateAnimationSpeed();
    }

    public void ChangeSpeed(float multiple, bool positive, bool isDefault)
    {
        if (isDefault)
        {
            currentMoveSpeed = _playerCharacteristic.PLAYER_DEFAULT_MOVE_SPEED;

            UpdateAnimationSpeed();

            return;
        }

        if (positive)
        {
            currentMoveSpeed = _playerCharacteristic.PLAYER_DEFAULT_MOVE_SPEED * multiple;
        }
        else
        {
            currentMoveSpeed = _playerCharacteristic.PLAYER_DEFAULT_MOVE_SPEED / multiple;
        }

        UpdateAnimationSpeed();
    }

    public void UpdateAnimationSpeed()
    {
        float movementAnimationSpeed = currentMoveSpeed;

        if (_player.animator != null)
        _player.animator.SetFloat("MovementAnimationSpeed", movementAnimationSpeed);
    }
}
