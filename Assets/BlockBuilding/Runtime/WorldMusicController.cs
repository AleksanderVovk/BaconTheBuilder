using System;
using UnityEngine;
using UnityEngine.Audio;

// Lives outside the HUD so opening a menu never disables the crossfade.
public class WorldMusicController : MonoBehaviour
{
    [Serializable]
    public class LevelTrack
    {
        [Header("Level ID"), Tooltip("Stable ID from the existing level catalog.")]
        public string levelId = "";

        [Header("Name"), Tooltip("Inspector label for this construction.")]
        public string title = "";

        [Header("Music"), Tooltip("Looped music in this construction world. Empty means silence.")]
        public AudioClip music;
    }

    [Header("Session"), Tooltip("Existing level selection and menu events.")]
    public BuildLevelSession session;

    [Header("Menu music"), Tooltip("Looped music while the level menu is open.")]
    public AudioClip menuMusic;

    [Header("Construction music"), Tooltip("Assign music for each stable level ID.")]
    public LevelTrack[] levelTracks = new LevelTrack[0];

    [Header("Transition seconds"), Tooltip("Full duration of the smooth crossfade, unaffected by game pause."), Min(0)]
    public float transitionSeconds = 1.5f;

    [Header("Volume"), Tooltip("Overall music loudness, independent of sound effects."), Range(0, 1)]
    public float volume = .35f;

    [Header("Mixer"), Tooltip("Optional existing music mixer group.")]
    public AudioMixerGroup output;

    [Header("Resume playback"), Tooltip("Continue a track from its paused position when returning. Disable to restart it.")]
    public bool resumePlayback = true;

    private float userVolume = 1f;
    public float UserVolume
    {
        get
        {
            return userVolume;
        }
    }

    public void SetUserVolume(float value)
    {
        userVolume = Mathf.Clamp01(value);
        if (voices != null)
            for (int i = 0; i < voices.Length; i++)
                if (voices[i] != null)
                    voices[i].volume = gains[i] * volume * userVolume;
    }

    private AudioSource[] voices;
    private float[] gains, starts;
    private bool[] started;
    private AudioClip current;
    private int target = -1;
    private float elapsed;
    private bool fading, initialized, hasSelection;
    public AudioClip CurrentTrack
    {
        get
        {
            return current;
        }
    }

    public bool IsTransitioning
    {
        get
        {
            return fading;
        }
    }

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized)
            return;
        int count = levelTracks.Length + 1;
        voices = new AudioSource[count];
        gains = new float[count];
        starts = new float[count];
        started = new bool[count];
        for (int i = 0; i < count; i++)
        {
            var voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.loop = true;
            voice.spatialBlend = 0;
            voice.volume = 0;
            voice.outputAudioMixerGroup = output;
            voices[i] = voice;
        }

        initialized = true;
    }

    private void OnEnable()
    {
        if (session != null)
            session.Changed += RefreshMusic;
    }

    private void Start()
    {
        RefreshMusic();
    }

    public void RefreshMusic()
    {
        if (!isActiveAndEnabled || session == null)
            return;
        Initialize();
        AudioClip wanted = menuMusic;
        if (!session.MenuOpen && session.Selected != null)
        {
            wanted = null;
            foreach (var entry in levelTracks)
                if (entry != null && entry.levelId == session.Selected.id)
                {
                    wanted = entry.music;
                    break;
                }
        }

        if (hasSelection && wanted == current)
            return;
        hasSelection = true;
        current = wanted;
        target = -1;
        for (int i = 0; i < voices.Length; i++)
            if (wanted != null && voices[i].clip == wanted)
            {
                target = i;
                break;
            }

        if (wanted != null && target < 0)
        {
            for (int i = 0; i < voices.Length; i++)
                if (voices[i].clip == null)
                {
                    target = i;
                    break;
                }

            // One reserved voice per configured track guarantees space, including rapid switching.
            if (target < 0)
                for (int i = 0; i < voices.Length; i++)
                    if (gains[i] <= .0001f && !voices[i].isPlaying)
                    {
                        target = i;
                        break;
                    }

            if (target >= 0)
            {
                voices[target].clip = wanted;
                voices[target].volume = 0;
                started[target] = false;
            }
        }

        if (target >= 0 && !voices[target].isPlaying)
        {
            if (resumePlayback && started[target])
                voices[target].UnPause();
            else
            {
                voices[target].Stop();
                voices[target].Play();
                started[target] = true;
            }
        }

        Array.Copy(gains, starts, gains.Length);
        elapsed = 0;
        fading = true;
        if (transitionSeconds <= 0)
            Tick(1);
    }

    private void Update()
    {
        if (!initialized)
            return;
        if (fading)
        {
            elapsed += Time.unscaledDeltaTime;
            Tick(transitionSeconds <= 0 ? 1 : Mathf.Clamp01(elapsed / transitionSeconds));
        }
        else
            for (int i = 0; i < voices.Length; i++)
                voices[i].volume = gains[i] * volume * userVolume;
    }

    private void Tick(float t)
    {
        float blend = t * t * (3 - 2 * t);
        for (int i = 0; i < voices.Length; i++)
        {
            gains[i] = Mathf.Lerp(starts[i], i == target ? 1 : 0, blend);
            voices[i].volume = gains[i] * volume * userVolume;
            if (t >= 1 && i != target && voices[i].isPlaying)
                voices[i].Pause();
        }

        if (t >= 1)
            fading = false;
    }

    private void OnDisable()
    {
        if (session != null)
            session.Changed -= RefreshMusic;
        if (voices != null)
            foreach (var v in voices)
                if (v != null)
                    v.Stop();
        if (gains != null)
            Array.Clear(gains, 0, gains.Length);
        if (started != null)
            Array.Clear(started, 0, started.Length);
        hasSelection = false;
        current = null;
        target = -1;
        fading = false;
    }

    private void OnDestroy()
    {
        if (voices != null)
            foreach (var voice in voices)
                if (voice != null)
                    Destroy(voice);
    }
}
