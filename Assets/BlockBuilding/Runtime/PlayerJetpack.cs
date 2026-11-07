using UnityEngine;

[DefaultExecutionOrder(200)]
public class PlayerJetpack : MonoBehaviour
{
    [Header("Controller"), Tooltip("Existing player controller; it remains responsible for all movement.")]
    public PlayerController controller;

    [Header("Capsule"), Tooltip("Existing collision controller.")]
    public CharacterController character;

    [Header("State"), Tooltip("Existing player state.")]
    public PlayerState state;

    [Header("Session"), Tooltip("Suspend flight in menus, settings and completion screens.")]
    public BuildLevelSession session;

    [Header("Carrier"), Tooltip("Pause flight while using a training bench.")]
    public PlayerBlockCarrier carrier;

    [Header("Input"), Tooltip("Held jump input from the existing mobile button or external code.")]
    public bool isJump;

    [Header("Climb speed"), Tooltip("Upward flight speed in world units per second."), Min(1)]
    public float climbSpeed = 12f;

    [Header("Acceleration"), Tooltip("Vertical acceleration while holding jump."), Min(1)]
    public float acceleration = 65f;

    [Header("Takeoff speed"), Tooltip("Immediate upward speed when thrust starts, including while falling."), Min(0)]
    public float takeoffSpeed = 7f;

    [Header("Release braking"), Tooltip("Remove upward momentum smoothly after releasing jump."), Min(0)]
    public float releaseBraking = 35f;

    [Header("Model"), Tooltip("Back-mounted Blender model and exhaust root.")]
    public GameObject visual;

    [Header("Exhaust"), Tooltip("Reusable left and right thruster effects.")]
    public ParticleSystem[] exhaust = new ParticleSystem[0];

    [Header("Engine"), Tooltip("Downloaded looping engine sound.")]
    public AudioSource engine;

    [Header("Engine volume"), Tooltip("Authored volume before the player's sound setting."), Range(0, 1)]
    public float engineVolume = .2f;

    [Header("Audio settings"), Tooltip("Existing independent sound volume setting.")]
    public GameSettingsStore settings;

    private float throttle;
    private float flightVelocity;
    private bool emitting;
    public bool IsFlying { get; private set; }

    public bool CanFly
    {
        get
        {
            return isActiveAndEnabled
                && controller != null
                && controller.enabled
                && character != null
                && character.enabled
                && state != null
                && state.canMove
                && !state.isDeath
                && !state.isPushed
                && !state.onLadder
                && (carrier == null
                    || !carrier.IsTraining)
                && (session == null
                    || !session.MenuOpen
                    && !session.SettingsOpen
                    && !session.AdminOverlayOpen
                    && (!session.Completed
                        || session.IsExploringCompleted));
        }
    }

    public void SetJumpHeld(bool held)
    {
        isJump = held;
    }

    // Called by the existing controller after gravity, before CharacterController.Move.
    public void ApplyFlight(float deltaTime)
    {
        bool wantsFlight = isJump || Input.GetKey(KeyCode.Space);
        bool wasFlying = IsFlying;
        IsFlying = CanFly && wantsFlight;
        if (IsFlying)
        {
            if (!wasFlying)
                flightVelocity = Mathf.Max(controller.velocity.y, Mathf.Min(takeoffSpeed, climbSpeed));
            flightVelocity = Mathf.MoveTowards(
                flightVelocity,
                Mathf.Max(1, climbSpeed),
                Mathf.Max(1, acceleration) * deltaTime
            );
            controller.velocity.y = flightVelocity;
        }
        else if (CanFly && controller.velocity.y > 0)
            controller.velocity.y = Mathf.MoveTowards(controller.velocity.y, 0, Mathf.Max(0, releaseBraking) * deltaTime);
    }

    private void Update()
    {
        if (!CanFly)
        {
            IsFlying = false;
            isJump = false;
        }

        bool show = carrier == null || !carrier.IsTraining;
        if (visual != null && visual.activeSelf != show)
            visual.SetActive(show);
        if (emitting != IsFlying)
        {
            emitting = IsFlying;
            foreach (var effect in exhaust)
                if (effect != null)
                {
                    if (emitting)
                        effect.Play(true);
                    else
                        effect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
        }

        throttle = Mathf.MoveTowards(throttle, IsFlying ? 1 : 0, Time.unscaledDeltaTime * 16);
        if (engine == null)
            return;
        engine.volume = engineVolume * (settings != null ? settings.SoundVolume : 1) * throttle;
        engine.pitch = Mathf.Lerp(.88f, 1.08f, throttle);
        if (throttle > .001f && !engine.isPlaying)
            engine.Play();
        else if (throttle <= .001f && engine.isPlaying)
            engine.Stop();
    }

    public void ResetFlight()
    {
        isJump = false;
        IsFlying = false;
        throttle = 0;
        flightVelocity = 0;
        emitting = false;
        foreach (var effect in exhaust)
            if (effect != null)
                effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (engine != null)
            engine.Stop();
    }

    private void OnDisable()
    {
        ResetFlight();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused)
            ResetFlight();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            ResetFlight();
    }
}
