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
    [Tooltip("Сколько максимум ДОПОЛНИТЕЛЬНЫХ врагов зацепит AoE (работает только для стихийных).")]
    [SerializeField] private int _maxAoeTargets = 3;

    [Header("Эффекты снаряда (на каждом префабе свои)")]
    [Tooltip("Взрыв при попадании — спавнится в точке удара.")]
    [SerializeField] private GameObject _hitEffect;

    [Header("Настройки попадания (Best Practice)")]
    [Tooltip("Радиус самого снаряда (чтобы не пролетал сквозь врагов).")]
    [SerializeField] private float _hitboxRadius = 0.3f;
    [Tooltip("Маска слоев, по которым может попасть снаряд (обычно слои врагов).")]
    [SerializeField] private LayerMask _enemyLayerMask = ~0;

    private int         _damage;
    private float       _distanceTravelled;
    private bool        _active;
    private ElementType _element = ElementType.None;
    private ParticleSystem[] _particles; // трейлы/эффекты самого снаряда

    // Буфер для поиска врагов без выделения памяти (GC Alloc = 0)
    private static Collider[] _aoeHitResults = new Collider[32];

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

        // Best Practice: Continuous Collision Detection через SphereCast.
        // Запускаем сферу из текущей позиции в следующую, чтобы снаряд не пролетал сквозь врагов при большой скорости.
        // Снаряд летит в направлении СВОЕГО forward (учитывает ротацию из Launch — для веера, конусов).
        // Раньше был Vector3.forward — снаряды летели только в мировой +Z независимо от ротации.
        Vector3 fwd = transform.forward;

        if (Physics.SphereCast(transform.position, _hitboxRadius, fwd, out RaycastHit hit, step, _enemyLayerMask, QueryTriggerInteraction.Collide))
        {
            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                // Перемещаемся в точку удара для точного спавна эффектов
                transform.position = transform.position + fwd * hit.distance;
                
                HitTarget(enemy, transform.position);
                return;
            }
        }

        transform.position += fwd * step;
        _distanceTravelled += step;

        if (_distanceTravelled >= _maxDistance)
            ReturnToPool();
    }

    private void HitTarget(Enemy directTarget, Vector3 hitPoint)
    {
        // 1. Гарантированный урон тому, в кого прямо попали
        ApplyDamageToEnemy(directTarget);

        // 2. Урон по площади (AoE) — ТОЛЬКО для стихийных снарядов (не базовых)
        if (_element != ElementType.None && _aoeRadius > 0f)
        {
            // Используем NonAlloc версию, чтобы не выделять массив в памяти каждый раз
            int hitCount = Physics.OverlapSphereNonAlloc(hitPoint, _aoeRadius, _aoeHitResults, _enemyLayerMask, QueryTriggerInteraction.Collide);
            
            // Сортировка вставками (Insertion Sort) — самый быстрый вариант для маленьких массивов.
            // Без делегатов (лямбд), без GC Alloc, используем sqrMagnitude (без тяжелого вычисления корня).
            for (int i = 1; i < hitCount; i++)
            {
                Collider key = _aoeHitResults[i];
                float keyDist = (key.transform.position - hitPoint).sqrMagnitude;
                int j = i - 1;

                while (j >= 0 && (_aoeHitResults[j].transform.position - hitPoint).sqrMagnitude > keyDist)
                {
                    _aoeHitResults[j + 1] = _aoeHitResults[j];
                    j--;
                }
                _aoeHitResults[j + 1] = key;
            }

            int targetsHit = 0;
            for (int i = 0; i < hitCount; i++)
            {
                Enemy enemy = _aoeHitResults[i].GetComponentInParent<Enemy>();
                // Пропускаем того, кому уже нанесли прямой урон
                if (enemy == null || enemy == directTarget) continue;

                ApplyDamageToEnemy(enemy);
                targetsHit++;

                // Ограничиваем количество задетых врагов
                if (targetsHit >= _maxAoeTargets) break;
            }
        }

        // Хит-эффект — ОДИН раз, в точке попадания
        if (_hitEffect != null && VfxPool.Instance != null)
            VfxPool.Instance.Spawn(hitPoint, Quaternion.identity, _hitEffect);

        ReturnToPool();
    }

    private void ApplyDamageToEnemy(Enemy enemy)
    {
        StatusController status = enemy.GetComponent<StatusController>();
        int finalDamage = DamageCalculator.CalculateFinalDamage(_damage, _element, status);

        DamageNumberType numberType = _element switch
        {
            ElementType.Fire      => DamageNumberType.Burn,
            ElementType.Ice       => DamageNumberType.Freeze,
            ElementType.Lightning => DamageNumberType.Shock,
            _                     => DamageNumberType.Normal
        };

        bool died = enemy.TakeDamage(finalDamage, true, numberType);

        if (!died && _element != ElementType.None && status != null)
        {
            StatusEffectType statusToApply = DamageCalculator.GetStatusFromElement(_element);
            status.ApplyStatus(statusToApply, finalDamage);
        }
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
