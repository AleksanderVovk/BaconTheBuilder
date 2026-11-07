using System.Collections;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(200)]
public class BuildAdminMenu : MonoBehaviour
{
    [Header("Session"), Tooltip("Existing level session.")]
    public BuildLevelSession session;

    [Header("Release access"), Tooltip("Normally available only in Editor and Development Build.")]
    public bool enableInRelease;

    [Header("Panel"), Tooltip("Admin overlay in the existing Canvas.")]
    public GameObject panel;

    [Header("Information"), Tooltip("Inventory and test-run status.")]
    public TMP_Text status;

    [Header("Buttons"), Tooltip("Open, close, grant, clear, strength, restore, pile, target, speed, complete site, restart, levels.")]
    public UnityEngine.UI.Button[] buttons = new UnityEngine.UI.Button[0];

    private bool resumeOnClose;
    private Coroutine building;
    public bool IsOpen
    {
        get
        {
            return panel.activeSelf;
        }
    }

    private void Awake()
    {
        if (!Application.isEditor && !Debug.isDebugBuild && !enableInRelease)
        {
            gameObject.SetActive(false);
            return;
        }

        buttons[0].onClick.AddListener(Toggle);
        buttons[1].onClick.AddListener(Close);
        buttons[2].onClick.AddListener(Grant999);
        buttons[3].onClick.AddListener(ClearHands);
        buttons[4].onClick.AddListener(AddStrength);
        buttons[5].onClick.AddListener(RestoreStrength);
        buttons[6].onClick.AddListener(TeleportToPile);
        buttons[7].onClick.AddListener(TeleportToTarget);
        buttons[8].onClick.AddListener(ToggleSpeed);
        buttons[9].onClick.AddListener(CompleteSite);
        buttons[10].onClick.AddListener(Restart);
        buttons[11].onClick.AddListener(Levels);
        session.Changed += Refresh;
        session.runtime.carrier.Changed += Refresh;
        panel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8))
            Toggle();
        else if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public void Toggle()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        resumeOnClose = !session.MenuOpen && (!session.Completed || session.IsExploringCompleted);
        session.AdminOverlayOpen = true;
        session.OpenMenu();
        panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        panel.SetActive(false);
        session.AdminOverlayOpen = false;
        if (resumeOnClose)
            session.Resume();
    }

    private bool Ready()
    {
        return session.Selected != null && !session.Completed;
    }

    public void Grant999()
    {
        if (!Ready())
            return;
        session.MarkAssisted();
        session.runtime.carrier.GrantForTesting(999);
        Refresh();
    }

    public void ClearHands()
    {
        session.runtime.carrier.Clear();
        Refresh();
    }

    public void AddStrength()
    {
        if (!Ready())
            return;
        session.MarkAssisted();
        var strength = session.runtime.carrier.strength;
        strength.SetStrengthForTesting(strength.Strength + 100);
        Refresh();
    }

    public void RestoreStrength()
    {
        session.runtime.carrier.strength.RestoreStrengthAfterTesting();
        Refresh();
    }

    private void Teleport(Vector3 point)
    {
        if (!Ready())
            return;
        session.MarkAssisted();
        var player = session.runtime.carrier.player;
        var controller = player.GetComponent<CharacterController>();
        bool active = controller != null && controller.enabled;
        if (active)
            controller.enabled = false;
        player.position = point;
        if (active)
            controller.enabled = true;
        session.onPlayerReset.Invoke();
        Refresh();
    }

    public void TeleportToPile()
    {
        if (Ready())
            Teleport(session.Selected.data.level.pickupPoint + Vector3.up * 1.1f);
    }

    public void TeleportToTarget()
    {
        if (!Ready() || session.runtime.CurrentSite == null)
            return;
        Vector3 point = session.runtime.CurrentSite.NextWorldPosition;
        point.y = session.Selected.data.level.pickupPoint.y + 1.1f;
        Teleport(point);
    }

    public void ToggleSpeed()
    {
        if (!Ready())
            return;
        session.MarkAssisted();
        session.runtime.DebugBuildSpeed = session.runtime.DebugBuildSpeed > 1 ? 1 : 10;
        Refresh();
    }

    public void CompleteSite()
    {
        if (!Ready() || building != null)
            return;
        session.MarkAssisted();
        var site = session.runtime.CurrentSite;
        resumeOnClose = true;
        Close();
        building = StartCoroutine(FinishSite(site, session.SelectedIndex));
    }

    private IEnumerator FinishSite(BuildSite site, int index)
    {
        yield return null;
        var carrier = session.runtime.carrier;
        while (site != null && !site.IsComplete && session.SelectedIndex == index)
        {
            if (session.MenuOpen)
            {
                yield return null;
                continue;
            }

            for (int i = 0; i < 64 && !site.IsComplete; i++)
            {
                if (carrier.Count == 0)
                    carrier.GrantForTesting(999);
                if (!site.TryBuild(carrier))
                    break;
            }

            yield return null;
        }

        building = null;
    }

    public void Restart()
    {
        StopBuilding();
        resumeOnClose = false;
        Close();
        session.Restart();
    }

    public void Levels()
    {
        StopBuilding();
        resumeOnClose = false;
        Close();
        session.OpenMenu();
    }

    private void StopBuilding()
    {
        if (building != null)
        {
            StopCoroutine(building);
            building = null;
        }
    }

    private void Refresh()
    {
        if (status == null || !panel.activeSelf)
            return;
        var carrier = session.runtime.carrier;
        status.text = (session.Selected != null
            ? session.Selected.title
            : "Сначала выбери уровень") + "\nБлоки: " + carrier.Count + " / сила: " + StrengthNumber.Format(carrier.strength.Strength) + " / скорость: x" + session.runtime.DebugBuildSpeed + "\n" + (session.IsAssistedRun
            ? "ТЕСТОВЫЙ ЗАБЕГ: рекорд сохраняется"
            : "Рекорды сохраняются и с тестовыми функциями");
        bool ready = Ready();
        for (int i = 2; i <= 9; i++)
            buttons[i].interactable = ready;
        buttons[10].interactable = session.Selected != null;
    }

    private void OnDestroy()
    {
        if (session != null)
        {
            session.Changed -= Refresh;
            if (session.runtime != null && session.runtime.carrier != null)
                session.runtime.carrier.Changed -= Refresh;
        }
    }
}
