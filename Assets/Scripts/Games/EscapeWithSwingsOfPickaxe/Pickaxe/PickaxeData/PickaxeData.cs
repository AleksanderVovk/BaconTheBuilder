using UnityEngine;

[CreateAssetMenu(fileName = "NewPickaxe", menuName = "Games/EscapeWithSwingsOfPickaxe/Pickaxe Data")]
public class PickaxeData : ScriptableObject
{
    [Header("Основное")]

    public Price price;

    [Tooltip("Уникальный ID кирки (строка).")]
    public string id;

    [Tooltip("Иконка кирки.")]
    public Sprite icon;

    [Tooltip("Отображаемое название.")]
    public string displayName;

    [Header("Характеристики")]

    [Min(0.01f)]
    public float damage = 1f;

    [Tooltip("Префаб кирки для спавна в мире (опционально).")]
    public GameObject pickaxePrefab;
}