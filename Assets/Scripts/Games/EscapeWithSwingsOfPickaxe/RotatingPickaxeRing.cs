using System.Collections.Generic;
using UnityEngine;

public class RotatingPickaxeRing : MonoBehaviour
{
    [Header("Target / центр игрока")]

    [Tooltip("Игрок или объект, за которым всегда следует центр кирок.")]
    [SerializeField] private Transform target;

    [Tooltip("Если Target не назначен, используется Transform объекта с этим компонентом.")]
    [SerializeField] private bool useThisTransformAsTarget = true;

    [Tooltip("Смещение центра кирок относительно игрока. Y = высота центра.")]
    [SerializeField] private Vector3 targetLocalOffset = new Vector3(0f, 1.2f, 0f);

    [Tooltip("Пустышка центра орбиты. Если оставить null, скрипт создаст её автоматически.")]
    [SerializeField] private Transform orbitCenter;

    [Header("Префаб кирки")]

    [SerializeField] private GameObject pickaxePrefab;

    [Tooltip("Максимальное количество кирок.")]
    [Min(1)]
    [SerializeField] private int maxPickaxeCount = 8;

    [Tooltip("Радиус круга вокруг центра.")]
    [Min(0.01f)]
    [SerializeField] private float orbitRadius = 2f;

    [Tooltip("Высота кирки относительно центра.")]
    [SerializeField] private float pickaxeHeight = 0f;

    [Tooltip("Общий начальный сдвиг кольца в градусах.")]
    [SerializeField] private float angleOffset = 0f;

    [Header("Вращение вокруг центра")]

    [Tooltip("Включает движение всех pivot-пустышек по окружности.")]
    [SerializeField] private bool rotateOrbit = true;

    [Tooltip("Скорость вращения всей системы вокруг центра: градусов в секунду.")]
    [SerializeField] private float orbitRotationSpeed = 90f;

    [Tooltip("1 = одна сторона, -1 = противоположная.")]
    [SerializeField] private float orbitRotationDirection = 1f;

    [Header("Вид модели кирки")]

    [Tooltip("Поправка ориентации префаба. Поставь Y = 90, если кирка смотрит боком.")]
    [SerializeField] private Vector3 pickaxeVisualRotationOffset = new Vector3(0f, 90f, 0f);

    [Tooltip("Локальное смещение модели от её pivot. Обычно X = радиус, Y = высота.")]
    [SerializeField] private Vector3 pickaxeVisualLocalOffset = new Vector3(2f, 0f, 0f);

    [Header("Добавление кирок")]

    [SerializeField] private KeyCode spawnKey = KeyCode.F;
    [SerializeField] private bool canSpawnWithKey = true;

    [Tooltip("Добавить одну кирку при старте.")]
    [SerializeField] private bool spawnOneOnStart = false;

    [Tooltip("Добавить все кирки до лимита при старте.")]
    [SerializeField] private bool spawnFullRingOnStart = false;

    [Header("Дебаг")]

    [SerializeField] private bool showDebugLogs = true;

    private class PickaxeData
    {
        public Transform pivot;
        public Transform visual;
    }

    private readonly List<PickaxeData> pickaxes = new List<PickaxeData>();

    private float orbitAngle;

    public int CurrentPickaxeCount => pickaxes.Count;
    public int MaxPickaxeCount => maxPickaxeCount;
    public bool IsFull => CurrentPickaxeCount >= maxPickaxeCount;

    private void Awake()
    {
        ResolveTarget();
        CreateOrbitCenterIfNeeded();
        UpdateOrbitCenterImmediately();
    }

    private void Start()
    {
        if (spawnFullRingOnStart)
        {
            SpawnPickaxesUntilMax();
        }
        else if (spawnOneOnStart)
        {
            SpawnOnePickaxe();
        }
    }

    private void Update()
    {
        HandleSpawnInput();

        UpdateOrbitCenter();
        RotateOrbitCenter();
    }

    private void ResolveTarget()
    {
        if (target == null && useThisTransformAsTarget)
        {
            target = transform;
        }

        if (target == null)
        {
            Debug.LogWarning(
                $"{nameof(RotatingPickaxeRing)}: Target не назначен. " +
                $"Перетащи Transform игрока в Inspector.",
                this
            );
        }
    }

    private void CreateOrbitCenterIfNeeded()
    {
        if (orbitCenter != null)
            return;

        GameObject centerObject = new GameObject("PickaxeOrbitCenter");

        orbitCenter = centerObject.transform;
        orbitCenter.position = transform.position;
        orbitCenter.rotation = Quaternion.identity;
    }

    /// <summary>
    /// Центр всегда находится на игроке.
    /// Его позиция обновляется, но сам центр не вращается.
    /// </summary>
    private void UpdateOrbitCenter()
    {
        if (orbitCenter == null || target == null)
            return;

        orbitCenter.position =
            target.position +
            target.TransformDirection(targetLocalOffset);

        orbitCenter.rotation = Quaternion.identity;
    }

    private void UpdateOrbitCenterImmediately()
    {
        if (orbitCenter == null || target == null)
            return;

        orbitCenter.position =
            target.position +
            target.TransformDirection(targetLocalOffset);

        orbitCenter.rotation = Quaternion.identity;
    }

    /// <summary>
    /// Вращает только pivot-пустышки вокруг orbitCenter.
    /// Объекты-кирки сами напрямую не вращаются.
    /// </summary>
    private void RotateOrbitCenter()
    {
        if (!rotateOrbit || pickaxes.Count == 0)
            return;

        orbitAngle += orbitRotationSpeed * orbitRotationDirection * Time.deltaTime;
        orbitAngle = Mathf.Repeat(orbitAngle, 360f);

        UpdatePivotsRotation();
    }

