using UnityEngine;

// Все состояния игрока
[System.Serializable]
public enum State
{
    Default,
    Avatar,
    Climbing,
    Car
}

public class PlayerState : MonoBehaviour
{
    public State currentPlayerState;

    public bool canMove;

    public bool isGrounded;
    public bool onPlatform;

    public bool isWalking;
    public bool isRunning;

    public float currentWalkSpeed;
    public float currentRunSpeed;

    // Лестница
    public bool onLadder;
    public bool isClimbing;

    public bool isDeath;
    public bool isPushed;

    public void NewPlayerState(State newState)
    {

    }
}
