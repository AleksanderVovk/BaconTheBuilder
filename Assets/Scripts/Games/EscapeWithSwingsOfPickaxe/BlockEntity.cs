using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BlockEntity : MonoBehaviour
{
    [Header("Данные блока")]
    [SerializeField] private string blockId = "Stone";

    [Tooltip("Максимальное здоровье блока.")]
    [Min(0.01f)]
    [SerializeField] private float maxHealth = 10f;

    [Tooltip("Текущее здоровье.")]
    [SerializeField] private float currentHealth = 10f;

    [Tooltip("Блок может получать урон.")]
    [SerializeField] private bool canReceiveDamage = true;

    [Header("Реакция на разрушение")]
    [Tooltip("Если включено — объект блока уничтожится при HP <= 0.")]
    [SerializeField] private bool destroyGameObjectOnDeath = true;

    [Tooltip("Задержка перед уничтожением объекта.")]
    [Min(0f)]
    [SerializeField] private float destroyDelay = 0f;

    [Tooltip("Эффект разрушения.")]
    [SerializeField] private GameObject destroyEffectPrefab;

    [Tooltip("Предмет, который выпадет при разрушении.")]
    [SerializeField] private GameObject dropPrefab;

    [Tooltip("Количество выпадающих предметов.")]
    [Min(0)]
    [SerializeField] private int dropCount = 0;

    [Tooltip("Радиус разброса дропа.")]
    [Min(0f)]
    [SerializeField] private float dropSpreadRadius = 0.4f;

    [Header("Эффекты попадания")]
    [SerializeField] private ParticleSystem damageParticle;

    [Tooltip("Рендереры блока.")]
    [SerializeField] private Renderer[] blockRenderers;

    [Tooltip("Мигать при получении урона.")]
    [SerializeField] private bool flashOnDamage = true;

    [Tooltip("Цвет вспышки.")]
    [SerializeField] private Color damageFlashColor = Color.red;

    [Tooltip("Длительность вспышки.")]
    [Min(0f)]
    [SerializeField] private float flashDuration = 0.08f;

    [Header("Дебаг")]
    [SerializeField] private bool showDebugLogs = true;

    public string BlockId => blockId;
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;

    public bool CanReceiveDamage =>
        canReceiveDamage && !_isDestroyed;

    public float HealthPercent =>
        maxHealth <= 0f
            ? 0f
            : currentHealth / maxHealth;

    public event Action<BlockEntity, float, PickaxeEntity> OnDamaged;
    public event Action<BlockEntity, PickaxeEntity> OnDestroyed;

    private bool _isDestroyed;

    private MaterialPropertyBlock _propertyBlock;

    private static readonly int DamageColorId =
        Shader.PropertyToID("_DamageColor");

    private static readonly int DamageFlashId =
        Shader.PropertyToID("_DamageFlash");

    private Coroutine _flashCoroutine;

    private void Reset()
    {
        currentHealth = maxHealth;
        damageFlashColor = Color.red;
    }

    private void Awake()
    {
        maxHealth = Mathf.Max(
            0.01f,
            maxHealth
        );

        if (currentHealth <= 0f ||
            currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        if (blockRenderers == null ||
            blockRenderers.Length == 0)
        {
            blockRenderers =
                GetComponentsInChildren<Renderer>();
        }

        _propertyBlock =
            new MaterialPropertyBlock();

        ResetDamageFlash();
    }

    public bool TakeDamage(
        float damage,
        PickaxeEntity attacker = null,
        Vector3 hitPoint = default
    )
    {
        if (!CanReceiveDamage)
        {
            return false;
        }

        if (damage <= 0f)
        {
            return false;
        }

        currentHealth = Mathf.Max(
            0f,
            currentHealth - damage
        );

        if (damageParticle != null)
        {
            damageParticle.transform.position =
                hitPoint;

            damageParticle.Play();
        }

        if (flashOnDamage)
        {
            FlashDamageColor();
        }

        OnDamaged?.Invoke(
            this,
            damage,
            attacker
        );

        if (showDebugLogs)
        {
            string attackerName =
                attacker != null
                    ? attacker.name
                    : "Unknown";

            Debug.Log(
                $"[BlockEntity] {name} получил " +
                $"{damage:0.##} урона от {attackerName}. " +
                $"HP: {currentHealth:0.##}/" +
                $"{maxHealth:0.##}",
                this
            );
        }

        if (currentHealth > 0f)
        {
            return false;
        }

        DestroyBlock(attacker);

        return true;
    }

    public void Heal(float value)
    {
        if (_isDestroyed || value <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Min(
            maxHealth,
            currentHealth + value
        );
    }

    public void SetHealth(float value)
    {
        if (_isDestroyed)
        {
            return;
        }

        currentHealth = Mathf.Clamp(
            value,
            0f,
            maxHealth
        );

        if (currentHealth <= 0f)
        {
            DestroyBlock(null);
        }
    }

    public void SetCanReceiveDamage(bool value)
    {
        canReceiveDamage = value;
    }

    private void DestroyBlock(
        PickaxeEntity attacker
    )
    {
        if (_isDestroyed)
        {
            return;
        }

        _isDestroyed = true;
        canReceiveDamage = false;

        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            _flashCoroutine = null;
        }

        ResetDamageFlash();

        OnDestroyed?.Invoke(
            this,
            attacker
        );

        if (showDebugLogs)
        {
            Debug.Log(
                $"[BlockEntity] Блок разрушен: {name}.",
                this
            );
        }

        SpawnDestroyEffect();
        SpawnDrops();

        if (destroyGameObjectOnDeath)
        {
            Destroy(
                gameObject,
                destroyDelay
            );
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void SpawnDestroyEffect()
    {
        if (destroyEffectPrefab == null)
        {
            return;
        }

        Instantiate(
            destroyEffectPrefab,
            transform.position,
            Quaternion.identity
        );
    }

    private void SpawnDrops()
    {
        if (dropPrefab == null ||
            dropCount <= 0)
        {
            return;
        }

        for (int i = 0; i < dropCount; i++)
        {
            Vector2 randomCircle =
                UnityEngine.Random.insideUnitCircle *
                dropSpreadRadius;

            Vector3 spawnPosition =
                transform.position +
                new Vector3(
                    randomCircle.x,
                    0.2f,
                    randomCircle.y
                );

            Instantiate(
                dropPrefab,
                spawnPosition,
                UnityEngine.Random.rotation
            );
        }
    }

    private void FlashDamageColor()
    {
        if (blockRenderers == null ||
            blockRenderers.Length == 0)
        {
            return;
        }

        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
        }

        _flashCoroutine =
            StartCoroutine(
                FlashDamageRoutine()
            );
    }

    private IEnumerator FlashDamageRoutine()
    {
        SetDamageFlash(
            damageFlashColor,
            1f
        );

        yield return new WaitForSeconds(
            flashDuration
        );

        ResetDamageFlash();

        _flashCoroutine = null;
    }

    private void SetDamageFlash(
    Color color,
    float amount
)
    {
        if (blockRenderers == null)
        {
            return;
        }

        for (int i = 0; i < blockRenderers.Length; i++)
        {
            Renderer targetRenderer =
                blockRenderers[i];

            if (targetRenderer == null)
            {
                continue;
            }

            targetRenderer.GetPropertyBlock(
                _propertyBlock
            );

            _propertyBlock.SetColor(
                DamageColorId,
                color
            );

            _propertyBlock.SetFloat(
                DamageFlashId,
                amount
            );

            targetRenderer.SetPropertyBlock(
                _propertyBlock
            );
        }
    }

    private void ResetDamageFlash()
    {
        if (_propertyBlock == null)
        {
            _propertyBlock =
                new MaterialPropertyBlock();
        }

        if (blockRenderers == null)
        {
            return;
        }

        for (int i = 0; i < blockRenderers.Length; i++)
        {
            Renderer targetRenderer =
                blockRenderers[i];

            if (targetRenderer == null)
            {
                continue;
            }

            targetRenderer.GetPropertyBlock(
                _propertyBlock
            );

            _propertyBlock.SetFloat(
                DamageFlashId,
                0f
            );

            targetRenderer.SetPropertyBlock(
                _propertyBlock
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        Collider blockCollider =
            GetComponent<Collider>();

        if (blockCollider == null)
        {
            return;
        }

        float healthPercent =
            maxHealth <= 0f
                ? 0f
                : Mathf.Clamp01(
                    currentHealth / maxHealth
                );

        Gizmos.color = Color.Lerp(
            Color.red,
            Color.green,
            healthPercent
        );

        Gizmos.DrawWireCube(
            blockCollider.bounds.center,
            blockCollider.bounds.size
        );
    }
}