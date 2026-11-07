using System;
using System.Collections.Generic;
using UnityEngine;
using YG;

public static class VictoryEconomy
{
    [Serializable]
    private class Data
    {
        public int balance, revision;
        public List<string> unlocked = new List<string>();
    }

    private const string Key = "BlockBuilding.Victories.v1";
    private static Data data;
    public static void ClearMemory()
    {
        data = null;
    }

    public static event Action Changed;
    private static Data Current
    {
        get
        {
            if (data == null)
                data = Read(PlayerPrefs.GetString(Key, ""));
            return data;
        }
    }

    private static Data Read(string json)
    {
        try
        {
            var d = string.IsNullOrEmpty(json) ? new Data() : JsonUtility.FromJson<Data>(json);
            if (d == null)
                d = new Data();
            d.balance = Mathf.Max(0, d.balance);
            if (d.unlocked == null)
                d.unlocked = new List<string>();
            return d;
        }
        catch
        {
            return new Data();
        }
    }

    public static int Balance
    {
        get
        {
            return Current.balance;
        }
    }

    public static bool Owns(string id)
    {
        return Current.unlocked.Contains(id);
    }

    public static void AwardWin()
    {
        if (Current.balance < int.MaxValue)
            Current.balance++;
        Commit();
    }

    public static bool Unlock(string id, int price)
    {
        if (string.IsNullOrEmpty(id) || price < 0 || Owns(id) || Balance < price)
            return false;
        Current.balance -= price;
        Current.unlocked.Add(id);
        Commit();
        return true;
    }

    private static void Commit()
    {
        Current.revision++;
        string json = JsonUtility.ToJson(Current);
        PlayerPrefs.SetString(Key, json);
        PlayerPrefs.Save();
        if (YG2.isSDKEnabled && YG2.saves != null)
        {
            YG2.saves.victoryEconomyJson = json;
            YG2.SaveProgress();
        }

        Changed?.Invoke();
    }

    public static void SyncCloud()
    {
        if (YG2.saves == null)
            return;
        var remote = Read(YG2.saves.victoryEconomyJson);
        if (remote.revision > Current.revision)
        {
            data = remote;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
        else if (Current.revision > remote.revision)
        {
            YG2.saves.victoryEconomyJson = JsonUtility.ToJson(Current);
            if (YG2.isSDKEnabled)
                YG2.SaveProgress();
        }
    }
}
