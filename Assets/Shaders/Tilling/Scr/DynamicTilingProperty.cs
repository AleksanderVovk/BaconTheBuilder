using UnityEngine;

public class DynamicTilingProperty : TilingProperty
{
    public Color color = Color.white;

    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public void SetColor(Color newColor)
    {
        color = newColor;
        SetMaterialPropertyBlock();
    }

    public override void SetProperty(MaterialPropertyBlock block)
    {
        block.SetColor(ColorId, color);
        base.SetProperty(block);
    }
}