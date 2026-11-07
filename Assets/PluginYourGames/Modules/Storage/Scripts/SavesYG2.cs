using System;
using System.Collections.Generic;
using static YG.YG2;

namespace YG
{
    [Serializable]
    public class PlayerData
    {
        public Wallet wallet = new Wallet();

        // ID всех купленных кирок
        public List<string> boughtPickaxes = new List<string>();

        // ID экипированной кирки
        public string equippedPickaxeId = string.Empty;
    }

    [Serializable]
    public class Wallet
    {
        public long money;
        public long diamonds;
        public long wins;
        public long spins;
    }

    [Serializable]
    public partial class SavesYG
    {
        public int idSave;

        public PlayerData playerData;

        public SavesYG()
        {
            playerData = new PlayerData();

            playerData.boughtPickaxes.Add("pickaxe_01");
            playerData.equippedPickaxeId = "pickaxe_01";
        }
    }
}
