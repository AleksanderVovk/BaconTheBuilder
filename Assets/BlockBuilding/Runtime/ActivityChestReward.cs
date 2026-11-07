using System;
using UnityEngine;
using YG;
using BigInteger = System.Numerics.BigInteger;

public class ActivityChestReward : MonoBehaviour
{
    // Legacy numeric fields remain readable. Exact counters are decimal strings in JSON.
    [Serializable]
    private class SaveData
    {
        public int progress, pending;
        public long revision;
        public string pendingBig = "", revisionBig = "", openingsBig = "";
    }

    private class State
    {
        public int progress;
        public BigInteger pending, revision, openings;
    }

    [Header("Session"), Tooltip("Existing world and menu state.")]
    public BuildLevelSession session;

    [Header("Strength"), Tooltip("The player's existing strength progression.")]
    public StrengthUpgradeSystem strength;

    [Header("Goal"), Tooltip("Placed blocks required for each gift."), Min(1)]
    public int blocksPerReward = 100;

    [Header("Starting reward"), Tooltip("Strength granted by each of the first five openings."), Min(1)]
    public int startingReward = 50;

    [Header("Reward interval"), Tooltip("Completed openings before the next reward increase."), Min(1)]
    public int openingsPerIncrease = 5;

    [Header("Reward increase"), Tooltip("Strength added to each subsequent reward tier."), Min(1)]
    public int rewardIncrease = 50;

    [Header("Range"), Tooltip("Horizontal interaction radius."), Min(1)]
    public float radius = 3.5f;

    [Header("Persistence"), Tooltip("Save locally and into YG2 saves.")]
    public bool saveProgress = true;

    [Header("Save key"), Tooltip("Separate chest progress key.")]
    public string saveKey = "BlockBuilding.ActivityChest.v1";

    private State data;
    private bool dirty, subscribed;
    private float nextSave, nextCloud, openedUntil;
    public event Action Changed;
    public event Action<System.Numerics.BigInteger> Claimed;
    public int Progress
    {
        get
        {
            Load();
            return data.progress;
        }
    }

    public BigInteger Pending
    {
        get
        {
            Load();
            return data.pending;
        }
    }

    public bool Opening
    {
        get
        {
            return Time.unscaledTime < openedUntil;
        }
    }

    public BigInteger Openings
    {
        get
        {
            Load();
            return data.openings;
        }
    }

    public BigInteger RewardAmount
    {
        get
        {
            return strength == null
                ? BigInteger.Zero
                : CalculateReward(Openings, startingReward, openingsPerIncrease, rewardIncrease);
        }
    }

    public static BigInteger CalculateReward(BigInteger completedOpenings, int startingReward = 50, int openingsPerIncrease = 5, int rewardIncrease = 50)
    {
        return Math.Max(1, startingReward) + BigInteger.Max(BigInteger.Zero, completedOpenings) / Math.Max(1, openingsPerIncrease) * Math.Max(1, rewardIncrease);
    }

    public bool IsInRange
    {
        get
        {
            if (session == null
                || session.runtime == null
                || session.runtime.carrier == null
                || session.runtime.carrier.player == null)
                return false;
            Vector3 d = session.runtime.carrier.player.position - transform.position;
            return d.x * d.x + d.z * d.z <= radius * radius && Mathf.Abs(d.y) < 4;
        }
    }

    private void Load()
    {
        if (data == null)
            data = Read(saveProgress ? PlayerPrefs.GetString(saveKey, "") : "");
    }

    private State Read(string json)
    {
        SaveData d = null;
        try
        {
            if (!string.IsNullOrEmpty(json))
                d = JsonUtility.FromJson<SaveData>(json);
        }
        catch (ArgumentException)
        {
        }

        if (d == null)
            d = new SaveData();
        BigInteger pending, revision, openings;
        if (!StrengthNumber.TryParse(d.pendingBig, out pending))
            pending = Math.Max(0, d.pending);
        if (!StrengthNumber.TryParse(d.revisionBig, out revision))
            revision = Math.Max(0, d.revision);
        if (!StrengthNumber.TryParse(d.openingsBig, out openings))
            openings = BigInteger.Zero;
        return new State
        {
            openings = openings,
            progress = Mathf.Clamp(d.progress, 0, Mathf.Max(1, blocksPerReward) - 1),
            pending = pending,
            revision = revision
        };
    }

