using UnityEngine;

/// <summary>
/// Летящий снаряд. Движется вперёд по Z на заданной скорости.
/// При попадании во врага наносит урон и возвращается в пул.
/// Самодостаточный: сам спавнит muzzle при запуске и hit при попадании.
/// Эффекты задаются прямо на префабе снаряда (SerializeField).
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("Параметры")]
    [SerializeField] private float _speed       = 15f;
    [SerializeField] private float _maxDistance = 16f;

    [Header("AoE — урон по площади")]
    [Tooltip("Радиус урона по площади. Не связан с визуалом эффекта.")]
    [SerializeField] private float _aoeRadius = 2f;

    [Header("Эффекты снаряда (на каждом префабе свои)")]
    [Tooltip("Взрыв при попадании — спавнится в точке удара.")]
    [SerializeField] private GameObject _hitEffect;

    private int         _damage;
    private float       _distanceTravelled;
    private bool        _active;
    private ElementType _element = ElementType.None;
    private ParticleSystem[] _particles; // трейлы/эффекты самого снаряда

    /// <summary>Стихия снаряда — нужна пулу, чтобы вернуть в правильную очередь.</summary>
    public ElementType Element => _element;

    /// <summary>Префаб, из которого создан снаряд — для возврата в нужную очередь пула.</summary>
    public GameObject SourcePrefab { get; set; }

    private void Awake()
    {
        _particles = GetComponentsInChildren<ParticleSystem>(true);
    }

    /// <summary>Запускает снаряд. Урон, дистанция, стихия. Спавнит muzzle в точке старта.</summary>
    public void Launch(int damage, float maxDistance, ElementType element)
    {
        _damage            = damage;
        _maxDistance       = maxDistance;
        _element           = element;
        _distanceTravelled = 0f;
        _active            = true;

        RestartParticles();
    }

    private void RestartParticles()
    {
        if (_particles == null) return;
        foreach (var ps in _particles)
        {
            ps.Clear(true);
            ps.Play(true);
        }
    }

    private void Update()
    {
        if (!_active) return;

        float step = _speed * Time.deltaTime;
        transform.position += Vector3.forward * step;
        _distanceTravelled += step;

        if (_distanceTravelled >= _maxDistance)
            ReturnToPool();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_active) return;

        // Попали хотя бы в одного врага?
        Enemy firstEnemy = other.GetComponent<Enemy>();
        if (firstEnemy == null) return;

        // Точка попадания — центр зоны урона
        Vector3 hitPoint = transform.position;

        // Находим всех врагов в радиусе AoE
        Collider[] hits = Physics.OverlapSphere(hitPoint, _aoeRadius, ~0, QueryTriggerInteraction.Collide);
        foreach (Collider col in hits)
        {
            Enemy enemy = col.GetComponent<Enemy>();
            if (enemy == null) continue;

            // Урон каждому врагу в радиусе (со стихией и статусами)
            StatusController status = enemy.GetComponent<StatusController>();
            int finalDamage = DamageCalculator.CalculateFinalDamage(_damage, _element, status);

            bool died = enemy.TakeDamage(finalDamage);

            if (!died && _element != ElementType.None && status != null)
            {
                StatusEffectType statusToApply = DamageCalculator.GetStatusFromElement(_element);
                status.ApplyStatus(statusToApply, finalDamage);
            }
        }

        // Хит-эффект — ОДИН раз, в точке попадания (не на каждом враге)
        if (_hitEffect != null && VfxPool.Instance != null)
            VfxPool.Instance.Spawn(hitPoint, Quaternion.identity, _hitEffect);

        ReturnToPool();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _aoeRadius);
    }

    private void ReturnToPool()
    {
        _active = false;

        if (_particles != null)
            foreach (var ps in _particles)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ProjectilePool.Instance.Return(this);
    }
}
