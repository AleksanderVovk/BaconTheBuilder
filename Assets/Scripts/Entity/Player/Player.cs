using TMPro; 
using UnityEngine;

[System.Serializable]
public enum Gender
{
    None,
    Man,
    Women
}

public class Player : MonoBehaviour
{
    public GameObject player;
    public Transform spawnPoint;

    [Header("Атрибуты и скорость")]
    public PlayerSpeed playerSpeed;
    public PlayerCharacteristic playerCharacteristics;

    [Header("Игровые данные")]
    public int sessionWins;
    public Gender gender;
    public bool thisRealPlayer;

    [Space(10f)]
    [Header("Ссылки")]
    [Space(2.5f)]
    private CharacterController _controller;
    public Animator animator;

    public PlayerState playerState;
    public PlayerMovement playerMovement;
    //public NPCController npcController;
    public PlayerController playerController;
    //public AI ai;

    [Header("Респаун")]
    public Transform checkpoint;

    [Header("Никнейм")]
    public string nickname;
    public TMP_Text _nicknameView;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        //playerCloth = GetComponent<PlayerCloth>();

        if (playerSpeed == null) playerSpeed = GetComponent<PlayerSpeed>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (playerCharacteristics == null) playerCharacteristics = GetComponent<PlayerCharacteristic>();

        if (thisRealPlayer)
        {
            //playerController = GetComponent<PlayerController>();
        }
        else
        {
            //ai = GetComponent<AI>();
        }
    }

    #region Инициализация

    public void InitializationPlayer()
    {
        // Загрузка гендера игрока
        //gender = YG2.saves.playerData.gender;

        Debug.Log("Инициализация игрока");

        if (playerSpeed != null)
        {
            // Просто берём то, что уже лежит в сохранении
            playerSpeed.InitRealPlayer();

            // НЕ трогаем SetSpeedFromLevel здесь, если правим speed напрямую в сейве
            // playerSpeed.SetSpeedFromLevel();

        }

        //playerCloth.Initialization();
    }


    public void InitializationNPC()
    {
        InitializationNickName();

        if (playerSpeed != null)
        {
            // Инициализация скорости и силы НПС + установка в NavMeshAgent
            //playerSpeed.InitNPC(ai, npcController);
        }

        //playerCloth.ApplyRandomBotSkin();

        Debug.Log("Инициализация НПС");
    }

    private void InitializationNickName()
    {
        if (_nicknameView != null)
        {
            _nicknameView.gameObject.SetActive(true);
            nickname = PlayersManager.GetRandomNickName();
            _nicknameView.text = nickname;
        }
    }

    #endregion

    #region Телепорт / падение / респаун

    public void TeleportPlayer(Vector3 position)
    {
        ControllerState(false);

        player.transform.position = position;

        ControllerState(true);
    }

    public void TeleportToSpawn()
    {
        TeleportPlayer(spawnPoint.position);

         //if (npcController != null)
            //npcController.Death();
    }

    public void Respawn()
    {
        if (checkpoint == null)
        {
            TeleportToSpawn();
        }
    }

    #endregion

    #region Контроллер

    public void ControllerState(bool value)
    {
        _controller.enabled = value;
    }

    #endregion
}
