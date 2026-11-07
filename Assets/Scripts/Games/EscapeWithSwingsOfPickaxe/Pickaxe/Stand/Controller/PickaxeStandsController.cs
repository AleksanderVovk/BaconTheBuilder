using System.Collections.Generic;
using UnityEngine;
using YG;

public class PickaxeStandsController : MonoBehaviour
{
    public static PickaxeStandsController Instance { get; private set; }

    [Header("Stands")]
    [SerializeField] private List<PickaxeStand> _stands = new List<PickaxeStand>();

    [Header("Settings")]
    [SerializeField] private bool _firstPickaxeAvailable = true;

    private readonly HashSet<string> _boughtPickaxes = new HashSet<string>();

    private string _equippedPickaxeId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        LoadData();
        RefreshAllStands();

        // ≈сли экипированна€ кирка отсутствует,
        // автоматически экипируем первую купленную
        if (string.IsNullOrEmpty(_equippedPickaxeId))
        {
            EquipFirstBoughtPickaxe();
        }

        RefreshAllStands();
    }

    private void LoadData()
    {
        if (YG2.saves == null)
        {
            Debug.LogError("YG2.saves равен null. ѕроверь инициализацию Yandex Game.");
            return;
        }

        if (YG2.saves.playerData == null)
        {
            YG2.saves.playerData = new PlayerData();
        }

        if (YG2.saves.playerData.boughtPickaxes == null)
        {
            YG2.saves.playerData.boughtPickaxes = new List<string>();
        }

        _boughtPickaxes.Clear();

        foreach (string pickaxeId in YG2.saves.playerData.boughtPickaxes)
        {
            if (!string.IsNullOrEmpty(pickaxeId))
            {
                _boughtPickaxes.Add(pickaxeId);
            }
        }

        _equippedPickaxeId = YG2.saves.playerData.equippedPickaxeId;
    }

    public void RefreshAllStands()
    {
        for (int i = 0; i < _stands.Count; i++)
        {
            if (_stands[i] != null)
            {
                _stands[i].RefreshStand();
            }
        }
    }

    public bool IsPickaxeBought(string pickaxeId)
    {
        if (string.IsNullOrEmpty(pickaxeId))
        {
            return false;
        }

        return _boughtPickaxes.Contains(pickaxeId);
    }

    public bool IsPickaxeEquipped(string pickaxeId)
    {
        if (string.IsNullOrEmpty(pickaxeId))
        {
            return false;
        }

        return _equippedPickaxeId == pickaxeId;
    }

    public bool CanBuyPickaxe(PickaxeStand stand)
    {
        if (stand == null || stand.PickaxeData == null)
        {
            return false;
        }

        if (IsPickaxeBought(stand.PickaxeId))
        {
            return false;
        }

        // ѕерва€ кирка может быть доступна сразу
        if (stand.IsFirstPickaxe)
        {
            return true;
        }

        PickaxeStand previousStand = GetPreviousStand(stand);

        if (previousStand == null)
        {
            return false;
        }

        return IsPickaxeBought(previousStand.PickaxeId);
    }

    public bool TryBuyPickaxe(PickaxeStand stand)
    {
        if (stand == null || stand.PickaxeData == null)
        {
            return false;
        }

        if (!CanBuyPickaxe(stand))
        {
            return false;
        }

        // —писание кубков
        bool successfullySubtracted =
            ValueSystem.SubstractionBalance(stand.PickaxeData.price.amount, stand.PickaxeData.price.valueType);

        if (!successfullySubtracted)
        {
            return false;
        }

        AddBoughtPickaxe(stand.PickaxeId);

        SaveData();

        RefreshAllStands();

        return true;
    }

    private void AddBoughtPickaxe(string pickaxeId)
    {
        if (string.IsNullOrEmpty(pickaxeId))
        {
            return;
        }

        _boughtPickaxes.Add(pickaxeId);

        if (!YG2.saves.playerData.boughtPickaxes.Contains(pickaxeId))
        {
            YG2.saves.playerData.boughtPickaxes.Add(pickaxeId);
        }
    }

    public void EquipPickaxe(PickaxeStand stand)
    {
        if (stand == null || stand.PickaxeData == null)
        {
            return;
        }

        if (!IsPickaxeBought(stand.PickaxeId))
        {
            return;
        }

        _equippedPickaxeId = stand.PickaxeId;
        YG2.saves.playerData.equippedPickaxeId = _equippedPickaxeId;

        SaveData();
        RefreshAllStands();
    }

    private void EquipFirstBoughtPickaxe()
    {
        for (int i = 0; i < _stands.Count; i++)
        {
            PickaxeStand stand = _stands[i];

            if (stand == null)
            {
                continue;
            }

            if (IsPickaxeBought(stand.PickaxeId))
            {
                _equippedPickaxeId = stand.PickaxeId;
                YG2.saves.playerData.equippedPickaxeId = _equippedPickaxeId;
                SaveData();
                return;
            }
        }
    }

    public PickaxeStand GetPreviousStand(PickaxeStand currentStand)
    {
        if (currentStand == null)
        {
            return null;
        }

        int index = _stands.IndexOf(currentStand);

        if (index <= 0)
        {
            return null;
        }

        return _stands[index - 1];
    }

    private void SaveData()
    {
        /*
         * ќставь здесь тот метод сохранени€,
         * который используетс€ в твоей версии YG2.
         *
         * ¬ большинстве проектов YG2 сохранение выполн€етс€ автоматически,
         * но если у теб€ есть ручной метод сохранени€, вызови его здесь.
         */
    }

    public IReadOnlyList<PickaxeStand> GetStands()
    {
        return _stands;
    }
}