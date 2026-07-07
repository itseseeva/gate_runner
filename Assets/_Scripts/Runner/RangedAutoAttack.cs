using System.Collections.Generic;
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
    private readonly HashSet<ElementType> _muzzleDiagLogged = new();

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

        // Логируем только первый выстрел каждой стихией — иначе консоль забьётся.
        bool shouldLog = !_muzzleDiagLogged.Contains(element);
        if (shouldLog) _muzzleDiagLogged.Add(element);

        if (shouldLog)
        {
            Debug.Log($"═══════ [Muzzle Diag] {name} стихия={element} prefab={muzzle.name} ═══════", this);

            // 1. Где сейчас рука в мире + куда смотрит герой.
            Debug.Log($"[Muzzle Origin] handWorld={origin.position:F3}, handRotEuler={origin.rotation.eulerAngles:F1}, " +
                      $"heroPos={transform.position:F3}, heroForward={transform.forward:F3}", this);

            // 2. Что лежит внутри самого prefab-а — до спавна.
            Debug.Log($"[Muzzle Prefab ROOT] {muzzle.name}: " +
                      $"pos={muzzle.transform.position:F3}, " +
                      $"localScale={muzzle.transform.localScale:F3}", muzzle);
            foreach (Transform child in muzzle.transform)
            {
                Debug.Log($"[Muzzle Prefab CHILD] {child.name}: " +
                          $"localPos={child.localPosition:F3}, " +
                          $"localScale={child.localScale:F3}", muzzle);
            }
        }

        // ── Спавн ──
        GameObject spawned = VfxPool.Instance.Spawn(origin.position, origin.rotation, muzzle);
        if (spawned == null)
        {
            if (shouldLog) Debug.LogWarning($"[Muzzle Diag] VfxPool вернул null для {muzzle.name}!", this);
            return;
        }

        if (!shouldLog) return;

        // 3. Где ФАКТИЧЕСКИ оказался эффект в сцене после спавна.
        string parentName = spawned.transform.parent != null ? spawned.transform.parent.name : "NULL";
        Debug.Log($"[Muzzle After Spawn ROOT] worldPos={spawned.transform.position:F3}, " +
                  $"parent={parentName}", spawned);
        foreach (Transform child in spawned.transform)
        {
            Debug.Log($"[Muzzle After Spawn CHILD] {child.name}: " +
                      $"worldPos={child.position:F3}, " +
                      $"localPos={child.localPosition:F3}", spawned);
        }

        // 4. Настройки всех ParticleSystem — главные подозреваемые в дрейфе.
        ParticleSystem[] systems = spawned.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            var main = ps.main;
            var shape = ps.shape;
            Debug.Log($"[Muzzle PS] {ps.name}: " +
                      $"SimSpace={main.simulationSpace}, " +
                      $"EmitVelMode={main.emitterVelocityMode}, " +
                      $"StartSpeed={main.startSpeed.constant:F2}, " +
                      $"ShapeEnabled={shape.enabled}, " +
                      $"ShapePos={shape.position:F3}, " +
                      $"psWorldPos={ps.transform.position:F3}, " +
                      $"psLocalPos={ps.transform.localPosition:F3}", ps);
        }
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