    private void HandleSpawnInput()
    {
        if (canSpawnWithKey && Input.GetKeyDown(spawnKey))
        {
            SpawnOnePickaxe();
        }
    }

    /// <summary>
    /// Добавляет одну кирку. Каждая кирка имеет свою pivot-пустышку.
    /// </summary>
    public void SpawnOnePickaxe()
    {
        CleanupDestroyedPickaxes();

        if (pickaxePrefab == null)
        {
            Debug.LogError(
                $"{nameof(RotatingPickaxeRing)}: Не назначен Pickaxe Prefab.",
                this
            );
            return;
        }

        if (IsFull)
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    $"{nameof(RotatingPickaxeRing)}: Лимит кирок: " +
                    $"{CurrentPickaxeCount}/{maxPickaxeCount}.",
                    this
                );
            }

            return;
        }

        ResolveTarget();
        CreateOrbitCenterIfNeeded();
        UpdateOrbitCenterImmediately();

        GameObject pivotObject = new GameObject(
            $"PickaxePivot_{CurrentPickaxeCount + 1}"
        );

        Transform pivot = pivotObject.transform;

        // Pivot находится в центре круга.
        pivot.SetParent(orbitCenter, false);
        pivot.localPosition = Vector3.zero;
        pivot.localRotation = Quaternion.identity;

        GameObject pickaxeObject = Instantiate(
            pickaxePrefab,
            pivot
        );

        pickaxeObject.name = $"Pickaxe_{CurrentPickaxeCount + 1}";

        Transform visual = pickaxeObject.transform;

        // Сама модель имеет фиксированный оффсет и фиксированный поворот.
        // После создания скрипт больше НЕ меняет visual.localRotation.
        visual.localPosition = new Vector3(
            orbitRadius,
            pickaxeHeight,
            0f
        );

        visual.localRotation = Quaternion.Euler(
            pickaxeVisualRotationOffset
        );

        pickaxes.Add(new PickaxeData
        {
            pivot = pivot,
            visual = visual
        });

        RebuildRing();

        if (showDebugLogs)
        {
            Debug.Log(
                $"{nameof(RotatingPickaxeRing)}: Добавлена кирка. " +
                $"Сейчас: {CurrentPickaxeCount}/{maxPickaxeCount}.",
                this
            );
        }
    }

    /// <summary>
    /// Добавляет кирки, пока не будет достигнут лимит.
    /// </summary>
    public void SpawnPickaxesUntilMax()
    {
        int amount = maxPickaxeCount - CurrentPickaxeCount;

        for (int i = 0; i < amount; i++)
        {
            SpawnOnePickaxe();
        }
    }

    /// <summary>
    /// Равномерно распределяет pivot-пустышки по кругу.
    /// Поворот модели кирки не меняется.
    /// </summary>
    public void RebuildRing()
    {
        CleanupDestroyedPickaxes();

        int count = pickaxes.Count;

        if (count == 0)
            return;

        float step = 360f / count;

        for (int i = 0; i < count; i++)
        {
            PickaxeData data = pickaxes[i];

            if (data.pivot == null || data.visual == null)
                continue;

            float angle = angleOffset + orbitAngle + step * i;

            // Pivot смотрит от центра наружу.
            // Именно pivot вращается вокруг своей Y-оси.
            data.pivot.localRotation = Quaternion.Euler(0f, angle, 0f);

            // Модель остаётся на фиксированной позиции относительно pivot.
            data.visual.localPosition = new Vector3(
                orbitRadius,
                pickaxeHeight,
                0f
            );

            // ВАЖНО:
            // data.visual.localRotation здесь намеренно НЕ меняется.
            // Поэтому сама модель не получает дополнительного вращения.
        }
    }

    private void UpdatePivotsRotation()
    {
        RebuildRing();
    }

    public void ClearPickaxes()
    {
        for (int i = pickaxes.Count - 1; i >= 0; i--)
        {
            if (pickaxes[i].pivot != null)
            {
                Destroy(pickaxes[i].pivot.gameObject);
            }
        }

        pickaxes.Clear();
    }

    public void RemoveLastPickaxe()
    {
        CleanupDestroyedPickaxes();

        if (pickaxes.Count == 0)
            return;

        int lastIndex = pickaxes.Count - 1;
        PickaxeData last = pickaxes[lastIndex];

        if (last.pivot != null)
        {
            Destroy(last.pivot.gameObject);
        }

        pickaxes.RemoveAt(lastIndex);

        RebuildRing();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        UpdateOrbitCenterImmediately();
    }

    public void SetRotationEnabled(bool value)
    {
        rotateOrbit = value;
    }

    public void SetOrbitRadius(float newRadius)
    {
        orbitRadius = Mathf.Max(0.01f, newRadius);
        RebuildRing();
    }

    public void SetMaxPickaxeCount(int newMaxCount)
    {
        maxPickaxeCount = Mathf.Max(1, newMaxCount);

        while (CurrentPickaxeCount > maxPickaxeCount)
        {
            RemoveLastPickaxe();
        }

        RebuildRing();
    }

    private void CleanupDestroyedPickaxes()
    {
        for (int i = pickaxes.Count - 1; i >= 0; i--)
        {
            if (pickaxes[i] == null ||
                pickaxes[i].pivot == null ||
                pickaxes[i].visual == null)
            {
                pickaxes.RemoveAt(i);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform gizmoTarget = target != null ? target : transform;

        if (gizmoTarget == null)
            return;

        Vector3 centerPosition =
            gizmoTarget.position +
            gizmoTarget.TransformDirection(targetLocalOffset);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(centerPosition, 0.14f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(centerPosition, orbitRadius);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(gizmoTarget.position, centerPosition);
    }
}