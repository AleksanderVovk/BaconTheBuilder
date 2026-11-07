using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class TilingProperty : MonoBehaviour
{
    public bool isDynamic;

    protected MeshRenderer meshRenderer;
    protected MaterialPropertyBlock mpb;

    private static readonly int ObjectScaleId = Shader.PropertyToID("_ObjectScale");
    private static readonly int IsDynamicId = Shader.PropertyToID("_IsDynamic");

    protected virtual void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        mpb = new MaterialPropertyBlock();
    }

    private void Start()
    {
        SetMaterialPropertyBlock();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (mpb == null)
            mpb = new MaterialPropertyBlock();

        SetMaterialPropertyBlock();
    }
#endif

    public void SetMaterialPropertyBlock()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (mpb == null)
            mpb = new MaterialPropertyBlock();

        meshRenderer.GetPropertyBlock(mpb);
        SetProperty(mpb);
        meshRenderer.SetPropertyBlock(mpb);
    }

    public virtual void SetProperty(MaterialPropertyBlock block)
    {
        block.SetVector(ObjectScaleId, transform.lossyScale);
        block.SetFloat(IsDynamicId, isDynamic ? 1f : 0f);
    }
}