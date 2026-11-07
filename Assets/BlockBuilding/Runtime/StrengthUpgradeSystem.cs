using System;
using UnityEngine;
using BigInteger = System.Numerics.BigInteger;

[Serializable]
public class StrengthLevel
{
    [Header("Threshold"), Tooltip("Total strength required to reach this level."), Min(0)]
    public int requiredStrength;

    [Header("Capacity"), Tooltip("Maximum carried cubes at this level."), Range(1, 9999)]
    public int capacity = 1;
}

public class StrengthUpgradeSystem : MonoBehaviour
{
    [Header("Progression"), Tooltip("Increasing thresholds, starting at zero. Capacity cannot decrease.")]
    public StrengthLevel[] levels =
    {
        new StrengthLevel
        {
            requiredStrength = 0,
            capacity = 1
        },
        new StrengthLevel
        {
            requiredStrength = 5,
            capacity = 2
        },
        new StrengthLevel
        {
            requiredStrength = 15,
            capacity = 3
        },
        new StrengthLevel
        {
            requiredStrength = 30,
            capacity = 4
        },
        new StrengthLevel
        {
            requiredStrength = 50,
            capacity = 5
        },
        new StrengthLevel
        {
            requiredStrength = 80,
            capacity = 6
        },
        new StrengthLevel
        {
            requiredStrength = 120,
            capacity = 7
        },
        new StrengthLevel
        {
            requiredStrength = 170,
            capacity = 8
        }
    };

    [Header("Persistence"), Tooltip("Store exact decimal strength locally and in the existing cloud save.")]
    public bool saveProgress = true;

    [Header("Save key"), Tooltip("Old integer key remains readable; exact numbers use the .Big.v1 suffix.")]
    public string saveKey = "BlockBuilding.Strength.v1";

    [Header("Feedback"), Tooltip("Existing level-up sound source.")]
    public AudioSource levelAudio;

    [Header("Effect"), Tooltip("Optional level-up particle system.")]
    public ParticleSystem levelEffect;

    [Header("Level sound"), Tooltip("Existing level-up clip.")]
    public AudioClip levelUpClip;

    [Header("Extended progression"), Tooltip("Keep gaining strength and levels after the authored table.")]
    public bool extendLevels = true;

    [Header("Extra level cost"), Tooltip("Additional cost growth for extended levels."), Min(1)]
    public int extraCostGrowth = 25;

    private BigInteger strength, beforeTesting, extraLevels;
    private int index;
    private bool initialized, testing;
    public event Action Changed;
    public event Action LeveledUp;
    public bool IsTesting
    {
        get
        {
            return testing;
        }
    }

    public string ExactSaveKey
    {
        get
        {
            return saveKey + ".Big.v1";
        }
    }

    public BigInteger Strength
    {
        get
        {
            Initialize();
            return strength;
        }
    }

    public BigInteger Level
    {
        get
        {
            Initialize();
            return index + BigInteger.One + extraLevels;
        }
    }

    public int Capacity
    {
        get
        {
            Initialize();
            if (extraLevels >= PlayerBlockCarrier.AdminMaximum)
                return PlayerBlockCarrier.AdminMaximum;
            return Mathf.Min(PlayerBlockCarrier.AdminMaximum, levels[index].capacity + (int)extraLevels);
        }
    }

    public int MaximumCapacity
    {
        get
        {
            return extendLevels
                ? PlayerBlockCarrier.AdminMaximum
                : Mathf.Clamp(levels[levels.Length - 1].capacity, 1, PlayerBlockCarrier.AdminMaximum);
        }
    }

    public BigInteger NextThreshold
    {
        get
        {
            Initialize();
            if (index + 1 < levels.Length)
                return levels[index + 1].requiredStrength;
            return extendLevels ? TailThreshold(extraLevels + 1) : -BigInteger.One;
        }
    }

    public float Progress
    {
        get
        {
            BigInteger next = NextThreshold;
            if (next.Sign < 0)
                return 1;
            BigInteger current = index < levels.Length - 1 ? levels[index].requiredStrength : TailThreshold(extraLevels);
            // Fixed-point division avoids float infinity/NaN for arbitrarily large values.
            BigInteger fraction = (strength - current) * 1000000 / BigInteger.Max(BigInteger.One, next - current);
            return (float)(int)BigInteger.Min(1000000, BigInteger.Max(BigInteger.Zero, fraction)) / 1000000f;
        }
    }

