using System.Collections.Generic;
using UnityEngine;

public class PickaxeDatabase : MonoBehaviour
{
    [Header("Настройки загрузки")]

    [Tooltip("Путь к папке в Resources, где лежат PickaxeData. " +
             "Оставь пустым, если все лежат прямо в Resources.")]
    [SerializeField]
    private string resourcesFolder = "Pickaxes";

    private static PickaxeDatabase _instance;
    public static PickaxeDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PickaxeDatabase>();

                if (_instance == null)
                {
                    Debug.LogError(
                        "PickaxeDatabase: Не найден объект с компонентом PickaxeDatabase на сцене."
                    );
                }
            }

            return _instance;
        }
    }

    private Dictionary<string, PickaxeData> _pickaxesById =
        new Dictionary<string, PickaxeData>();

    private List<PickaxeData> _allPickaxes =
        new List<PickaxeData>();

    public IReadOnlyList<PickaxeData> AllPickaxes => _allPickaxes;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning(
                "PickaxeDatabase: Найден второй экземпляр. Уничтожаю дубликат.",
                this
            );

            Destroy(gameObject);
            return;
        }

        _instance = this;

        LoadAllPickaxes();
    }

    private void LoadAllPickaxes()
    {
        _pickaxesById.Clear();
        _allPickaxes.Clear();

        PickaxeData[] all;

        if (string.IsNullOrEmpty(resourcesFolder))
        {
            all = Resources.LoadAll<PickaxeData>("");
        }
        else
        {
            all = Resources.LoadAll<PickaxeData>(resourcesFolder);
        }

        foreach (var data in all)
        {
            if (data == null)
                continue;

            if (string.IsNullOrEmpty(data.id))
            {
                Debug.LogWarning(
                    $"PickaxeDatabase: У ассета '{data.name}' не задан ID. Пропускаем.",
                    data
                );

                continue;
            }

            if (_pickaxesById.ContainsKey(data.id))
            {
                Debug.LogWarning(
                    $"PickaxeDatabase: Дубликат ID '{data.id}'. " +
                    $"Пропускаем ассет '{data.name}'.",
                    data
                );

                continue;
            }

            _pickaxesById[data.id] = data;
            _allPickaxes.Add(data);
        }

        Debug.Log(
            $"PickaxeDatabase: Загружено кирок: {_allPickaxes.Count}.",
            this
        );
    }

    /// <summary>
    /// Получить кирку по ID. Возвращает null, если не найдена.
    /// </summary>
    public PickaxeData GetPickaxeById(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        _pickaxesById.TryGetValue(id, out var data);
        return data;
    }

    /// <summary>
    /// Проверить, существует ли кирка с таким ID.
    /// </summary>
    public bool HasPickaxe(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;

        return _pickaxesById.ContainsKey(id);
    }

    /// <summary>
    /// Получить все кирки.
    /// </summary>
    public List<PickaxeData> GetAllPickaxes()
    {
        return new List<PickaxeData>(_allPickaxes);
    }
}