    private void Start()
    {
        Load();
        session.runtime.BlockPlaced += Placed;
        strength.Changed += StrengthChanged;
        YG2.onGetSDKData += SyncCloud;
        subscribed = true;
        if (YG2.isSDKEnabled)
            SyncCloud();
        Changed?.Invoke();
    }

    private void Placed(BlockType type, Vector3 point)
    {
        RecordBlock();
    }

    public void RecordBlock()
    {
        Load();
        data.progress++;
        if (data.progress >= Mathf.Max(1, blocksPerReward))
        {
            data.progress = 0;
            data.pending++;
        }

        Touch();
        Changed?.Invoke();
    }

    public bool TryClaim()
    {
        Load();
        if (strength == null
            || session == null
            || session.MenuOpen
            || session.SettingsOpen
            || session.AdminOverlayOpen
            || (session.training != null
                && session.training.IsTraining)
            || !IsInRange
            || Opening
            || data.pending <= 0)
            return false;
        var before = strength.Strength;
        var amount = RewardAmount;
        if (amount <= 0)
            return false;
        data.pending--;
        data.openings++;
        openedUntil = Time.unscaledTime + 2.2f;
        strength.AddStrength(amount);
        amount = strength.Strength - before;
        Touch();
        Flush(true);
        Changed?.Invoke();
        Claimed?.Invoke(amount);
        return true;
    }

    private void StrengthChanged()
    {
        Changed?.Invoke();
    }

    private void Touch()
    {
        data.revision++;
        dirty = true;
    }

    private void Update()
    {
        if (dirty && Time.unscaledTime >= nextSave)
        {
            nextSave = Time.unscaledTime + 2;
            Flush(Time.unscaledTime >= nextCloud);
        }
    }

    private void Flush(bool cloud)
    {
        if (GameSaveReset.IsResetting)
            return;
        if (!saveProgress || data == null)
            return;
        var serialized = new SaveData
        {
            progress = data.progress,
            pending = StrengthNumber.LegacyValue(data.pending),
            revision = (long)BigInteger.Min(long.MaxValue, data.revision),
            pendingBig = StrengthNumber.Serialize(data.pending),
            revisionBig = StrengthNumber.Serialize(data.revision),
            openingsBig = StrengthNumber.Serialize(data.openings)
        };
        string json = JsonUtility.ToJson(serialized);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();
        dirty = false;
        if (YG2.isSDKEnabled && YG2.saves != null && cloud)
        {
            YG2.saves.activityChestJson = json;
            if (strength != null && !strength.IsTesting)
                strength.WritePlatformSave(YG2.saves);
            YG2.SaveProgress();
            nextCloud = Time.unscaledTime + 10;
        }
        else if (YG2.isSDKEnabled && !cloud)
            dirty = true;
    }

    private void SyncCloud()
    {
        if (!saveProgress || YG2.saves == null)
            return;
        Load();
        var remote = Read(YG2.saves.activityChestJson);
        if (remote.revision > data.revision)
        {
            data = remote;
            dirty = true;
            Flush(false);
            Changed?.Invoke();
        }
        else if (data.revision > remote.revision)
            Flush(true);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            Flush(true);
    }

    private void OnApplicationQuit()
    {
        Flush(true);
    }

    private void OnDisable()
    {
        if (dirty)
            Flush(true);
    }

    private void OnDestroy()
    {
        if (!subscribed)
            return;
        session.runtime.BlockPlaced -= Placed;
        strength.Changed -= StrengthChanged;
        YG2.onGetSDKData -= SyncCloud;
    }
}
