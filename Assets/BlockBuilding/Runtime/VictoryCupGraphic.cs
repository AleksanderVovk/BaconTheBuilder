using UnityEngine;

// Vector trophy: crisp at any canvas size, with no external artwork dependency.
[RequireComponent(typeof(CanvasRenderer))]
public class VictoryCupGraphic : UnityEngine.UI.MaskableGraphic
{
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
    {
        vh.Clear();
        Color gold = new Color(1, .72f, .02f);
        Box(vh, .06f, .57f, .24f, .82f, Color.black);
        Box(vh, .76f, .57f, .94f, .82f, Color.black);
        Box(vh, .10f, .61f, .24f, .78f, gold);
        Box(vh, .76f, .61f, .90f, .78f, gold);
        Box(vh, .43f, .17f, .57f, .51f, Color.black);
        Box(vh, .47f, .18f, .53f, .51f, gold);
        Box(vh, .22f, .06f, .78f, .21f, Color.black);
        Box(vh, .27f, .10f, .73f, .17f, gold);
        Shape(
            vh,
            new[] { new Vector2(.16f, .88f), new Vector2(.84f, .88f), new Vector2(.74f, .48f), new Vector2(.5f, .35f), new Vector2(.26f, .48f) },
            Color.black
        );
        Shape(
            vh,
            new[] { new Vector2(.22f, .83f), new Vector2(.78f, .83f), new Vector2(.69f, .52f), new Vector2(.5f, .41f), new Vector2(.31f, .52f) },
            gold
        );
        Box(vh, .27f, .72f, .72f, .80f, new Color(1, .96f, .52f));
    }

    private void Box(UnityEngine.UI.VertexHelper vh, float x, float y, float x2, float y2, Color c)
    {
        Shape(vh, new[] { new Vector2(x, y), new Vector2(x2, y), new Vector2(x2, y2), new Vector2(x, y2) }, c);
    }

    private void Shape(UnityEngine.UI.VertexHelper vh, Vector2[] points, Color tint)
    {
        int first = vh.currentVertCount;
        var r = rectTransform.rect;
        // Polygon fan triangles, with a shared center inside the cup silhouette.
        Vector2 center = Vector2.zero;
        foreach (var p in points)
            center += p;
        center /= points.Length;
        vh.AddVert(new Vector3(r.xMin + center.x * r.width, r.yMin + center.y * r.height), tint, Vector2.zero);
        foreach (var p in points)
            vh.AddVert(new Vector3(r.xMin + p.x * r.width, r.yMin + p.y * r.height), tint, Vector2.zero);
        for (int i = 0; i < points.Length; i++)
            vh.AddTriangle(first, first + 1 + i, first + 1 + (i + 1) % points.Length);
    }
}
