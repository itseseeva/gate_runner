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

    [Header("Muzzle-эффект")]
    [Tooltip("Кость/точка руки мага — сюда перетащи hand-bone из иерархии.")]
    [SerializeField] private Transform  _muzzlePoint;
    [SerializeField] private GameObject _muzzleEffectBase;      // None / без стихии
    [SerializeField] private GameObject _muzzleEffectFire;      // Fire
    [SerializeField] private GameObject _muzzleEffectIce;       // Ice
    [SerializeField] private GameObject _muzzleEffectLightning; // Lightning

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

    /// <summary>Вызывается читом — триггерит анимацию, снаряд вылетит по animation event OnShoot.</summary>
    public void ForceShoot()
    {
        if (_animator != null)
            _animator.SetTrigger("Shoot");
        else
            OnShoot(); // fallback если аниматора нет
        UpdateCooldown();
    }

    public void OnShoot()
    {
        if (_projectilePool == null) _projectilePool = ProjectilePool.Instance;
        if (_projectilePool == null || _unit == null) return;

        ElementType element = _unit.Element;

        // Unity fake-null safe: используем != null явно
        GameObject prefab = null;
        if (_unit.Data != null)
        {
            prefab = _unit.Data.GetProjectile(element);
        }
        else
        {
            prefab = _projectilePrefab;
        }

        if (prefab == null)
        {
            Debug.LogWarning($"[RangedAutoAttack] {name}: нет префаба снаряда для {element}. Назначьте его в HeroDefinitionSO или в поле _projectilePrefab.", this);
            return;
        }

        Vector3 spawnPos = transform.position
                         + Vector3.up * _spawnHeightOffset
                         + transform.forward * 1.0f;
        Projectile projectile = _projectilePool.Get(prefab, spawnPos, transform.rotation);
        if (projectile == null) return;

        projectile.Launch(_baseDamage * _unit.PowerMultiplier, _range, element);
    }

    public void SpawnMuzzle()
    {
        if (_unit == null) return;
        ElementType element = _unit.Element;
        GameObject muzzle = GetMuzzleEffect(element);
        if (muzzle == null || VfxPool.Instance == null) return;
        Transform origin = _muzzlePoint != null ? _muzzlePoint : transform;
        
        // Применяем чистые данные (смещения и повороты) из самого префаба 
        // относительно точки спавна (руки), чтобы ничего не игнорировалось
        Vector3 finalPos = origin.TransformPoint(muzzle.transform.localPosition);
        Quaternion finalRot = origin.rotation * muzzle.transform.localRotation;

        // Передаём origin как parent, чтобы мазл двигался за рукой!
        VfxPool.Instance.Spawn(finalPos, finalRot, muzzle, origin);
    }

    private GameObject GetMuzzleEffect(ElementType element) => element switch
    {
        ElementType.Fire      => _muzzleEffectFire      != null ? _muzzleEffectFire      : _muzzleEffectBase,
        ElementType.Ice       => _muzzleEffectIce       != null ? _muzzleEffectIce       : _muzzleEffectBase,
        ElementType.Lightning => _muzzleEffectLightning != null ? _muzzleEffectLightning : _muzzleEffectBase,
        _                     => _muzzleEffectBase,
    };

    public HitResult Hit(Enemy target)
    {
        if (!IsReady || target == null) return HitResult.Miss();

        if (_animator != null) _animator.SetTrigger("Shoot");
        else Debug.LogWarning($"[Ranged] {name}: _animator == null!", this);

        UpdateCooldown();
        return new HitResult { Hit = true };
    }
}