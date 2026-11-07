using System;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class BuildLevelSession : MonoBehaviour
{
    [Header("Catalog"), Tooltip("Six data assets, loaded into the existing scene.")]
    public BuildLevelCatalog catalog;

    [Header("Construction"), Tooltip("Existing block runtime.")]
    public BlockLevelRuntime runtime;

    [Header("Training"), Tooltip("Stop training before changing levels or opening the menu.")]
    public StrengthTrainingZone training;

    [Header("Gameplay HUD"), Tooltip("Existing collection and strength HUD.")]
    public GameObject gameplayHUD;

    [Header("Pause"), Tooltip("Existing controller and interaction components to suspend in menus.")]
    public Behaviour[] gameplayBehaviours = new Behaviour[0];

    [Header("Spawn"), Tooltip("Player spawn in the current scene.")]
    public Vector3 spawn = new Vector3(0, 1, 0);

    [Header("Access stairs"), Tooltip("One combined stair mesh and collider, reused between levels.")]
    public LevelAccessStairway accessStairs;

    [Header("Controller integration"), Tooltip("Optional reset hook on the existing controller, invoked after teleport.")]
    public UnityEngine.Events.UnityEvent onPlayerReset = new UnityEngine.Events.UnityEvent();

    [Header("Menu input"), Tooltip("Optional hook to release the existing mouse capture.")]
    public UnityEngine.Events.UnityEvent onMenuOpened = new UnityEngine.Events.UnityEvent();

    private CursorLockMode savedCursorLock;
    private bool savedCursorVisible;
    private bool[] previousEnabled;
    private bool suspended;
    private double elapsed;
    private bool loadingProgress, lastCheckpointAssisted;
    private string activeSignature;
    private int lastCheckpointBuilt = -1;
    private double lastCheckpointSeconds;
    private float nextProgressFlush, nextCloudFlush;
    public event Action Changed;
    public int SelectedIndex { get; private set; } = -1;
    public bool MenuOpen { get; private set; } = true;
    public bool Completed { get; private set; }
    public bool IsExploringCompleted { get; private set; }
    public bool NewRecord { get; private set; }
    public bool IsAssistedRun { get; private set; }
    public bool SettingsOpen { get; private set; }
    public bool AdminOverlayOpen { get; set; }

    public void MarkAssisted()
    {
        IsAssistedRun = true;
        Checkpoint(false);
        Changed?.Invoke();
    }

    public float Elapsed
    {
        get
        {
            return (float)elapsed;
        }
    }

    public BuildLevelEntry Selected
    {
        get
        {
            return SelectedIndex >= 0 ? catalog.levels[SelectedIndex] : null;
        }
    }

    private void Start()
    {
        previousEnabled = new bool[gameplayBehaviours.Length];
        runtime.Changed += RuntimeChanged;
        YG.YG2.onGetSDKData += SyncProgressCloud;
        if (YG.YG2.isSDKEnabled)
            SyncProgressCloud();
        OpenMenu();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextProgressFlush)
        {
            nextProgressFlush = Time.unscaledTime + .75f;
            Checkpoint(false);
            bool cloud = Time.unscaledTime >= nextCloudFlush;
            if (cloud)
                nextCloudFlush = Time.unscaledTime + 10;
            BuildProgressStore.Flush(cloud);
        }

        if (SettingsOpen)
            return;
        if (Selected != null && !MenuOpen && !Completed)
            elapsed += Time.unscaledDeltaTime;
        if (!AdminOverlayOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            if (MenuOpen)
                Resume();
            else
                OpenMenu();
        }
    }

    public void SetSettingsOpen(bool value)
    {
        if (SettingsOpen == value)
            return;
        if (value)
            Checkpoint(true);
        SettingsOpen = value;
        if (value)
        {
            if (training != null)
                training.StopTraining();
            runtime.SetPaused(true);
            Suspend(true);
            onMenuOpened.Invoke();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            runtime.SetPaused(MenuOpen || Completed);
            Suspend(MenuOpen || (Completed && !IsExploringCompleted));
        }

        Changed?.Invoke();
    }

    public void Select(int index)
    {
        if (index < 0 || index >= catalog.levels.Length)
            throw new ArgumentOutOfRangeException("index");
        var entry = catalog.levels[index];
        entry.data.level.Validate();
        if (entry.BlockCount == 0)
            throw new InvalidOperationException("Cannot play an empty level.");
        Checkpoint(true);
        if (training != null)
            training.StopTraining();
        Suspend(true);
        runtime.SetPaused(true);
        runtime.carrier.Clear();
        SelectedIndex = index;
        Completed = false;
        IsExploringCompleted = false;
        NewRecord = false;
        elapsed = 0;
        IsAssistedRun = false;
        if (runtime.carrier.strength != null)
            runtime.carrier.strength.RestoreStrengthAfterTesting();
        ResetPlayerToSpawn();
        loadingProgress = true;
        try
        {
            activeSignature = BuildProgressStore.Signature(entry.data);
            runtime.LoadLevel(entry.data);
            BuildProgressStore.Snapshot saved;
            if (BuildProgressStore.TryGet(entry.id, activeSignature, runtime.RawTotalCount, out saved))
            {
                elapsed = saved.elapsedSeconds;
                IsAssistedRun = saved.assisted;
                runtime.RestoreBuiltCount(saved.builtBlocks);
            }

            lastCheckpointBuilt = runtime.StoredBuiltCount;
            lastCheckpointSeconds = elapsed;
            lastCheckpointAssisted = IsAssistedRun;
        }
        finally
        {
            loadingProgress = false;
        }

        if (accessStairs != null)
            accessStairs.Configure(entry, runtime.data.blockMaterial, runtime.worldLayer);
        MenuOpen = false;
        runtime.SetPaused(false);
        Suspend(false);
        if (gameplayHUD != null)
            gameplayHUD.SetActive(true);
        Changed?.Invoke();
    }

    public void Restart()
    {
        if (SelectedIndex >= 0)
            Select(SelectedIndex);
    }

    public void OpenMenu()
    {
        Checkpoint(true);
        if (training != null)
            training.StopTraining();
        MenuOpen = true;
        onMenuOpened.Invoke();
        runtime.SetPaused(true);
        Suspend(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (gameplayHUD != null)
            gameplayHUD.SetActive(false);
        Changed?.Invoke();
    }

    public void Resume()
    {
        if (Selected == null || Completed && !IsExploringCompleted)
            return;
        MenuOpen = false;
        runtime.SetPaused(false);
        Suspend(false);
        if (gameplayHUD != null)
            gameplayHUD.SetActive(true);
        Changed?.Invoke();
    }

    public void ContinueExploring()
    {
        if (Selected == null || !Completed)
            return;
        IsExploringCompleted = true;
        MenuOpen = false;
        runtime.carrier.Clear();
        runtime.SetPaused(true);
        Suspend(false);
        if (gameplayHUD != null)
            gameplayHUD.SetActive(true);
        Changed?.Invoke();
    }

    private void RuntimeChanged()
    {
        if (loadingProgress)
            return;
        if (Selected != null && !Completed && runtime.IsComplete)
        {
            Completed = true;
            BuildProgressStore.Complete(Selected.id, activeSignature);
            VictoryEconomy.AwardWin();
            NewRecord = LevelRecordStore.Record(Selected.id, Elapsed);
            BuildProgressStore.Flush(true);
            runtime.SetPaused(true);
            Suspend(true);
            if (gameplayHUD != null)
                gameplayHUD.SetActive(false);
        }

        Checkpoint(false);
        Changed?.Invoke();
    }

    private void Suspend(bool value)
    {
        if (value == suspended)
            return;
        if (previousEnabled == null)
            previousEnabled = new bool[gameplayBehaviours.Length];
        for (int i = 0; i < gameplayBehaviours.Length; i++)
            if (gameplayBehaviours[i] != null)
            {
                if (value)
                {
                    previousEnabled[i] = gameplayBehaviours[i].enabled;
                    gameplayBehaviours[i].enabled = false;
                }
                else
                    gameplayBehaviours[i].enabled = previousEnabled[i];
            }

        if (value)
        {
            savedCursorLock = Cursor.lockState;
            savedCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = savedCursorLock;
            Cursor.visible = savedCursorVisible;
        }

        suspended = value;
    }

    private void ResetPlayerToSpawn()
    {
        var player = runtime.carrier.player;
        var controller = player.GetComponent<CharacterController>();
        bool wasEnabled = controller != null && controller.enabled;
        if (wasEnabled)
            controller.enabled = false;
        player.SetPositionAndRotation(spawn, Quaternion.identity);
        if (wasEnabled)
            controller.enabled = true;
        onPlayerReset.Invoke();
    }

    private void Checkpoint(bool flush)
    {
        if (GameSaveReset.IsResetting)
            return;
        if (!loadingProgress && Selected != null && runtime != null && !Completed && !string.IsNullOrEmpty(activeSignature))
        {
            int built = runtime.StoredBuiltCount;
            if (built > 0
                && built < runtime.RawTotalCount
                && (built != lastCheckpointBuilt
                    || elapsed - lastCheckpointSeconds > .5
                    || IsAssistedRun != lastCheckpointAssisted))
            {
                BuildProgressStore.Save(Selected.id, activeSignature, built, elapsed, IsAssistedRun);
                lastCheckpointBuilt = built;
                lastCheckpointSeconds = elapsed;
                lastCheckpointAssisted = IsAssistedRun;
            }
        }

        if (flush)
            BuildProgressStore.Flush(true);
    }

    private void SyncProgressCloud()
    {
        BuildProgressStore.SyncCloud(catalog);
        if (Selected == null || runtime == null || Completed || loadingProgress || string.IsNullOrEmpty(activeSignature))
            return;
        BuildProgressStore.Snapshot saved;
        if (!BuildProgressStore.TryGet(Selected.id, activeSignature, runtime.RawTotalCount, out saved)
            || saved.builtBlocks <= runtime.StoredBuiltCount)
            return;
        // A late SDK response can add a newer checkpoint without firing reward events.
        if (training != null)
            training.StopTraining();
        loadingProgress = true;
        try
        {
            runtime.SetPaused(true);
            runtime.carrier.Clear();
            runtime.LoadLevel(Selected.data);
            runtime.RestoreBuiltCount(saved.builtBlocks);
            ResetPlayerToSpawn();
            elapsed = Math.Max(elapsed, saved.elapsedSeconds);
            IsAssistedRun |= saved.assisted;
            lastCheckpointBuilt = runtime.StoredBuiltCount;
            lastCheckpointSeconds = elapsed;
            lastCheckpointAssisted = IsAssistedRun;
            runtime.SetPaused(MenuOpen || SettingsOpen);
        }
        finally
        {
            loadingProgress = false;
        }

        Changed?.Invoke();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            Checkpoint(true);
    }

    private void OnApplicationQuit()
    {
        Checkpoint(true);
    }

    private void OnDisable()
    {
        Checkpoint(true);
    }

    private void OnDestroy()
    {
        if (runtime != null)
            runtime.Changed -= RuntimeChanged;
        YG.YG2.onGetSDKData -= SyncProgressCloud;
    }
}
