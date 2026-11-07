using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BuildMilestoneFeedback : MonoBehaviour
{
    [Serializable]
    public class Popup
    {
        [Header("Root"), Tooltip("Prebuilt pooled notification.")]
        public RectTransform root;

        [Header("Opacity"), Tooltip("Never intercepts touches.")]
        public CanvasGroup group;

        [Header("Text"), Tooltip("Milestone text.")]
        public TMP_Text text;

        [Header("Slot"), Tooltip("Layout slot; the card inside it animates independently.")]
        public RectTransform slot;

        [NonSerialized]
        public float started;

        [NonSerialized]
        public Vector2 origin;

        [NonSerialized]
        public bool active;
    }

    [Header("Session"), Tooltip("Existing level session.")]
    public BuildLevelSession session;

    [Header("Canvas"), Tooltip("Existing HUD canvas.")]
    public Canvas canvas;

    [Header("Area"), Tooltip("Centered full-screen popup layer.")]
    public RectTransform area;

    [Header("Bar"), Tooltip("Existing construction slider.")]
    public RectTransform bar;

    [Header("Milestone labels"), Tooltip("Compact milestone row inside the bar.")]
    public TMP_Text marks;

    [Header("Popups"), Tooltip("Reusable notifications; no Instantiate per milestone.")]
    public Popup[] popups = new Popup[0];

    [Header("Impact"), Tooltip("Downloaded impact recording.")]
    public AudioClip impact;

    [Header("Celebration"), Tooltip("Downloaded success sound.")]
    public AudioClip celebration;

    [Header("Audio"), Tooltip("Dedicated reusable sound voice.")]
    public AudioSource voice;

    [Header("Settings"), Tooltip("Existing sound volume settings.")]
    public GameSettingsStore settings;

    [Header("Volume"), Tooltip("Milestone sound volume before user setting."), Range(0, 1)]
    public float volume = .6f;

    [Header("Vertical stack"), Tooltip("Right-hand container with a VerticalLayoutGroup.")]
    public RectTransform stack;

    [Header("Visible limit"), Tooltip("Further messages wait in the queue."), Range(1, 4)]
    public int maxVisible = 3;

    [Header("Message interval"), Tooltip("Minimum delay between incoming cards."), Min(.2f)]
    public float messageInterval = .75f;

    [Header("Lifetime"), Tooltip("Seconds including entry and exit animations."), Min(.8f)]
    public float lifetime = 2.8f;

    private readonly Queue<int> pending = new Queue<int>(1024);
    private int selected = -2, lastBuilt;
    private float pulse, nextSound;
    private void OnEnable()
    {
        session.runtime.BlockPlaced += Placed;
        session.Changed += Sync;
        Sync();
    }

    private void OnDisable()
    {
        session.runtime.BlockPlaced -= Placed;
        session.Changed -= Sync;
        pending.Clear();
        if (bar != null)
            bar.localScale = Vector3.one;
        foreach (var p in popups)
        {
            p.active = false;
            p.root.gameObject.SetActive(false);
            if (p.slot != null)
                p.slot.gameObject.SetActive(false);
        }

        if (voice != null)
            voice.Stop();
    }

    public static int MilestoneAt(int built)
    {
        return built < 50 ? 0 : built < 100 ? 50 : built / 100 * 100;
    }

    private void Sync()
    {
        // Restore/load updates the baseline silently; rewards are only from placement events.
        int built = session.runtime.BuiltCount;
        if (selected != session.SelectedIndex || built != lastBuilt)
        {
            pending.Clear();
            selected = session.SelectedIndex;
            lastBuilt = built;
        }

        UpdateMarks();
    }

    private void UpdateMarks()
    {
        if (marks == null)
            return;
        int built = session.runtime.BuiltCount;
        int first = built < 200 ? 50 : Mathf.Max(200, built / 100 * 100);
        int second = first == 50 ? 100 : first + 100, third = first == 50 ? 200 : first + 200;
        marks.text = Label(first, built) + "    |    " + Label(second, built) + "    |    " + Label(third, built);
    }

    private string Label(int n, int built)
    {
        if (n > session.runtime.TotalCount)
            return "";
        return "<color=" + (built >= n ? "#ABFF36" : "#C9D9C9") + ">" + (built >= n ? "+ " : "") + n + "</color>";
    }

    private void Placed(BlockType type, Vector3 position)
    {
        int built = session.runtime.BuiltCount, milestone = MilestoneAt(built);
        if (milestone > MilestoneAt(lastBuilt))
        {
            pending.Enqueue(milestone);
            pulse = 1;
        }

        lastBuilt = built;
        UpdateMarks();
    }

    private void Show(int count)
    {
        if (popups.Length == 0)
            return;
        Popup p = null;
        foreach (var item in popups)
            if (!item.active)
            {
                p = item;
                break;
            }

        if (p == null)
            return;
        p.slot.SetAsLastSibling();
        p.slot.gameObject.SetActive(true);
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(stack);
        p.origin = area.InverseTransformPoint(p.slot.position);
        p.text.text = "<size=15><color=#ACFF64>ЭТАП ЗАВЕРШЁН</color></size>\n<size=30>"
            + count.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)
            + " БЛОКОВ</size>";
        p.started = Time.unscaledTime;
        p.active = true;
        p.group.alpha = 0;
        p.root.gameObject.SetActive(true);
        p.root.position = area.TransformPoint(p.origin + Vector2.right * (stack.rect.width + 40));
        voice.volume = volume * (settings != null ? settings.SoundVolume : 1);
        voice.Stop();
        if (impact != null)
            voice.PlayOneShot(impact, .75f);
        if (celebration != null)
            voice.PlayOneShot(celebration, count >= 200 ? .75f : .5f);
    }

    private void Update()
    {
        if (session.MenuOpen || session.SettingsOpen)
        {
            pending.Clear();
            return;
        }

        PositionStack();
        int visible = 0;
        foreach (var p in popups)
            if (p.active)
                visible++;
        if (pending.Count > 0 && visible < maxVisible && Time.unscaledTime >= nextSound)
        {
            int n = pending.Dequeue();
            nextSound = Time.unscaledTime + messageInterval;
            Show(n);
        }

        pulse = Mathf.MoveTowards(pulse, 0, Time.unscaledDeltaTime * 2.6f);
        if (bar != null)
            bar.localScale = Vector3.one * (1 + Mathf.Sin(pulse * Mathf.PI) * .035f);
        foreach (var p in popups)
        {
            if (!p.active)
                continue;
            float t = (Time.unscaledTime - p.started) / lifetime;
            if (t >= 1)
            {
                p.active = false;
                p.root.gameObject.SetActive(false);
                p.slot.gameObject.SetActive(false);
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(stack);
                continue;
            }

            float enter = 1 - Mathf.Pow(1 - Mathf.Clamp01((Time.unscaledTime - p.started) / .28f), 3);
            float leave = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(lifetime - .3f, lifetime, Time.unscaledTime - p.started));
            Vector2 target = area.InverseTransformPoint(p.slot.position);
            p.origin = Vector2.Lerp(p.origin, target, 1 - Mathf.Exp(-14 * Time.unscaledDeltaTime));
            p.root.position = area.TransformPoint(p.origin + Vector2.right * ((1 - enter) + leave) * (stack.rect.width + 40));
            p.root.localScale = Vector3.one;
            p.group.alpha = enter * (1 - leave);
        }
    }

    private void PositionStack()
    {
        if (stack == null)
            return;
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Rect safe = Screen.safeArea;
        Vector2 topRight;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(area, safe.max, camera, out topRight);
        stack.anchoredPosition = topRight - area.rect.max + new Vector2(-24, -220);
    }
}
