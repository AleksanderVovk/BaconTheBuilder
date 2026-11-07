using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InfiniteBlocksZone : MonoBehaviour
{
    [Header("Session"), Tooltip("Existing game state.")]
    public BuildLevelSession session;

    [Header("Carrier"), Tooltip("Existing block carrier; ordinary inventory is preserved.")]
    public PlayerBlockCarrier carrier;

    [Header("Duration"), Tooltip("Real seconds of infinite blocks after entering."), Min(1)]
    public float duration = 30;

    [Header("Radius"), Tooltip("Horizontal activation radius."), Min(.5f)]
    public float radius = 2.4f;

    [Header("Exit margin"), Tooltip("Leave this extra distance before activating again."), Min(.1f)]
    public float exitMargin = .6f;

    [Header("Badge"), Tooltip("Buff icon container in the existing HUD.")]
    public GameObject badge;

    [Header("Countdown fill"), Tooltip("Radial image that drains from full to empty.")]
    public Image countdownFill;

    [Header("Time text"), Tooltip("Seconds remaining under the icon.")]
    public TMP_Text timeText;

    [Header("Audio"), Tooltip("Existing stock feedback.")]
    public StockGameAudio feedback;

    private float expiresAt;
    private bool active, inside;
    private int lastSecond = -1;
    public bool Active
    {
        get
        {
            return active;
        }
    }

    public float Remaining
    {
        get
        {
            return active ? Mathf.Max(0, expiresAt - Time.realtimeSinceStartup) : 0;
        }
    }

    private void Start()
    {
        session.runtime.Changed += Preview;
        badge.SetActive(false);
    }

    private void Preview()
    {
        carrier.RefreshInfinitePreview();
    }

    public bool Activate()
    {
        if (active || session.MenuOpen || session.AdminOverlayOpen || carrier.IsTraining)
            return false;
        expiresAt = Time.realtimeSinceStartup + Mathf.Max(1, duration);
        active = true;
        lastSecond = -1;
        carrier.SetInfiniteBlocks(true);
        if (feedback != null && feedback.clips.Length > 5 && feedback.clips[5] != null)
            feedback.effects.PlayOneShot(feedback.clips[5], .35f);
        return true;
    }

    public bool CheckEntry()
    {
        Vector3 d = carrier.player.position - transform.position;
        float distance = d.x * d.x + d.z * d.z;
        if (distance > (radius + exitMargin) * (radius + exitMargin) || Mathf.Abs(d.y) > 4)
        {
            inside = false;
            return false;
        }

        if (distance > radius * radius || inside || session.MenuOpen || session.AdminOverlayOpen)
            return false;
        inside = true;
        return Activate();
    }

    private void Update()
    {
        if (active && Remaining <= 0)
            EndBonus();
        CheckEntry();
        bool show = active && !session.MenuOpen && !session.AdminOverlayOpen;
        if (badge.activeSelf != show)
            badge.SetActive(show);
        if (!active)
            return;
        countdownFill.fillAmount = Mathf.Clamp01(Remaining / Mathf.Max(1, duration));
        int second = Mathf.CeilToInt(Remaining);
        if (second != lastSecond)
        {
            lastSecond = second;
            timeText.text = second + "с";
        }
    }

    public void EndBonus()
    {
        if (!active)
            return;
        active = false;
        carrier.SetInfiniteBlocks(false);
        if (badge != null)
            badge.SetActive(false);
    }

    private void OnDisable()
    {
        EndBonus();
    }

    private void OnDestroy()
    {
        if (session != null && session.runtime != null)
            session.runtime.Changed -= Preview;
    }
}
