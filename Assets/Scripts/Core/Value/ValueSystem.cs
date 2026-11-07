using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YG;


[System.Serializable]
public enum ValueType
{
    Coin,
    Diamond,
    Wins,
    Spin
}

[System.Serializable]
public class Price
{
    public int amount;
    public ValueType valueType;
}

public class ValueSystem : MonoBehaviour
{
    public static ValueSystem instance;

    public static event Action OnBalanceChanged;

    [Space(15f)]

    [Header("¬ид")]
    [Space(5f)]
    [SerializeField] private TMP_Text _balanceView_coin;
    [SerializeField] private TMP_Text _balanceView_diamond;
    [SerializeField] private TMP_Text _balanceView_spin;

    public bool thisX2Money;

    private void Awake()
    {
        // SINGLETON
        instance = this;
    }

    private void Start()
    {
        InitializationBalance();
    }

    public void InitializationBalance()
    {
        // »нициализировать тут баланс

        UpdateBalance();
    }

    /// <summary>
    /// ƒобавление баланса к общему кошельку игрока
    /// </summary>
    /// <param name="amount"></param>
    public static void AddBalance(long amount, ValueType valueType) => instance._AddBalance(amount, valueType);
    public void _AddBalance(long amount, ValueType valueType)
    {
        long totalAmount;

        if (amount >= 5)
        {
            totalAmount = amount / 25;

            if (totalAmount > 10)
                totalAmount = 10;
        }
        else
        {
            totalAmount = 1;
        }

        switch (valueType)
        {
            case ValueType.Coin:

                YG2.saves.playerData.wallet.money += amount;

                YG2.SetLeaderboard("TopMoney", (int)YG2.saves.playerData.wallet.money);

                break;


            case ValueType.Diamond:

                YG2.saves.playerData.wallet.diamonds += amount;

                break;

            case ValueType.Spin:

                YG2.saves.playerData.wallet.spins += amount;

                break;
        }

        UpdateBalance();

        YG2.SaveProgress();
    }

    /// <summary>
    /// ¬ычитание баланса из общего кошелька игрока
    /// </summary>
    /// <param name="amount"></param>
    /// <returns>¬озвращает true/false (true - если общий баланс больше или равен вычитаемому значению, false - если общий баланс меньше чем вычитаемое значение</returns>
    public static bool SubstractionBalance(long amount, ValueType valueType) => instance._SubstractionBalance(amount, valueType);
    public bool _SubstractionBalance(long amount, ValueType valueType)
    {
        switch (valueType)
        {
            case ValueType.Coin:

                if (YG2.saves.playerData.wallet.money >= amount)
                {
                    YG2.saves.playerData.wallet.money -= amount;

                    UpdateBalance();

                    return true;
                }

                break;

            case ValueType.Diamond:

                if (YG2.saves.playerData.wallet.diamonds >= amount)
                {
                    YG2.saves.playerData.wallet.diamonds -= amount;

                    UpdateBalance();

                    return true;
                }

                break;

            case ValueType.Spin:

                if (YG2.saves.playerData.wallet.spins >= amount)
                {
                    YG2.saves.playerData.wallet.spins -= amount;

                    UpdateBalance();

                    return true;
                }

                break;
        }

        switch (YG2.envir.language)
        {
            case "ru":
                NotificationManager.ShowNotification("” вас недостаточно средств!", NotificationType.Error);
                break;
            case "en":
                NotificationManager.ShowNotification("You don't have enough funds!", NotificationType.Error);
                break;
            case "tr":
                NotificationManager.ShowNotification("Yeterli paraniz yok!", NotificationType.Error);
                break;
        }

        return false;
    }

    public static bool HaveEnoughMoney(Price price) => instance._HaveEnoughMoney(price);
    public bool _HaveEnoughMoney(Price price)
    {
        switch (price.valueType)
        {
            case ValueType.Coin:

                if (YG2.saves.playerData.wallet.money >= price.amount)
                {
                    return true;
                }

                break;

            case ValueType.Diamond:

                if (YG2.saves.playerData.wallet.diamonds >= price.amount)
                {
                    return true;
                }

                break;
        }

        return false;
    }

    // ќбновление баланса
    private void UpdateBalance()
    {
        if (_balanceView_coin != null) _balanceView_coin.text = "$" + UTF.AbbreviateNumber(YG2.saves.playerData.wallet.money);

        if (_balanceView_diamond != null) _balanceView_diamond.text = YG2.saves.playerData.wallet.diamonds.ToString();

        if (_balanceView_spin != null) _balanceView_spin.text = YG2.saves.playerData.wallet.spins.ToString();

        OnBalanceChanged?.Invoke();
    }
}
