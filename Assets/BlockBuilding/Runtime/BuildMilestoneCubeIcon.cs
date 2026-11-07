using UnityEngine;

// Small crisp UI cube, drawn directly without an extra texture or material.
[ExecuteAlways]
[RequireComponent(typeof(CanvasRenderer))]
public class BuildMilestoneCubeIcon : UnityEngine.UI.MaskableGraphic
{
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        Vector2 c = r.center;
        float w = r.width * .45f, h = r.height * .45f;
        Vector2 top = c + new Vector2(0, h),
            right = c + new Vector2(w, h * .45f),
            bottomRight = c + new Vector2(w, -h * .5f),
            bottom = c + new Vector2(0, -h),
            bottomLeft = c + new Vector2(-w, -h * .5f),
            left = c + new Vector2(-w, h * .45f),
            mid = c + new Vector2(0, 0);
        Quad(vh, top, right, mid, left, new Color32(181, 255, 93, 255));
        Quad(vh, left, mid, bottom, bottomLeft, new Color32(72, 195, 34, 255));
        Quad(vh, mid, right, bottomRight, bottom, new Color32(30, 138, 37, 255));
    }

    private static void Quad(UnityEngine.UI.VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 color)
    {
        int n = vh.currentVertCount;
        vh.AddVert(a, color, Vector2.zero);
        vh.AddVert(b, color, Vector2.zero);
        vh.AddVert(c, color, Vector2.zero);
        vh.AddVert(d, color, Vector2.zero);
        vh.AddTriangle(n, n + 1, n + 2);
        vh.AddTriangle(n, n + 2, n + 3);
    }
}
