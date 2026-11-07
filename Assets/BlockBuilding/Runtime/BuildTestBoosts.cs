using System.Collections;
using UnityEngine;

public class BuildTestBoosts : MonoBehaviour
{
    [Header("Session"), Tooltip("Existing construction session. Boost runs are marked assisted.")]
    public BuildLevelSession session;

    [Header("Buttons"), Tooltip("Four future donation buttons; currently free test grants.")]
    public UnityEngine.UI.Button[] buttons = new UnityEngine.UI.Button[0];

    [Header("Amounts"), Tooltip("Installed cells per click, capped by remaining level cells.")]
    public int[] amounts =
    {
        100,
        500,
        2084,
        10418
    };

    private bool busy;
    public bool IsBusy
    {
        get
        {
            return busy;
        }
    }

    private void Start()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[i].onClick.AddListener(() => Grant(index));
        }

        session.Changed += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        bool ready = !busy
            && session.Selected != null
            && !session.MenuOpen
            && !session.Completed
            && !session.AdminOverlayOpen
            && !session.runtime.carrier.IsTraining;
        foreach (var b in buttons)
            b.interactable = ready;
    }

    private void Update()
    {
        Refresh();
    }

    public void Grant(int index)
    {
        if (index < 0
            || index >= amounts.Length
            || busy
            || session.MenuOpen
            || session.Completed
            || session.Selected == null
            || session.runtime.carrier.IsTraining)
            return;
        StartCoroutine(Build(Mathf.Clamp(amounts[index], 0, 10418)));
    }

    private IEnumerator Build(int count)
    {
        var runtime = session.runtime;
        var initial = runtime.CurrentSite;
        int selected = session.SelectedIndex;
        if (initial == null)
            yield break;
        busy = true;
        session.MarkAssisted();
        Refresh();
        try
        {
            while (count > 0
                && initial != null
                && !runtime.IsComplete
                && !session.MenuOpen
                && selected == session.SelectedIndex)
            {
                for (int i = 0; i < 48 && count > 0 && !runtime.IsComplete; i++)
                {
                    if (!runtime.CurrentSite.TryBuildGranted())
                        yield break;
                    count--;
                }

                yield return null;
            }
        }
        finally
        {
            // Keep carried quantity, but realign types to the new next cells.
            if (initial != null && selected == session.SelectedIndex)
                runtime.carrier.GrantForTesting(Mathf.Min(runtime.carrier.Count, runtime.TotalCount - runtime.BuiltCount));
            busy = false;
            Refresh();
        }
    }

    private void OnDestroy()
    {
        if (session != null)
            session.Changed -= Refresh;
    }
}
