using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayersManager : MonoBehaviour
{
    public static PlayersManager instance;

    public MeshRenderer spawnBounds;

    public Transform sellPoint;

    public Player realPlayer;
    public Player[] allPlayers;

    public string[] playersNicknames;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        if (allPlayers.Length <= 0)
            Debug.Log("Вы не добавили игроков в {PlayersManager}");
    }

    public void ReadNames()
    {
        // Загружаем текстовый файл из папки Resources
        TextAsset textAsset = Resources.Load<TextAsset>("names"); // Указываем имя файла без расширения

        if (textAsset != null)
        {
            // Разделяем текст на строки и сохраняем в массив
            playersNicknames = textAsset.text.Split('\n');
        }
        else
        {
            Debug.LogError("Файл не найден!");
        }
    }

    public static string GetRandomNickName() => instance._GetRandomNickName();
    public string _GetRandomNickName()
    {
        return playersNicknames[Random.Range(0, playersNicknames.Length)];
    }

    public static Vector3 GetRealPlayerPosition() => instance._GetRealPlayerPosition();
    public Vector3 _GetRealPlayerPosition()
    {
        return realPlayer.transform.position;
    }

    public void InitializationAllPlayers()
    {
        foreach (Player player in allPlayers)
        {
            if (player.thisRealPlayer)
            {
                realPlayer = player;
                player.InitializationPlayer();
            }
            else
            {
                player.InitializationNPC();
            }
        }
    }

    public static Player[] GetAllPlayers() => instance._GetAllPlayers();
    public Player[] _GetAllPlayers()
    {
        return allPlayers;
    }
}
