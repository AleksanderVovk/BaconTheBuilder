using TMPro;
using UnityEngine;

public class RainbowWorldText : MonoBehaviour
{
    [Header("Label"), Tooltip("Rainbow world-space text.")]
    public TMP_Text label;

    [Header("Subtitle"), Tooltip("Smaller white caption, sharing label visibility.")]
    public TMP_Text subtitle;

    [Header("Session"), Tooltip("Hide world labels while the level menu is open.")]
    public BuildLevelSession session;

    [Header("Speed"), Tooltip("Rainbow cycles per second."), Min(.05f)]
    public float speed = .22f;

    private Renderer subtitleRenderer;
    private void Start()
    {
        label.ForceMeshUpdate();
        if (subtitle != null)
            subtitleRenderer = subtitle.GetComponent<Renderer>();
    }

    private void LateUpdate()
    {
        bool visible = session == null || !session.MenuOpen;
        if (subtitleRenderer != null)
            subtitleRenderer.enabled = visible;
        if (!visible)
            return;
        var info = label.textInfo;
        for (int i = 0; i < info.characterCount; i++)
        {
            var ch = info.characterInfo[i];
            if (!ch.isVisible)
                continue;
            var colors = info.meshInfo[ch.materialReferenceIndex].colors32;
            int at = ch.vertexIndex;
            for (int corner = 0; corner < 4; corner++)
                colors[at + corner] = Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * speed + i * .055f + (corner > 1
                    ? .04f
                    : 0), 1), .85f, 1);
        }

        label.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}
