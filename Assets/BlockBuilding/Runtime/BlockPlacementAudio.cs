using UnityEngine;

public class BlockPlacementAudio : MonoBehaviour
{
    [Header("Construction"), Tooltip("Receives successful block placement events only.")]
    public BlockLevelRuntime runtime;

    [Header("Stone"), Tooltip("Short original dry stone impacts.")]
    public AudioClip[] stone = new AudioClip[0];

    [Header("Wood"), Tooltip("Wooden block impacts.")]
    public AudioClip[] wood = new AudioClip[0];

    [Header("Glass"), Tooltip("Soft glass placement ticks.")]
    public AudioClip[] glass = new AudioClip[0];

    [Header("Soft materials"), Tooltip("Grass, cloth, snow and water feedback.")]
    public AudioClip[] soft = new AudioClip[0];

    [Header("Sand"), Tooltip("Minecraft sand placement variants.")]
    public AudioClip[] sand = new AudioClip[0];

    [Header("Dirt"), Tooltip("Minecraft gravel/dirt placement variants.")]
    public AudioClip[] dirt = new AudioClip[0];

    [Header("Snow"), Tooltip("Minecraft snow placement variants.")]
    public AudioClip[] snow = new AudioClip[0];

    [Header("Cloth"), Tooltip("Minecraft wool placement variants for flags.")]
    public AudioClip[] cloth = new AudioClip[0];

    [Header("Volume"), Tooltip("Placement feedback volume."), Range(0, 1)]
    public float volume = 0.45f;

    [Header("Rate limit"), Tooltip("Prevents hundreds of simultaneous sounds during admin fast-build."), Min(0.02f)]
    public float minimumInterval = 0.045f;

    private readonly AudioSource[] voices = new AudioSource[3];
    private float userVolume = 1f;
    public void SetUserVolume(float value)
    {
        userVolume = Mathf.Clamp01(value);
        foreach (var source in voices)
            if (source != null)
                source.volume = volume * userVolume;
    }

    private int voice;
    private float nextSound;
    public int PlayedCount { get; private set; }

    private void Awake()
    {
        for (int i = 0; i < voices.Length; i++)
        {
            var go = new GameObject("PlacementVoice_" + i);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            voices[i] = source;
        }
    }

    private void OnEnable()
    {
        if (runtime != null)
            runtime.BlockPlaced += Play;
    }

    private void OnDisable()
    {
        if (runtime != null)
            runtime.BlockPlaced -= Play;
    }

    private void Play(BlockType type, Vector3 position)
    {
        if (Time.unscaledTime < nextSound)
            return;
        AudioClip[] clips = stone;
        switch (type)
        {
            case BlockType.OakPlanks:
            case BlockType.OakLog:
            case BlockType.BirchPlanks:
            case BlockType.BirchLog:
                clips = wood;
                break;
            case BlockType.Glass:
            case BlockType.Ice:
                clips = glass;
                break;
            case BlockType.Sand:
                clips = sand;
                break;
            case BlockType.Dirt:
            case BlockType.Land:
                clips = dirt;
                break;
            case BlockType.Snow:
                clips = snow;
                break;
            case BlockType.FlagGold:
            case BlockType.FlagRed:
                clips = cloth;
                break;
            case BlockType.Leaves:
            case BlockType.Grass:
            case BlockType.Ocean:
            case BlockType.ShallowWater:
            case BlockType.Thatch:
                clips = soft;
                break;
        }

        if (clips == null || clips.Length == 0)
            return;
        var clip = clips[Random.Range(0, clips.Length)];
        if (clip == null)
            return;
        var source = voices[voice];
        voice = (voice + 1) % voices.Length;
        source.pitch = Random.Range(0.784f, 0.816f);
        source.clip = clip;
        source.volume = volume * userVolume;
        source.Play();
        PlayedCount++;
        nextSound = Time.unscaledTime + minimumInterval;
    }
}
