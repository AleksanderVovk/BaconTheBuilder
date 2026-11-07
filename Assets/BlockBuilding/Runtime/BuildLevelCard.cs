using TMPro;
using UnityEngine;

public class BuildLevelCard : MonoBehaviour
{
    [Header("Title"), Tooltip("Level title.")]
    public TMP_Text title;

    [Header("Details"), Tooltip("Block count and best completed time.")]
    public TMP_Text details;

    [Header("Icon"), Tooltip("Actual colored render; black tint until completion.")]
    public UnityEngine.UI.Image icon;

    [Header("Status"), Tooltip("Completion state.")]
    public TMP_Text status;

    [Header("Button"), Tooltip("Select this level.")]
    public UnityEngine.UI.Button button;

    [Header("Selection"), Tooltip("Gold frame around the currently selected level.")]
    public UnityEngine.UI.Image selectionFrame;
    public void Refresh(BuildLevelEntry entry, int number)
    {
        bool done = LevelRecordStore.Completed(entry.id);
        float best;
        title.text = number.ToString("00") + "  " + entry.title;
        details.text = entry.BlockCount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)
            + " БЛОКОВ\nРекорд: " + (LevelRecordStore.TryGetBest(entry.id, out best)
            ? LevelRecordStore.Format(best)
            : "ещё нет");
        icon.sprite = entry.icon;
        icon.color = done ? Color.white : Color.black;
        BuildProgressStore.Snapshot saved;
        bool ongoing = BuildProgressStore.TryGet(
            entry.id,
            BuildProgressStore.Signature(entry.data),
            BuildFoundation.RawCount(entry.data),
            out saved
        );
        status.text = ongoing
            ? "ПРОДОЛЖИТЬ: " + BuildFoundation.WorkInPrefix(entry.data, saved.builtBlocks) + " / " + entry.BlockCount
            : done
            ? "ПОСТРОЕНО  /  ИГРАТЬ ЕЩЁ"
            : "ПОСТРОИТЬ";
        status.color = done ? new Color(0.4f, 0.95f, 0.65f) : new Color(1, 0.78f, 0.36f);
    }
}
