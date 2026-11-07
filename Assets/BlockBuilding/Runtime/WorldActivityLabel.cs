using TMPro;
using UnityEngine;

public class WorldActivityLabel : MonoBehaviour
{
    [Header("Session"), Tooltip("Existing menu controls label visibility.")]
    public BuildLevelSession session;

    [Header("Text"), Tooltip("World-space text; remains available after the tutorial.")]
    public TMP_Text label;

    [Header("Camera"), Tooltip("Assigned gameplay camera.")]
    public Camera view;

    [Header("Pulse"), Tooltip("Scale multiplier at the top of each pulse."), Range(1, 1.4f)]
    public float pulseScale = 1.15f;

    [Header("Period"), Tooltip("Seconds per pulse."), Min(.2f)]
    public float pulseSeconds = 1.5f;

    [Header("Near distance"), Tooltip("Shrink labels when viewed close up."), Min(1)]
    public float nearDistance = 9;

    [Header("Icons"), Tooltip("Optional world-space canvas attached to this label.")]
    public Canvas iconCanvas;

    private Vector3 originalScale;
    private Renderer textRenderer;
    private void Awake()
    {
        originalScale = transform.localScale;
        if (label != null)
            textRenderer = label.GetComponent<Renderer>();
    }

    private void LateUpdate()
    {
        if (label == null || view == null)
            return;
        bool visible = session == null || !session.MenuOpen;
        if (textRenderer != null)
            textRenderer.enabled = visible;
        if (iconCanvas != null)
            iconCanvas.enabled = visible;
        if (!visible)
            return;
        transform.rotation = view.transform.rotation;
        float wave = .5f - .5f * Mathf.Cos(Time.unscaledTime * 2 * Mathf.PI / Mathf.Max(.2f, pulseSeconds));
        float distance = Vector3.Distance(view.transform.position, transform.position);
        transform.localScale = originalScale * Mathf.Lerp(1, pulseScale, wave) * Mathf.Clamp(distance / nearDistance, .55f, 1);
    }
}
