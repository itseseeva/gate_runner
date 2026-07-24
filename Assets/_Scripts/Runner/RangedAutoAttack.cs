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

    [Header("Спецатака (веер)")]
    [Tooltip("Включает спецатаку для этого юнита. У лучника — да, у мага — нет.")]
    [SerializeField] private bool _hasSpecialAttack = false;

    [Tooltip("Каждая N-я атака будет спецатакой. 3 = 3-я, 6-я, 9-я...")]
    [SerializeField] private int _attacksBetweenSpecial = 3;

    [Tooltip("Сколько стрел вылетает при спецатаке")]
    [Range(2, 7)]
    [SerializeField] private int _specialArrowCount = 3;

    [Tooltip("Общий угол разлёта веера в градусах (±половина от центра)")]
    [Range(10f, 90f)]
    [SerializeField] private float _specialSpreadAngle = 30f;

    [Tooltip("Сколько залпов веера в одной спецатаке (должно совпадать с числом OnShoot Event на клипе SpecialAttackRun)")]
    [SerializeField] private int _specialFanVolleys = 3;

    private int _attackCounter = 0;
    private bool _isSpecialShot = false;
    
    // Счётчик оставшихся веерных залпов. Пока > 0 — каждый OnShoot спавнит веер.
    private int _pendingFanShots = 0;

    // Safety-timeout от вечной блокировки, если по какой-то причине OnShoot Event не прилетел.
    private float _pendingFanExpireTime = 0f;

    // Блокировка прерывания обычной атаки: ждём хотя бы первого OnShoot.
    private bool _waitForAnimationEvent = false;
    private float _waitForAnimationTimeout = 0f;

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



    public void OnShoot()
    {
        // Как только сработал хотя бы один OnShoot (обычный или первый из веера) — снимаем блок
        _waitForAnimationEvent = false;

        if (_projectilePool == null) _projectilePool = ProjectilePool.Instance;
        if (_projectilePool == null || _unit == null) return;

        {}

        ElementType element = _unit.Element;

        GameObject prefab = null;
        if (_unit.Data != null)
            prefab = _unit.Data.GetProjectile(element);
        
        // Если стихийный префаб не назначен в HeroDefinitionSO, падаем на дефолтный
        if (prefab == null)
            prefab = _projectilePrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"[RangedAutoAttack] {name}: нет префаба снаряда для {element}. " +
                             $"Назначьте его в HeroDefinitionSO или в поле _projectilePrefab.", this);
            
            // Важно списать заряд, иначе зависнем!
            if (_pendingFanShots > 0) _pendingFanShots--;
            return;
        }

        Vector3 spawnPos = transform.position
                         + Vector3.up * _spawnHeightOffset
                         + transform.forward * 1.0f;

        int damage = _baseDamage * _unit.PowerMultiplier;

        // Решение "веер или одиночка" на основе счётчика pendingFanShots,
        // а не флага _isSpecialShot. Так работа не зависит от того, что играет аниматор
        // прямо сейчас — только от того, сколько залпов мы ещё должны выпустить.
        if (_pendingFanShots > 0)
        {
            {}

            // ── Веер: несколько стрел с равномерным разлётом по углу ──
            int count = _specialArrowCount;
            float halfSpread = _specialSpreadAngle * 0.5f;
            float angleStep  = _specialSpreadAngle / (count - 1);

            for (int i = 0; i < count; i++)
            {
                float angle = -halfSpread + angleStep * i;
                Quaternion rot = transform.rotation * Quaternion.Euler(0f, angle, 0f);
                SpawnOneArrow(prefab, spawnPos, rot, damage, element);
            }

            _pendingFanShots--; // израсходовали один залп
        }
        else
        {
            {}
            SpawnOneArrow(prefab, spawnPos, transform.rotation, damage, element);
        }
    }

    /// <summary>Спавн одной стрелы в позицию spawnPos с ротацией rot.</summary>
    private void SpawnOneArrow(GameObject prefab, Vector3 spawnPos, Quaternion rot,
                               int damage, ElementType element)
    {
        Projectile projectile = _projectilePool.Get(prefab, spawnPos, rot);
        if (projectile == null) return;
        projectile.Launch(damage, _range, element);
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
            {}

            // 1. Где сейчас рука в мире + куда смотрит герой.
            {}

            // 2. Что лежит внутри самого prefab-а — до спавна.
            {}
            foreach (Transform child in muzzle.transform)
            {
                {}
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
        {}
        foreach (Transform child in spawned.transform)
        {
            {}
        }

        // 4. Настройки всех ParticleSystem — главные подозреваемые в дрейфе.
        ParticleSystem[] systems = spawned.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            var main = ps.main;
            var shape = ps.shape;
            {}
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
        if (!IsReady) return HitResult.Miss();

        // Пока не отстрелялись все залпы спецатаки — новую атаку не принимаем.
        if (_pendingFanShots > 0) return HitResult.Miss();

        // Ждём OnShoot от прошлой атаки (защита от слишком частых Hit при высоком attack speed).
        if (_waitForAnimationEvent && Time.time < _waitForAnimationTimeout) return HitResult.Miss();

        PerformAttack();
        return new HitResult { Hit = true };
    }

    /// <summary>Вызывается читом — идёт по той же логике что обычная атака, чтобы спецатака тоже работала.</summary>
    public void ForceShoot()
    {
        if (!IsReady) return;
        if (_pendingFanShots > 0) return;
        if (_waitForAnimationEvent && Time.time < _waitForAnimationTimeout) return;

        PerformAttack();
    }

    /// <summary>
    /// Единая точка запуска атаки. Считает счётчик, определяет спецатаку,
    /// выставляет параметры аниматора и cooldown.
    /// Вызывается и из Hit (обычный бой), и из ForceShoot (чит смены стихии).
    /// </summary>
    private void PerformAttack()
    {
        if (_hasSpecialAttack)
        {
            _attackCounter++;
            _isSpecialShot = (_attackCounter % _attacksBetweenSpecial == 0);

            if (_isSpecialShot)
            {
                _pendingFanShots = _specialFanVolleys;
                _pendingFanExpireTime = Time.time + 2f;
            }
        }
        else
        {
            _isSpecialShot = false;
        }

        if (_animator != null)
        {
            if (_hasSpecialAttack)
                _animator.SetBool("SpecialAttack", _isSpecialShot);
            _animator.SetTrigger("Shoot");
        }
        else
        {
            // fallback без аниматора — стреляем сразу
            OnShoot();
        }

        _waitForAnimationEvent = true;
        _waitForAnimationTimeout = Time.time + 1.5f;

        UpdateCooldown();
    }

    private void Update()
    {
        // Safety: если по какой-то причине OnShoot Event не пришёл за 2 сек —
        // не блокируем лучника навсегда.
        if (_pendingFanShots > 0 && Time.time > _pendingFanExpireTime)
        {
            Debug.LogWarning($"[Archer] Спецатака timeout — сбрасываю {_pendingFanShots} залпов", this);
            _pendingFanShots = 0;
            _waitForAnimationEvent = false;
        }
    }
}