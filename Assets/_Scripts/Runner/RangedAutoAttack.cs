using UnityEngine;

public class RangedAutoAttack : MonoBehaviour, IUnitAttack
{
    [Header("Параметры стрельбы")]
    [SerializeField] private float _attackSpeed = 1f;
    [SerializeField] private float _range = 10f;
    [SerializeField] private int _baseDamage = 15;
    [SerializeField] private ProjectilePool _projectilePool;
    [SerializeField] private GameObject _projectilePrefab;

    [Header("Дальнобойная атака")]
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

    public void UpdateCooldown() => _lastFireTime = Time.time;
    public void ForceCooldown() => UpdateCooldown();

    public void OnShoot()
    {
        Debug.Log($"[Ranged] OnShoot вызван, {name}", this);

        if (_projectilePool == null) _projectilePool = ProjectilePool.Instance;
        if (_projectilePool == null || _unit == null) return;

        ElementType element = _unit.Element;
        GameObject prefab = _unit.Data != null ? _unit.Data.GetProjectile(element) : _projectilePrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"[RangedAutoAttack] {name}: нет префаба снаряда для {element}.", this);
            return;
        }

        GameObject hitEffect = _unit.Data != null ? _unit.Data.GetHitEffect(element) : null;

        Vector3 spawnPos = transform.position
                         + Vector3.up * _spawnHeightOffset
                         + transform.forward * 1.0f;
        Projectile projectile = _projectilePool.Get(prefab, spawnPos, transform.rotation);
        if (projectile == null) return;

        projectile.Launch(_baseDamage * _unit.PowerMultiplier, _range, element, hitEffect);
    }

    public HitResult Hit(Enemy target)
    {
        if (!IsReady || target == null) return HitResult.Miss();

        Debug.Log($"[Ranged] Hit вызван, {name}, триггер Attack", this);

        if (_animator != null) _animator.SetTrigger("Shoot");
        else Debug.LogWarning($"[Ranged] {name}: _animator == null!", this);

        UpdateCooldown();
        return new HitResult { Hit = true };
    }
}