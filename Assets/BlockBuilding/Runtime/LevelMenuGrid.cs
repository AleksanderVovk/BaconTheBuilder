using UnityEngine;

public class LevelMenuGrid : MonoBehaviour
{
    [Header("Grid"), Tooltip("Responsive grid; three columns in landscape, two in portrait.")]
    public UnityEngine.UI.GridLayoutGroup layout;
    private void OnEnable()
    {
        Resize();
    }

    private void OnRectTransformDimensionsChange()
    {
        Resize();
    }

    private void Resize()
    {
        if (layout == null)
            return;
        var r = (RectTransform)transform;
        int columns = r.rect.width >= r.rect.height ? 3 : 2;
        int rows = (6 + columns - 1) / columns;
        layout.constraintCount = columns;
        layout.cellSize = new Vector2(
            Mathf.Max(1, (r.rect.width - layout.spacing.x * (columns - 1)) / columns),
            Mathf.Max(1, (r.rect.height - layout.spacing.y * (rows - 1)) / rows)
        );
    }
}
