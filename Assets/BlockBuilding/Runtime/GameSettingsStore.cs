using System;
using UnityEngine;
using YG;

[DefaultExecutionOrder(-200)]
public class GameSettingsStore : MonoBehaviour
{
    [Serializable]
    public class Settings
    {
        public float sensitivity = 200f;
        public float soundVolume = 1f;
        public float musicVolume = 1f;
        public long revision;
    }

    public const string SaveKey = "BlockBuilding.Settings.v1";

    [Header("Player"), Tooltip("Existing player characteristics; shared mouse and touch sensitivity.")]
    public PlayerCharacteristic player;

    [Header("Music"), Tooltip("Existing music crossfade controller.")]
    public WorldMusicController music;

    [Header("Placement sounds"), Tooltip("Runtime-created construction voices.")]
    public BlockPlacementAudio placement;

    [Header("Sound effects"), Tooltip("Existing sound sources, excluding music voices.")]
    public AudioSource[] soundSources = new AudioSource[0];

    [Header("Authored volumes"), Tooltip("Original source volumes, preserved under the user volume multiplier.")]
    public float[] baseVolumes = new float[0];
    public float SoundVolume { get; private set; } = 1f;

    private Settings saved;
    public Settings Saved
    {
        get
        {
            EnsureLoaded();
            return Copy(saved);
        }
    }

    public event Action Reloaded;
    private void Awake()
    {
        EnsureLoaded();
        RestoreSaved();
    }

    private void OnEnable()
    {
        YG2.onGetSDKData += SyncCloud;
    }

    private void Start()
    {
        if (YG2.isSDKEnabled)
            SyncCloud();
    }

    private void OnDisable()
    {
        YG2.onGetSDKData -= SyncCloud;
    }

    private void EnsureLoaded()
    {
        if (saved != null)
            return;
        saved = Read(PlayerPrefs.GetString(SaveKey, ""));
        if (saved == null)
            saved = new Settings
            {
                sensitivity = player != null ? player.CAMERA_SENSITIVITY : 200f
            };
        Clamp(saved);
    }

    private static Settings Copy(Settings s)
    {
        return new Settings
        {
            sensitivity = s.sensitivity,
            soundVolume = s.soundVolume,
            musicVolume = s.musicVolume,
            revision = s.revision
        };
    }

    private static Settings Read(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;
        try
        {
            var s = JsonUtility.FromJson<Settings>(json);
            if (s == null)
                return null;
            Clamp(s);
            return s;
        }
        catch
        {
            return null;
        }
    }

    private static void Clamp(Settings s)
    {
        s.sensitivity = float.IsNaN(s.sensitivity)
            || float.IsInfinity(s.sensitivity)
            ? 200f
            : Mathf.Clamp(s.sensitivity, 20, 600);
        s.soundVolume = float.IsNaN(s.soundVolume) ? 1 : Mathf.Clamp01(s.soundVolume);
        s.musicVolume = float.IsNaN(s.musicVolume) ? 1 : Mathf.Clamp01(s.musicVolume);
        s.revision = Math.Max(0, s.revision);
    }

    public void Preview(float sensitivity, float soundVolume, float musicVolume)
    {
        SoundVolume = Mathf.Clamp01(soundVolume);
        if (player != null)
            player.CAMERA_SENSITIVITY = Mathf.Clamp(sensitivity, 20, 600);
        for (int i = 0; i < soundSources.Length; i++)
            if (soundSources[i] != null)
                soundSources[i].volume = (i < baseVolumes.Length ? baseVolumes[i] : 1f) * Mathf.Clamp01(soundVolume);
        if (music != null)
            music.SetUserVolume(musicVolume);
        if (placement != null)
            placement.SetUserVolume(soundVolume);
    }

    public void RestoreSaved()
    {
        EnsureLoaded();
        Preview(saved.sensitivity, saved.soundVolume, saved.musicVolume);
    }

    public void Save(float sensitivity, float soundVolume, float musicVolume)
    {
        EnsureLoaded();
        saved = new Settings
        {
            sensitivity = sensitivity,
            soundVolume = soundVolume,
            musicVolume = musicVolume,
            revision = Math.Max(saved.revision + 1, DateTime.UtcNow.Ticks)
        };
        Clamp(saved);
        RestoreSaved();
        WriteLocal();
        if (YG2.saves != null)
        {
            YG2.saves.gameSettingsJson = JsonUtility.ToJson(saved);
            if (YG2.isSDKEnabled)
                YG2.SaveProgress();
        }
    }

    private void WriteLocal()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(saved));
        PlayerPrefs.Save();
    }

    private void SyncCloud()
    {
        if (YG2.saves == null)
            return;
        EnsureLoaded();
        var remote = Read(YG2.saves.gameSettingsJson);
        if (remote != null && remote.revision > saved.revision)
        {
            saved = remote;
            WriteLocal();
            RestoreSaved();
            Reloaded?.Invoke();
        }
        else if (saved.revision > 0 && (remote == null || remote.revision < saved.revision))
        {
            YG2.saves.gameSettingsJson = JsonUtility.ToJson(saved);
            if (YG2.isSDKEnabled)
                YG2.SaveProgress();
        }
    }
}
