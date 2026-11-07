using UnityEngine;

public class StockGameAudio : MonoBehaviour
{
    [Header("Session"), Tooltip("Existing gameplay session.")]
    public BuildLevelSession session;

    [Header("Effects"), Tooltip("One shared nonspatial effects voice.")]
    public AudioSource effects;

    [Header("UI"), Tooltip("Separate source for button clicks.")]
    public AudioSource uiVoice;

    [Header("Buttons"), Tooltip("Existing buttons, wired once at Start.")]
    public UnityEngine.UI.Button[] buttons = new UnityEngine.UI.Button[0];

    [Header("Stock clips"), Tooltip("Click, pickup, training start, training stop, repetition, completion, teleport.")]
    public AudioClip[] clips = new AudioClip[0];

    [Header("Error"), Tooltip("Creator Store error feedback.")]
    public AudioClip errorClip;

    [Header("Purchase"), Tooltip("Creator Store successful purchase feedback.")]
    public AudioClip purchaseClip;

    private GymEquipmentHub gym;
    private float nextResult;
    private int lastCount, lastReturn;
    private System.Numerics.BigInteger lastStrength;
    private bool finished, lastTraining;
    private BuildPlayerSafety safety;
    private float nextUi, nextEffect;
    private PlayerBlockCarrier Carrier
    {
        get
        {
            return session.runtime.carrier;
        }
    }

    private void Start()
    {
        foreach (var b in buttons)
            if (b != null)
                b.onClick.AddListener(Click);
        gym = session.training.GetComponent<GymEquipmentHub>();
        if (gym != null)
        {
            gym.PurchaseSucceeded += Purchase;
            gym.InteractionRejected += Error;
        }

        Carrier.Changed += Inventory;
        session.training.Changed += Training;
        Carrier.strength.Changed += Strength;
        session.Changed += Session;
        lastCount = Carrier.Count;
        lastStrength = Carrier.strength.Strength;
        safety = session.runtime.GetComponent<BuildPlayerSafety>();
        lastTraining = session.training.IsTraining;
    }

    public void Error()
    {
        Result(errorClip);
    }

    public void Purchase()
    {
        Result(purchaseClip);
    }

    private void Result(AudioClip clip)
    {
        if (clip == null || Time.unscaledTime < nextResult)
            return;
        nextResult = Time.unscaledTime + .12f;
        uiVoice.PlayOneShot(clip, .6f);
    }

    private void Click()
    {
        if (Time.unscaledTime < nextUi || clips.Length == 0 || clips[0] == null)
            return;
        nextUi = Time.unscaledTime + .06f;
        uiVoice.PlayOneShot(clips[0], .28f);
    }

    private void Play(int index, float volume = .35f)
    {
        if (index >= clips.Length || clips[index] == null || Time.unscaledTime < nextEffect)
            return;
        nextEffect = Time.unscaledTime + .055f;
        effects.PlayOneShot(clips[index], volume);
    }

    private void Inventory()
    {
        if (Carrier.Count > lastCount)
            Play(1);
        lastCount = Carrier.Count;
    }

    private void Training()
    {
        bool active = session.training.IsTraining;
        if (active == lastTraining)
            return;
        lastTraining = active;
        Play(active ? 2 : 3, .22f);
    }

    private void Strength()
    {
        if (session.training.IsTraining && Carrier.strength.Strength > lastStrength)
            Play(4, .15f);
        lastStrength = Carrier.strength.Strength;
    }

    private void Session()
    {
        if (session.Completed && !finished && clips.Length > 5 && clips[5] != null)
            effects.PlayOneShot(clips[5], .55f);
        finished = session.Completed;
    }

    private void Update()
    {
        if (safety != null && safety.ReturnCount != lastReturn)
        {
            lastReturn = safety.ReturnCount;
            Play(6);
        }
    }

    private void OnDestroy()
    {
        foreach (var b in buttons)
            if (b != null)
                b.onClick.RemoveListener(Click);
        if (gym != null)
        {
            gym.PurchaseSucceeded -= Purchase;
            gym.InteractionRejected -= Error;
        }

        if (session == null)
            return;
        Carrier.Changed -= Inventory;
        session.training.Changed -= Training;
        Carrier.strength.Changed -= Strength;
        session.Changed -= Session;
    }
}
