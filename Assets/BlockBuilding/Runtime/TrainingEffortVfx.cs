using TMPro;
using UnityEngine;

[DefaultExecutionOrder(11000)]
public class TrainingEffortVfx : MonoBehaviour
{
    [Header("Training"), Tooltip("Tap intensity and existing exercise.")]
    public TrainingPresentation presentation;

    [Header("Weight trails"), Tooltip("Two preallocated short world-space ribbons.")]
    public TrailRenderer[] trails = new TrailRenderer[0];

    [Header("Burst"), Tooltip("Small local burst on rapid screen presses.")]
    public ParticleSystem particles;

    [Header("Tempo"), Tooltip("Small noninteractive tempo display.")]
    public TMP_Text tempo;

    private bool wasTraining;
    private float tapPulse, nextBurst;
    private int displayedTempo = -1;
    public bool Active
    {
        get
        {
            return presentation.training.IsTraining && presentation.Intensity > .35f;
        }
    }

    private void OnEnable()
    {
        if (presentation != null)
            presentation.Tapped += Tap;
    }

    private void OnDisable()
    {
        if (presentation != null)
            presentation.Tapped -= Tap;
        Clear();
    }

    private void Tap()
    {
        tapPulse = 1;
        if (!Active || particles == null || Time.unscaledTime < nextBurst)
            return;
        nextBurst = Time.unscaledTime + .08f;
        if (!particles.isPlaying)
            particles.Play();
        var training = presentation.training;
        var frame = training.Equipment != null ? training.Equipment.transform : training.transform;
        for (int side = -1; side <= 1; side += 2)
        {
            var emit = new ParticleSystem.EmitParams();
            emit.position = training.barbell.position + frame.right * side * 1.38f;
            emit.velocity = Vector3.up * .45f + frame.right * side * .35f;
            emit.startLifetime = .25f;
            emit.startSize = .08f;
            emit.startColor = new Color(1, .82f, .35f, .8f);
            particles.Emit(emit, 3);
        }
    }

    private void LateUpdate()
    {
        bool training = presentation.training.IsTraining;
        if (!training)
        {
            if (wasTraining)
                Clear();
            wasTraining = false;
            return;
        }

        wasTraining = true;
        tapPulse = Mathf.MoveTowards(tapPulse, 0, Time.unscaledDeltaTime * 5);
        var t = presentation.training;
        var frame = t.Equipment != null ? t.Equipment.transform : t.transform;
        for (int i = 0; i < trails.Length; i++)
        {
            var trail = trails[i];
            Vector3 point = t.barbell.position + frame.right * (i == 0 ? -1.38f : 1.38f);
            if (!trail.emitting && Active)
            {
                trail.transform.position = point;
                trail.Clear();
            }

            trail.transform.position = point;
            trail.emitting = Active;
            trail.widthMultiplier = Mathf.Lerp(.03f, .14f, presentation.Intensity);
            trail.time = .22f;
        }

        if (tempo != null)
        {
            bool show = presentation.Intensity > .08f;
            if (tempo.gameObject.activeSelf != show)
                tempo.gameObject.SetActive(show);
            if (show)
            {
                int value = Mathf.RoundToInt(t.RepetitionSpeed * 10);
                if (value != displayedTempo)
                {
                    displayedTempo = value;
                    tempo.text = "ТЕМП x" + (value / 10f).ToString("F1");
                }

                tempo.transform.localScale = Vector3.one * (1 + tapPulse * .08f);
            }
        }
    }

    private void Clear()
    {
        foreach (var trail in trails)
            if (trail != null)
            {
                trail.emitting = false;
                trail.Clear();
            }

        if (particles != null)
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (tempo != null)
            tempo.gameObject.SetActive(false);
        tapPulse = 0;
    }
}