    private BigInteger TailThreshold(BigInteger extra)
    {
        int last = levels.Length - 1;
        int step = last > 0 ? Math.Max(1, levels[last].requiredStrength - levels[last - 1].requiredStrength) : 5;
        return levels[last].requiredStrength + (BigInteger)step * extra + (BigInteger)Math.Max(1, extraCostGrowth) * extra * (extra + 1) / 2;
    }

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (initialized)
            return;
        if (levels == null || levels.Length == 0 || levels[0] == null || levels[0].requiredStrength != 0)
            throw new ArgumentException("Strength table must start at zero.");
        for (int i = 0; i < levels.Length; i++)
            if (levels[i] == null
                || levels[i].capacity < 1
                || levels[i].capacity > PlayerBlockCarrier.AdminMaximum
                || (i > 0
                    && (levels[i].requiredStrength <= levels[i - 1].requiredStrength
                        || levels[i].capacity < levels[i - 1].capacity)))
                throw new ArgumentException("Invalid strength progression table.");
        strength = BigInteger.Zero;
        if (saveProgress)
        {
            if (!StrengthNumber.TryParse(PlayerPrefs.GetString(ExactSaveKey, ""), out strength))
                strength = Mathf.Max(0, PlayerPrefs.GetInt(saveKey, 0));
        }

        Recalculate();
        initialized = true;
    }

    public void ApplySavedStrength(BigInteger value)
    {
        Initialize();
        if (testing || value <= strength)
            return;
        strength = value;
        Recalculate();
        Persist();
        Changed?.Invoke();
    }

    public void AddStrength(BigInteger amount)
    {
        Initialize();
        if (amount.Sign <= 0)
            return;
        BigInteger previous = Level;
        strength += amount;
        Recalculate();
        Persist();
        Changed?.Invoke();
        if (Level != previous)
        {
            if (levelAudio != null && levelUpClip != null)
                levelAudio.PlayOneShot(levelUpClip, .5f);
            if (levelEffect != null)
                levelEffect.Play();
            LeveledUp?.Invoke();
        }
    }

    public void SetStrengthForTesting(BigInteger value)
    {
        Initialize();
        if (!testing)
        {
            beforeTesting = strength;
            testing = true;
        }

        strength = BigInteger.Max(BigInteger.Zero, value);
        Recalculate();
        Changed?.Invoke();
    }

    public void RestoreStrengthAfterTesting()
    {
        if (!testing)
            return;
        strength = beforeTesting;
        testing = false;
        Recalculate();
        Changed?.Invoke();
    }

    public void WritePlatformSave(YG.SavesYG saves)
    {
        if (GameSaveReset.IsResetting || saves == null || testing)
            return;
        saves.gymStrengthBig = StrengthNumber.Serialize(Strength);
        saves.gymStrength = StrengthNumber.LegacyValue(Strength);
    }

    private void Persist()
    {
        if (GameSaveReset.IsResetting || !saveProgress || testing)
            return;
        PlayerPrefs.SetString(ExactSaveKey, StrengthNumber.Serialize(strength));
        PlayerPrefs.SetInt(saveKey, StrengthNumber.LegacyValue(strength));
    }

    private void Recalculate()
    {
        index = 0;
        extraLevels = BigInteger.Zero;
        while (index + 1 < levels.Length && strength >= levels[index + 1].requiredStrength)
            index++;
        if (!extendLevels || index != levels.Length - 1)
            return;
        int last = levels.Length - 1;
        BigInteger step = last > 0 ? Math.Max(1, levels[last].requiredStrength - levels[last - 1].requiredStrength) : 5;
        BigInteger growth = Math.Max(1, extraCostGrowth), b = growth + 2 * step;
        BigInteger discriminant = b * b + 8 * growth * (strength - levels[last].requiredStrength);
        extraLevels = BigInteger.Max(BigInteger.Zero, (StrengthNumber.SquareRoot(discriminant) - b) / (2 * growth));
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && saveProgress)
            PlayerPrefs.Save();
    }

    private void OnDisable()
    {
        if (saveProgress && initialized)
            PlayerPrefs.Save();
    }
}
