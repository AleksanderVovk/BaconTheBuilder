using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class AtlasTextureSelector : MonoBehaviour
{
    [Header("Atlas")]
    [SerializeField] private Texture2D atlasTexture;

    [Header("Atlas Grid")]
    [Min(1)]
    [SerializeField] private int atlasColumns = 6;

    [Min(1)]
    [SerializeField] private int atlasRows = 2;

    [Header("Texture")]
    [Min(0)]
    [SerializeField] private int textureIndex = 0;

    [Header("Material")]
    [Range(0f, 2f)]
    [SerializeField] private float brightness = 1f;

    private Renderer cachedRenderer;
    private MaterialPropertyBlock propertyBlock;

    private static readonly int MainTexId =
        Shader.PropertyToID("_MainTex");

    private static readonly int AtlasColumnsId =
        Shader.PropertyToID("_AtlasColumns");

    private static readonly int AtlasRowsId =
        Shader.PropertyToID("_AtlasRows");

    private static readonly int TextureIndexId =
        Shader.PropertyToID("_TextureIndex");

    private static readonly int BrightnessId =
        Shader.PropertyToID("_Brightness");

    private static readonly int DamageColorId =
        Shader.PropertyToID("_DamageColor");

    private static readonly int DamageFlashId =
        Shader.PropertyToID("_DamageFlash");

    private void Awake()
    {
        Initialize();
        Apply();
    }

    private void OnEnable()
    {
        Initialize();
        Apply();
    }

    private void OnValidate()
    {
        atlasColumns = Mathf.Max(1, atlasColumns);
        atlasRows = Mathf.Max(1, atlasRows);

        textureIndex = Mathf.Clamp(
            textureIndex,
            0,
            GetTextureCount() - 1
        );

        brightness = Mathf.Clamp(
            brightness,
            0f,
            2f
        );

        Initialize();
        Apply();
    }

    private void Initialize()
    {
        if (cachedRenderer == null)
        {
            cachedRenderer = GetComponent<Renderer>();
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
    }

    public void Apply()
    {
        Initialize();

        cachedRenderer.GetPropertyBlock(propertyBlock);

        if (atlasTexture != null)
        {
            propertyBlock.SetTexture(
                MainTexId,
                atlasTexture
            );
        }

        propertyBlock.SetFloat(
            AtlasColumnsId,
            atlasColumns
        );

        propertyBlock.SetFloat(
            AtlasRowsId,
            atlasRows
        );

        propertyBlock.SetFloat(
            TextureIndexId,
            textureIndex
        );

        propertyBlock.SetFloat(
            BrightnessId,
            brightness
        );

        /*
         * Начальное состояние:
         * урон выключен.
         */
        propertyBlock.SetFloat(
            DamageFlashId,
            0f
        );

        propertyBlock.SetColor(
            DamageColorId,
            Color.white
        );

        cachedRenderer.SetPropertyBlock(propertyBlock);
    }

    public void SetTextureIndex(int index)
    {
        textureIndex = Mathf.Clamp(
            index,
            0,
            GetTextureCount() - 1
        );

        Apply();
    }

    public void SetTexture(int column, int row)
    {
        column = Mathf.Clamp(
            column,
            0,
            atlasColumns - 1
        );

        row = Mathf.Clamp(
            row,
            0,
            atlasRows - 1
        );

        textureIndex =
            row * atlasColumns + column;

        Apply();
    }

    public void SetBrightness(float value)
    {
        brightness = Mathf.Clamp(
            value,
            0f,
            2f
        );

        Apply();
    }

    public int GetTextureCount()
    {
        return Mathf.Max(
            1,
            atlasColumns * atlasRows
        );
    }

    public bool IsValidIndex(int index)
    {
        return index >= 0 &&
               index < atlasColumns * atlasRows;
    }
}