using UnityEngine;

public class CompletionPanelLayout : MonoBehaviour
{
    [Header("Container"), Tooltip("Existing result box.")]
    public RectTransform box;

    [Header("Preview"), Tooltip("Live 3D model region.")]
    public RectTransform preview;

    [Header("Text"), Tooltip("Title, result and record text.")]
    public RectTransform text;

    [Header("Continue"), Tooltip("Explore completed map.")]
    public RectTransform continueButton;

    [Header("Menu"), Tooltip("Return to level cards.")]
    public RectTransform menuButton;
    private void OnEnable()
    {
        Layout();
    }

    private void OnRectTransformDimensionsChange()
    {
        Layout();
    }

    public void Layout()
    {
        if (box == null || preview == null || text == null || continueButton == null || menuButton == null)
            return;
        var parent = (RectTransform)transform;
        box.anchorMin = box.anchorMax = box.pivot = new Vector2(.5f, .5f);
        box.anchoredPosition = Vector2.zero;
        box.sizeDelta = new Vector2(
            Mathf.Max(500, Mathf.Min(1600, parent.rect.width - 96)),
            Mathf.Max(650, Mathf.Min(950, parent.rect.height - 80))
        );
        bool wide = box.sizeDelta.x > 1100;
        Set(preview, wide
            ? new Vector2(0, 0.08f)
            : new Vector2(0, 0.36f), wide
            ? new Vector2(0.61f, 0.95f)
            : new Vector2(1, 0.98f));
        Set(continueButton, wide
            ? new Vector2(0.61f, 0.23f)
            : new Vector2(0, 0.02f), wide
            ? new Vector2(1, 0.34f)
            : new Vector2(0.49f, 0.12f));
        Set(menuButton, wide
            ? new Vector2(0.61f, 0.08f)
            : new Vector2(0.51f, 0.02f), wide
            ? new Vector2(1, 0.19f)
            : new Vector2(1, 0.12f));
        // Share the buttons' horizontal bounds so the result stays centered above them.
        float left = Mathf.Min(continueButton.anchorMin.x, menuButton.anchorMin.x);
        float right = Mathf.Max(continueButton.anchorMax.x, menuButton.anchorMax.x);
        text.pivot = new Vector2(.5f, .5f);
        Set(text, new Vector2(left, wide ? .36f : .15f), new Vector2(right, wide ? .76f : .35f));
        var caption = text.GetComponent<TMPro.TMP_Text>();
        if (caption != null)
        {
            caption.alignment = TMPro.TextAlignmentOptions.Center;
            caption.margin = new Vector4(12, 8, 12, 8);
        }
    }

    private static void Set(RectTransform r, Vector2 min, Vector2 max)
    {
        r.pivot = new Vector2(.5f, .5f);
        r.anchorMin = min;
        r.anchorMax = max;
        r.offsetMin = new Vector2(18, 12);
        r.offsetMax = new Vector2(-18, -12);
    }
}
