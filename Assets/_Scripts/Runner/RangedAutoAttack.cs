using UnityEngine;

/// <summary>
/// Компонент стрельбы для дальних юнитов (лучник, маг).
/// Вызывается через Animation Event (OnShoot) в момент выстрела.
/// Снаряд и хит-эффект берутся из данных юнита (HeroDefinitionSO) по стихии.
/// </summary>
public class RangedAutoAttack : MonoBehaviour, IUnitAttack
{
    [Header("Параметры стрельбы")]
    [SerializeField] private float _attackSpeed = 1f;
    [SerializeField] private float _range = 10f;
    [SerializeField] private int _baseDamage = 15;
    [SerializeField] private ProjectilePool _projectilePool;
    [SerializeField] private GameObject _projectilePrefab; // запасной, если в SO нет

    [Header("Дальнобойная атака")]
    [Tooltip("Высота спавна снаряда относительно юнита")]
    [SerializeField] private float _spawnHeightOffset = 0.5f;

    private float _lastFireTime = -999f;
    private Unit _unit;
    private Animator _animator;

    public float Range => _range;
    public bool IsReady => Time.time - _lastFireTime >= (1f / _attackSpeed);

    protected virtual void Awake()
    {
        _unit = GetComponent<Unit>();
        _animator = GetComponentInChildren<Animator>();
    }

    /// <summary>Обновляет время последнего выстрела.</summary>
    public void UpdateCooldown() => _lastFireTime = Time.time;

    /// <summary>Принудительный запуск кулдауна (для чита / ручного вызова).</summary>
    public void ForceCooldown() => UpdateCooldown();

    /// <summary>
    /// Вызывается из Animation Event в момент выстрела. Спавнит снаряд через пул.
    /// </summary>
    public void OnShoot()
    {
        if (_projectilePool == null || _unit == null) return;

        ElementType element = _unit.Element;

        // Префаб снаряда из данных юнита (у мага и лучника разные).
        GameObject prefab = _unit.Data != null ? _unit.Data.GetProjectile(element) : _projectilePrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"[RangedAutoAttack] {name}: нет префаба снаряда для стихии {element}.", this);
            return;
        }

        GameObject hitEffect = _unit.Data != null ? _unit.Data.GetHitEffect(element) : null;

        Vector3 spawnPos = transform.position + Vector3.up * _spawnHeightOffset;
        Projectile projectile = _projectilePool.Get(prefab, spawnPos, transform.rotation);
        if (projectile == null) return;

        projectile.Launch(_baseDamage * _unit.PowerMultiplier, _range, element, hitEffect);
    }

    /// <summary>
    /// Запускает анимацию стрельбы. Сам снаряд спавнится через Animation Event (OnShoot).
    /// </summary>
    public HitResult Hit(Enemy target)
    {
        if (!IsReady || target == null) return HitResult.Miss();

        if (_animator != null)
            _animator.SetTrigger("Attack");

        UpdateCooldown();

        return new HitResult { Hit = true };
    }
}