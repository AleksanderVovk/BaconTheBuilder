using UnityEngine;
using TMPro;

[DefaultExecutionOrder(200)]
public class GameSettingsPanel : MonoBehaviour
{
    [Header("Settings"), Tooltip("Persistent audio and camera settings.")]
    public GameSettingsStore settings;

    [Header("Session"), Tooltip("Pause gameplay while the window is open.")]
    public BuildLevelSession session;

    [Header("Window"), Tooltip("Modal backdrop and settings box.")]
    public GameObject overlay;

    [Header("Box"), Tooltip("Responsive window rectangle.")]
    public RectTransform box;

    [Header("Open"), Tooltip("Top-right settings button.")]
    public UnityEngine.UI.Button openButton;

    [Header("Close"), Tooltip("Discard unsaved changes.")]
    public UnityEngine.UI.Button closeButton;

    [Header("Save"), Tooltip("Apply and persist settings.")]
    public UnityEngine.UI.Button saveButton;

    [Header("Sensitivity"), Tooltip("Existing camera sensitivity, 20 to 600.")]
    public UnityEngine.UI.Slider sensitivity;

    [Header("Sounds"), Tooltip("Sound effect volume, independent of music.")]
    public UnityEngine.UI.Slider sounds;

    [Header("Music"), Tooltip("Music volume, independent of sound effects.")]
    public UnityEngine.UI.Slider music;

    [Header("Values"), Tooltip("Sensitivity, sound percent, music percent.")]
    public TMP_Text[] values = new TMP_Text[0];

    [Header("Feedback"), Tooltip("Existing stock UI audio source.")]
    public AudioSource uiVoice;

    [Header("Saved sound"), Tooltip("Existing success clip.")]
    public AudioClip successClip;
    private void Awake()
    {
        openButton.onClick.AddListener(Open);
        closeButton.onClick.AddListener(Cancel);
        saveButton.onClick.AddListener(Save);
        sensitivity.onValueChanged.AddListener(Preview);
        sounds.onValueChanged.AddListener(Preview);
        music.onValueChanged.AddListener(Preview);
        settings.Reloaded += Populate;
        overlay.SetActive(false);
    }

    private void Update()
    {
        if (!session.SettingsOpen)
            return;
        Adapt();
        if (Input.GetKeyDown(KeyCode.Escape))
            Cancel();
    }

    public void Open()
    {
        if (session.SettingsOpen)
            return;
        Populate();
        session.SetSettingsOpen(true);
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        Adapt();
    }

    private void Populate()
    {
        var s = settings.Saved;
        sensitivity.SetValueWithoutNotify(s.sensitivity);
        sounds.SetValueWithoutNotify(s.soundVolume);
        music.SetValueWithoutNotify(s.musicVolume);
        RefreshLabels();
    }

    private void Preview(float unused)
    {
        settings.Preview(sensitivity.value, sounds.value, music.value);
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        if (values.Length > 0 && values[0] != null)
            values[0].text = Mathf.RoundToInt(sensitivity.value).ToString();
        if (values.Length > 1 && values[1] != null)
            values[1].text = Mathf.RoundToInt(sounds.value * 100) + "%";
        if (values.Length > 2 && values[2] != null)
            values[2].text = Mathf.RoundToInt(music.value * 100) + "%";
    }

    public void Save()
    {
        settings.Save(sensitivity.value, sounds.value, music.value);
        Close();
        if (uiVoice != null && successClip != null)
            uiVoice.PlayOneShot(successClip, .35f);
    }

    public void Cancel()
    {
        settings.RestoreSaved();
        Close();
    }

    private void Close()
    {
        overlay.SetActive(false);
        session.SetSettingsOpen(false);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (box != null)
            Adapt();
    }

    private void Adapt()
    {
        if (box == null)
            return;
        var parent = box.parent as RectTransform;
        if (parent == null)
            return;
        float scale = Mathf.Min(1f, Mathf.Min((parent.rect.width - 40) / 760f, (parent.rect.height - 40) / 680f));
        box.localScale = Vector3.one * Mathf.Max(.1f, scale);
    }

    private void OnDestroy()
    {
        if (settings != null)
            settings.Reloaded -= Populate;
        if (openButton != null)
            openButton.onClick.RemoveListener(Open);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Cancel);
        if (saveButton != null)
            saveButton.onClick.RemoveListener(Save);
        if (sensitivity != null)
            sensitivity.onValueChanged.RemoveListener(Preview);
        if (sounds != null)
            sounds.onValueChanged.RemoveListener(Preview);
        if (music != null)
            music.onValueChanged.RemoveListener(Preview);
    }
}
