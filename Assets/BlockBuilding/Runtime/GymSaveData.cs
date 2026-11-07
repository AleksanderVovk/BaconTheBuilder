using System.Collections.Generic;

namespace YG
{
    public partial class SavesYG
    {
        public List<string> ownedGyms = new List<string>();
        public int gymStrength = -1;
        public string gymStrengthBig = "";
        public bool firstBuildTutorialDone;
        public string victoryEconomyJson = "";
        public string activityChestJson = "";
        public string gameSettingsJson = "";
        public string buildingProgressJson = "";
    }
}
