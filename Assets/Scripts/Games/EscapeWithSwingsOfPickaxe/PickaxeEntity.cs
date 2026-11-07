using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PickaxeEntity : MonoBehaviour
{
    [Header("Урон")]

    [SerializeField]
    [Min(0.01f)]
    private float damage = 1f;

    [SerializeField]
    [Min(0f)]
    private float hitCooldown = 0.25f;

    [SerializeField]
    private bool canDealDamage = true;

    [Header("Настройки Collider")]

    [Tooltip("Автоматически делает Collider триггером.")]
    [SerializeField]
    private bool setColliderAsTrigger = true;

    [Tooltip("Автоматически добавляет kinematic Rigidbody, если его нет.")]
    [SerializeField]
    private bool addKinematicRigidbodyIfMissing = true;

    [SerializeField]
    private bool searchBlockInParent = true;

    [Header("Дебаг")]

    [SerializeField]
    private bool showDebugLogs = true;

    private Collider _hitCollider;
    private Rigidbody _rigidbody;

    private readonly Dictionary<BlockEntity, float> _lastHitTimes =
        new Dictionary<BlockEntity, float>();

    public float Damage => damage;
    public float HitCooldown => hitCooldown;
    public bool CanDealDamage => canDealDamage;

    private void Awake()
    {
        SetupPhysics();
    }

    private void OnDisable()
    {
        _lastHitTimes.Clear();
    }

    private void SetupPhysics()
    {
        _hitCollider = GetComponent<Collider>();

        if (_hitCollider == null)
        {
            Debug.LogError(
                $"{nameof(PickaxeEntity)}: Нужен Collider на объекте hitbox.",
                this
            );
            return;
        }

        if (setColliderAsTrigger)
        {
            _hitCollider.isTrigger = true;
        }

        _rigidbody = GetComponent<Rigidbody>();

        if (_rigidbody == null && addKinematicRigidbodyIfMissing)
        {
            _rigidbody = gameObject.AddComponent<Rigidbody>();
        }

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
            _rigidbody.interpolation = RigidbodyInterpolation.None;
            _rigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    // Урон наносится ТОЛЬКО при входе в триггер.
    private void OnTriggerEnter(Collider other)
    {
        TryDamageBlock(other, applyCooldown: true);
    }

    // При желании можно вообще убрать OnTriggerStay или оставить пустым.
    private void OnTriggerStay(Collider other)
    {
        // Ничего не делаем — урон только по Enter.
    }

    private void TryDamageBlock(Collider other, bool applyCooldown = true)
    {
        if (!canDealDamage || other == null)
            return;

        BlockEntity block = other.GetComponent<BlockEntity>();

        if (block == null && searchBlockInParent)
        {
            block = other.GetComponentInParent<BlockEntity>();
        }

        if (block == null || !block.CanReceiveDamage)
            return;

        // Если нужен кулдаун между входами в один и тот же блок:
        if (applyCooldown && !CanHitBlockNow(block))
            return;

        if (applyCooldown)
        {
            _lastHitTimes[block] = Time.time;
        }

        Vector3 hitPoint = other.ClosestPoint(transform.position);

        bool wasDestroyed = block.TakeDamage(
            damage,
            this,
            hitPoint
        );

        if (showDebugLogs)
        {
            Debug.Log(
                $"[PickaxeEntity] {name} → {block.name}. " +
                $"Урон: {damage:0.##}; HP блока: " +
                $"{block.CurrentHealth:0.##}/{block.MaxHealth:0.##}",
                this
            );
        }

        if (wasDestroyed)
        {
            _lastHitTimes.Remove(block);
        }
    }

    private bool CanHitBlockNow(BlockEntity block)
    {
        if (hitCooldown <= 0f)
            return true;

        if (!_lastHitTimes.TryGetValue(block, out float lastHitTime))
            return true;

        return Time.time >= lastHitTime + hitCooldown;
    }

    public void SetDamage(float value)
    {
        damage = Mathf.Max(0.01f, value);
    }

    public void SetHitCooldown(float value)
    {
        hitCooldown = Mathf.Max(0f, value);
    }

    public void SetCanDealDamage(bool value)
    {
        canDealDamage = value;
    }

    private void OnDrawGizmosSelected()
    {
        Collider hitCollider = GetComponent<Collider>();

        if (hitCollider == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            hitCollider.bounds.center,
            hitCollider.bounds.size
        );
    }
}