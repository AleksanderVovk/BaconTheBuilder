using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(10000)]
public class TrainingPresentation : MonoBehaviour
{
    [Header("Training"), Tooltip("Existing exercise controller.")]
    public StrengthTrainingZone training;

    [Header("Camera"), Tooltip("Existing main camera, temporarily controlled during exercise.")]
    public Camera view;

    [Header("Camera driver"), Tooltip("Existing CinemachineBrain, restored after the return transition.")]
    public Behaviour cameraDriver;

    [Header("Framing"), Tooltip("Bench-relative overhead side view.")]
    public Vector3 cameraOffset = new Vector3(3.4f, 4.6f, 3.6f);

    [Header("Transition"), Tooltip("Seconds to ease into and out of training."), Min(0.1f)]
    public float transitionSeconds = 0.65f;

    [Header("Tap speed"), Tooltip("Maximum repetition speed when tapping rapidly."), Range(1, 4)]
    public float maximumSpeed = 2.5f;

    [Header("Tap gain"), Tooltip("Intensity gained per screen press."), Range(0.05f, 1)]
    public float tapGain = 0.22f;

    [Header("Decay"), Tooltip("Intensity lost per second without tapping."), Min(0.1f)]
    public float decay = 0.5f;

    [Header("Hint"), Tooltip("Nonblocking hint shown for three seconds at exercise start.")]
    public CanvasGroup hint;

    [Header("Speed streaks"), Tooltip("Preallocated UI streaks; no creation during gameplay.")]
    public RectTransform[] streaks = new RectTransform[0];
    public event System.Action Tapped;
    public float Intensity { get; private set; }

    public bool CameraControlled
    {
        get
        {
            return controlling;
        }
    }

    private bool controlling, wasDriver, entering;
    private float blend, originalFov, startFov, hintUntil;
    private Vector3 startPosition, returnPosition;
    private Quaternion startRotation, returnRotation;
    private void OnEnable()
    {
        if (training != null)
            training.Changed += Changed;
    }

    private void Changed()
    {
        if (training.IsTraining)
        {
            if (controlling && entering)
                return;
            if (!controlling)
            {
                returnPosition = view.transform.position;
                returnRotation = view.transform.rotation;
                originalFov = view.fieldOfView;
                wasDriver = cameraDriver != null && cameraDriver.enabled;
                if (wasDriver)
                    cameraDriver.enabled = false;
            }

            controlling = true;
            entering = true;
            blend = 0;
            Intensity = 0;
            startPosition = view.transform.position;
            startRotation = view.transform.rotation;
            startFov = view.fieldOfView;
            hintUntil = Time.unscaledTime + 3;
            if (hint != null)
                hint.gameObject.SetActive(true);
        }
        else if (controlling && entering)
        {
            entering = false;
            blend = 0;
            startPosition = view.transform.position;
            startRotation = view.transform.rotation;
            startFov = view.fieldOfView;
            Intensity = 0;
            training.RepetitionSpeed = 1;
            if (hint != null)
                hint.gameObject.SetActive(false);
        }
    }

    public void RegisterTap()
    {
        if (!training.IsTraining)
            return;
        Intensity = Mathf.Clamp01(Intensity + tapGain);
        training.RepetitionSpeed = Mathf.Lerp(1, maximumSpeed, Intensity);
        Tapped?.Invoke();
    }

    private void Update()
    {
        if (!training.IsTraining)
            return;
        Intensity = Mathf.MoveTowards(Intensity, 0, Time.unscaledDeltaTime * decay);
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began
                    && (EventSystem.current == null
                        || !EventSystem.current.IsPointerOverGameObject(touch.fingerId)))
                    RegisterTap();
            }
        }
        else if (Input.GetMouseButtonDown(0)
            && (EventSystem.current == null
                || !EventSystem.current.IsPointerOverGameObject()))
            RegisterTap();
        training.RepetitionSpeed = Mathf.Lerp(1, maximumSpeed, Intensity);
        if (hint != null)
        {
            float remaining = hintUntil - Time.unscaledTime;
            hint.alpha = Mathf.Clamp01(remaining / 0.3f);
            if (remaining <= 0)
                hint.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (controlling)
        {
            blend = Mathf.Clamp01(blend + Time.unscaledDeltaTime / Mathf.Max(.1f, transitionSeconds));
            float eased = Mathf.SmoothStep(0, 1, blend);
            Vector3 point = training.InteractionPosition;
            Transform frame = training.Equipment != null ? training.Equipment.transform : training.transform;
            Vector3 target = entering ? point + frame.TransformDirection(cameraOffset) : returnPosition;
            Quaternion rotation = entering ? Quaternion.LookRotation(point + Vector3.up * 0.85f - target) : returnRotation;
            view.transform.SetPositionAndRotation(
                Vector3.Lerp(startPosition, target, eased),
                Quaternion.Slerp(startRotation, rotation, eased)
            );
            view.fieldOfView = Mathf.Lerp(startFov, entering ? 48 - Intensity * 3.5f : originalFov, eased);
            if (!entering && blend >= 1)
            {
                controlling = false;
                if (cameraDriver != null)
                    cameraDriver.enabled = wasDriver;
            }
        }

        // Legacy screen streaks are retired; effort feedback follows the bar in world space.
        foreach (var line in streaks)
            if (line.gameObject.activeSelf)
                line.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (training != null)
        {
            training.Changed -= Changed;
            training.RepetitionSpeed = 1;
        }

        if (controlling && view != null)
        {
            view.transform.SetPositionAndRotation(returnPosition, returnRotation);
            view.fieldOfView = originalFov;
            if (cameraDriver != null)
                cameraDriver.enabled = wasDriver;
        }

        controlling = false;
        Intensity = 0;
        if (hint != null)
            hint.gameObject.SetActive(false);
        foreach (var line in streaks)
            if (line != null)
                line.gameObject.SetActive(false);
    }
}
