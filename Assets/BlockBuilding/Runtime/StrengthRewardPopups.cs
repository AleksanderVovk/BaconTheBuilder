using System;
using TMPro;
using UnityEngine;

public class StrengthRewardPopups : MonoBehaviour
{
    [Serializable]
    public class Item
    {
        [Header("Root"), Tooltip("Prebuilt pooled popup.")]
        public RectTransform root;

        [Header("Opacity"), Tooltip("Noninteractive popup group.")]
        public CanvasGroup group;

        [Header("Number"), Tooltip("Actual strength earned.")]
        public TMP_Text text;

        [Header("Icon"), Tooltip("Existing strong sprite.")]
        public RectTransform icon;

        [NonSerialized]
        public float started;

        [NonSerialized]
        public Vector2 origin;

        [NonSerialized]
        public float drift;

        [NonSerialized]
        public bool active;
    }

    [Header("Training"), Tooltip("Existing exercise reward event.")]
    public StrengthTrainingZone training;

    [Header("Area"), Tooltip("Full-screen rect inside the existing HUD.")]
    public RectTransform area;

    [Header("Canvas"), Tooltip("Existing parent canvas.")]
    public Canvas canvas;

    [Header("Pool"), Tooltip("Reused UI elements; no creation per repetition.")]
    public Item[] items = new Item[0];

    [Header("Spawn square"), Tooltip("Centered spawn region in screen pixels, clamped to the safe area."), Min(100)]
    public float squarePixels = 900;

    [Header("Lifetime"), Tooltip("Seconds including fade in and fade out."), Min(.3f)]
    public float lifetime = 1.25f;

    [Header("Rise"), Tooltip("Upward movement in screen pixels."), Min(0)]
    public float risePixels = 100;

    private int next;
    private void OnEnable()
    {
        if (training != null)
            training.StrengthEarned += Show;
    }

    private void OnDisable()
    {
        if (training != null)
            training.StrengthEarned -= Show;
        foreach (var item in items)
            if (item.root != null)
            {
                item.active = false;
                item.root.gameObject.SetActive(false);
            }
    }

    public void Show(System.Numerics.BigInteger amount)
    {
        if (amount <= 0 || items.Length == 0 || !isActiveAndEnabled)
            return;
        var item = items[next];
        next = (next + 1) % items.Length;
        float scale = Mathf.Max(.01f, canvas.scaleFactor);
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Rect safe = Screen.safeArea;
        Vector2 minScreen = Vector2.Max(
            safe.min,
            new Vector2(Screen.width, Screen.height) * .5f - Vector2.one * squarePixels * .5f
        );
        Vector2 maxScreen = Vector2.Min(
            safe.max,
            new Vector2(Screen.width, Screen.height) * .5f + Vector2.one * squarePixels * .5f
        );
        Vector2 min, max;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(area, minScreen, cam, out min);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(area, maxScreen, cam, out max);
        Vector2 half = item.root.sizeDelta * .5f;
        min += half + new Vector2(30 / scale, 0);
        max -= half + new Vector2(30 / scale, risePixels / scale);
        if (min.x > max.x)
            min.x = max.x = (min.x + max.x) * .5f;
        if (min.y > max.y)
            min.y = max.y = (min.y + max.y) * .5f;
        item.origin = new Vector2(UnityEngine.Random.Range(min.x, max.x), UnityEngine.Random.Range(min.y, max.y));
        item.drift = UnityEngine.Random.Range(-25f, 25f) / scale;
        item.text.textWrappingMode = TextWrappingModes.NoWrap;
        item.text.overflowMode = TextOverflowModes.Overflow;
        item.text.enableAutoSizing = true;
        item.text.fontSizeMin = 28;
        item.text.fontSizeMax = 54;
        item.text.text = "+" + StrengthNumber.Format(amount);
        float width = Mathf.Clamp(item.text.GetPreferredValues(item.text.text, float.PositiveInfinity, 76).x, 50, 230);
        float total = width + 10 + 58;
        item.text.rectTransform.anchoredPosition = new Vector2(-total * .5f, 0);
        item.text.rectTransform.sizeDelta = new Vector2(width + 2, 76);
        item.icon.anchoredPosition = new Vector2(total * .5f - 29, 0);
        item.started = Time.unscaledTime;
        item.active = true;
        item.group.alpha = 0;
        item.root.anchoredPosition = item.origin;
        item.root.localScale = Vector3.one * .85f;
        item.root.gameObject.SetActive(true);
    }

    private void Update()
    {
        float scale = Mathf.Max(.01f, canvas.scaleFactor);
        foreach (var item in items)
        {
            if (!item.active)
                continue;
            float t = (Time.unscaledTime - item.started) / Mathf.Max(.3f, lifetime);
            if (t >= 1)
            {
                item.active = false;
                item.root.gameObject.SetActive(false);
                continue;
            }

            item.root.anchoredPosition = item.origin + new Vector2(item.drift * t, risePixels / scale * (1 - Mathf.Pow(1 - t, 2)));
            item.group.alpha = Mathf.Clamp01(t / .08f) * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.35f, 1, t)));
            float pop = t < .16f ? Mathf.Lerp(.85f, 1.1f, t / .16f) : Mathf.Lerp(1.1f, 1, Mathf.Clamp01((t - .16f) / .2f));
            item.root.localScale = Vector3.one * pop;
        }
    }
}
