using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Логика ближнего боя врага — всё в одном компоненте.
/// Врaг едет через WorldScroller к отряду с хаосом (wobble/lazy),
/// в дистанции AttackRange останавливается и циклично бьёт.
/// Урон летит через Animation Event (EnemyAnimationEventReceiver → OnAnimationHit).
/// Смерть — через Enemy.PlayDeathAnimation (DOTween).
/// </summary>
[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(WorldScroller))]
public class EnemyMeleeCombat : MonoBehaviour
{
    // Константы движения — "чувство" врага, не баланс.
    // TODO: вынести в EnemyDefinitionSO при добавлении разных типов врагов.
    private const float TrackingRange   = 8f;
    // TODO: вынести в EnemyDefinitionSO при добавлении разных типов врагов.
    // 3.5 (было 7) — согласовано со снижением WorldSpeed.
    private const float TrackingSpeed   = 3.5f;
    private const float SeparationForce = 4f;
    private const float WobbleAmount    = 0.3f;
    private const float WobbleSpeed     = 2f;
    private const float LazyChance      = 0.3f;
    private const float LazyDuration    = 0.5f;
    private const float LazyCheckPeriod       = 1f;
    private const float RotationSpeed         = 8f;   // скорость поворота лицом к цели
    private const float MinEnemyGap           = 0.35f; // минимальная дистанция между центрами врагов

    // Все живые враги — для взаимного отталкивания.
    private static readonly List<EnemyMeleeCombat> _all = new();

    private Enemy           _enemy;
    private WorldScroller   _scroller;
    private Animator        _animator;
    private SquadController _squad;
    private CapsuleCollider _myCollider; // кэшируем для Physics.ComputePenetration

    private Unit    _target;
    private bool    _isAttackMode;
    private Vector3 _targetOffset;

    private float _personalSpeedFactor;
    private float _wobblePhase;
    private float _lazyUntil;
    private float _nextLazyCheck;
    private float _chaseOffsetX;      // личный хаос по X
    private float _chaseOffsetZ;      // разброс В ГЛУБИНУ за линией
    private bool  _isChasing;         // после удара — в режиме преследования
    private bool  _hasChased;
    private float _chaseMinUntil;
    private Transform _leader;         // отряд-якорь
    private float _prevLeaderX;

    // Балансные числа из SO через Enemy.Data
    private float AttackRange      => _enemy.Data != null ? _enemy.Data.AttackRange      : 0.7f;
    private int   Damage           => _enemy.Data != null ? _enemy.Data.AttackDamage     : 10;
    private float SeparationRadius => _enemy.Data != null ? _enemy.Data.SeparationRadius : 0.5f;

    private void Awake()
    {
        _enemy      = GetComponent<Enemy>();
        _scroller   = GetComponent<WorldScroller>();
        _animator   = GetComponentInChildren<Animator>();
        _myCollider = GetComponent<CapsuleCollider>();
    }

    private void OnEnable()
    {
        _all.Add(this);
        _personalSpeedFactor = Random.Range(0.7f, 1.3f);
        _wobblePhase         = Random.Range(0f, Mathf.PI * 2f);
        _nextLazyCheck       = 0f;
        _lazyUntil           = 0f;
        _isAttackMode        = false;
        // Случайная точка внутри зоны толпы (овал за отрядом).
        _chaseOffsetX = Random.Range(-2.5f, 2.5f);  // ширина овала по X
        _chaseOffsetZ = Random.Range(-0.5f, 0.5f);
        _isChasing           = false;
        _hasChased           = false;

        if (_scroller != null) _scroller.enabled = true;
        if (_animator != null) _animator.SetBool("IsAttacking", false);
    }

    private void OnDisable()
    {
        _all.Remove(this);

        // Освобождаем цель в реестре при деактивации (пул / cleanup / смерть)
        if (_target != null)
        {
            EnemyTargetRegistry.Unregister(_target);
            _target = null;
        }
    }

    private void Start()
    {
        _squad = FindAnyObjectByType<SquadController>();
        if (_squad != null) _leader = _squad.transform;
        if (_leader != null) _prevLeaderX = _leader.position.x;
    }

    private void Update()
    {
        if (Time.frameCount % 60 == 0)
            Debug.Log($"[UpdRun] {name}: enemy={_enemy != null}, squad={_squad != null}", this);
        
        if (_enemy == null || _squad == null) return;
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying) return;

        UpdateTarget();

        if (_target == null)
        {
            SetAttackMode(false);
            return;
        }

        // Дистанция до цели по XZ (по чистому центру цели, без offset).
        float distSqr = SqrDistanceXZ(transform.position, _target.transform.position);
        float rangeSqr = AttackRange * AttackRange;

        if (_isChasing)
        {
            UpdateChase();
        }
        else if (distSqr <= 0.45f * 0.45f)
        {
            // Подошли вплотную — останавливаемся и бьём из текущей точки. Никаких рывков.
            SetAttackMode(true);
            FaceTarget();
        }
        else
        {
            // Если вошли в широкую зону атаки, сбрасываем оффсет, чтобы точно добежать до цели
            if (distSqr <= rangeSqr)
            {
                _targetOffset = Vector3.zero;
            }
            
            // Продолжаем честное движение через UpdateMovement, пока не подойдем на 0.45
            SetAttackMode(false);
            UpdateMovement();
        }

        ResolveOverlap();
        ResolveHeroOverlap();

        if (Time.frameCount % 60 == 0)
        {
            float dist = _target != null 
                ? Mathf.Sqrt(SqrDistanceXZ(transform.position, _target.transform.position)) 
                : -1f;
            string phase = _isChasing ? "CHASE" : (distSqr <= rangeSqr ? "ATTACK" : "APPROACH");
            string targetName = _target != null ? _target.name : "NULL";
            string targetPosStr = _target != null ? _target.transform.position.ToString("F2") : "NULL";
            Debug.Log($"[EnemyDiag] {name}: phase={phase}, target={targetName}, " +
                      $"dist={dist:F2}, myPos={transform.position:F2}, targetPos={targetPosStr}, " +
                      $"hasChased={_hasChased}", this);
        }
    }

    /// <summary>
    /// Вызывается через Animation Event на клипе Attack (Skeleton_110 → slash01).
    /// EnemyAnimationEventReceiver пробрасывает вызов сюда.
    /// </summary>
    public void OnAnimationHit()
    {
        if (_target == null || _target.IsDead) return;

        // Ещё раз проверяем дистанцию — цель могла отойти пока анимация играла.
        float distSqr = SqrDistanceXZ(transform.position, GetTargetPoint());
        float rangeSqr = AttackRange * AttackRange;
        if (distSqr > rangeSqr) return;

        bool killed = _target.TakeDamage(Damage);
        if (killed)
        {
            _squad?.OnUnitDied(_target);
            EnemyTargetRegistry.Unregister(_target);
            _target = null;
        }
        if (!_hasChased)
        {
            _isChasing = true;
            _hasChased = true;
            _chaseMinUntil = Time.time + 1.5f;
        }

        // При переходе в Chase — сразу разворачиваем врaга по направлению отхода.
        // Slerp в UpdateChase будет только корректировать по ходу движения.
        if (_target != null && _enemy.Data != null)
        {
            Vector3 basePos = _target.transform.position;
            float chaseDist = _enemy.Data.ChaseDistance;
            Vector3 chasePos = new Vector3(basePos.x, transform.position.y, basePos.z - chaseDist);
            Vector3 initDir = chasePos - transform.position;
            initDir.y = 0;
            if (initDir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(initDir);
        }
    }

    // ── Приватная логика ─────────────────────────────────────────────

    /// <summary>
    /// Разделяет капсулы врагов между собой через Physics.ComputePenetration.
    /// Работает во ВСЕХ фазах кроме атаки — атакующий стоит как вкопанный.
    /// </summary>
    private void ResolveOverlap()
    {
        if (_myCollider == null) return;

        foreach (EnemyMeleeCombat other in _all)
        {
            if (other == this || other._myCollider == null) continue;

            if (Physics.ComputePenetration(
                _myCollider,       transform.position,       transform.rotation,
                other._myCollider, other.transform.position, other.transform.rotation,
                out Vector3 dir, out float dist))
            {
                // Каждый выталкивает себя на половину — второй враг сделает то же самое
                transform.position += dir * (dist * 0.5f);
            }
        }
    }

    /// <summary>
    /// Разделяет капсулу врага и капсулы героев через Physics.ComputePenetration.
    /// Unity сам считывает точное перекрытие между капсулами и выдаёт направление/дистанцию отталкивания.
    /// </summary>
    private void ResolveHeroOverlap()
    {
        if (_squad == null || _myCollider == null) return;

        foreach (Unit u in _squad.AllUnits)
        {
            if (u == null || u.IsDead || !u.gameObject.activeSelf) continue;

            var heroCollider = u.GetComponent<CapsuleCollider>();
            if (heroCollider == null) continue;

            if (Physics.ComputePenetration(
                _myCollider,  transform.position, transform.rotation,
                heroCollider, u.transform.position, u.transform.rotation,
                out Vector3 dir, out float dist))
            {
                // Сдвигаем врага ровно на глубину проникновения — капсулы вплотную, но не пересекаются
                transform.position += dir * dist;
            }
        }
    }

    private void UpdateTarget()
    {
        if (_target != null && !_target.IsDead && _target.gameObject.activeSelf) return;

        if (_target != null)
        {
            EnemyTargetRegistry.Unregister(_target);
            _target = null;
        }

        _target = EnemyTargetRegistry.GetLeastAttacked(transform.position, _squad);
        if (_target != null)
        {
            EnemyTargetRegistry.Register(_target);
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(0.3f, 0.7f);
            _targetOffset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        if (_target == null && Time.frameCount % 60 == 0)
        {
            string squadName = _squad != null ? _squad.name : "NULL";
            int squadUnits = _squad?.AllUnits?.Count ?? -1;
            Debug.Log($"[NoTarget] {name}: squad={squadName}, " +
                      $"squadUnits={squadUnits}, myPos={transform.position:F2}", this);
        }
    }

    private void SetAttackMode(bool attacking)
    {
        _isAttackMode = attacking;

        // Всегда синхронизируем scroller — без early return.
        // Иначе состояние scroller может рассинхронизоваться после Chase.
        if (_scroller != null)
            _scroller.enabled = !attacking;

        if (_animator != null)
            _animator.SetBool("IsAttacking", attacking);
    }

    private void UpdateMovement()
    {
        // Множитель скорости — WorldScroller учитывает Frozen-статус
        float speedMul = _scroller != null ? _scroller.SpeedMultiplier : 1f;

        // Lazy — иногда враг "ленится" на 0.5 сек
        if (Time.time >= _nextLazyCheck)
        {
            _nextLazyCheck = Time.time + LazyCheckPeriod;
            if (Random.value < LazyChance)
                _lazyUntil = Time.time + LazyDuration;
        }
        float personalMul = _personalSpeedFactor;

        // Lazy штраф ТОЛЬКО когда близко к цели — не когда ещё догоняем.
        bool isLazyClose = _target != null && 
            SqrDistanceXZ(transform.position, _target.transform.position) < 4f;
        if (Time.time < _lazyUntil && isLazyClose) personalMul *= 0.3f;

        // Wobble — покачивание по X с личной фазой
        float wobble = Mathf.Sin(Time.time * WobbleSpeed + _wobblePhase)
                     * WobbleAmount * speedMul * Time.deltaTime;

        // Tracking по X — всегда (враг ловит линию перед своим героем).
        // Tracking по Z — только если враг ПОЗАДИ отряда (dirZ > 0),
        // чтобы компенсировать WorldScroller, тащащий врага дальше в -Z.
        // Спереди отряда tracking по Z ОТКЛЮЧЁН — иначе WorldScroller и tracking складываются
        // и получается визуальный "рывок" при входе в TrackingRange.
        float trackingDeltaX = 0f;
        float trackingDeltaZ = 0f;
        if (_target != null)
        {
            Vector3 toTarget = GetTargetPoint() - transform.position;
            float distXZ = Mathf.Sqrt(toTarget.x * toTarget.x + toTarget.z * toTarget.z);
            if (distXZ < TrackingRange && distXZ > 0.01f)
            {
                float dirX = toTarget.x / distXZ;
                float dirZ = toTarget.z / distXZ;

                trackingDeltaX = dirX * TrackingSpeed * personalMul * Time.deltaTime;

                // Компенсация WorldScroller только когда враг позади (dirZ > 0 — цель "впереди" по +Z).
                if (dirZ > 0f)
                    trackingDeltaZ = dirZ * TrackingSpeed * personalMul * Time.deltaTime;
            }
        }

        // Отталкивание от других врагов — только пока далеко от цели.
        // Если близко (в 2×AttackRange) — не отталкиваемся, иначе застрянем в куче.
        float separationDeltaX = 0f;
        float separationDeltaZ = 0f;

        bool closeToTarget = false;
        if (_target != null)
        {
            float distToTargetSqr = SqrDistanceXZ(transform.position, GetTargetPoint());
            float noSepRange = AttackRange * 2f;
            closeToTarget = distToTargetSqr < noSepRange * noSepRange;
        }

        if (!closeToTarget)
        {
            float sepRadius = SeparationRadius;
            float sepRadSqr = sepRadius * sepRadius;
            Vector3 myPos = transform.position;

            foreach (EnemyMeleeCombat other in _all)
            {
                if (other == this) continue;
                Vector3 d = myPos - other.transform.position;
                float distSqr = d.x * d.x + d.z * d.z;
                if (distSqr > sepRadSqr) continue;

                float dist = Mathf.Sqrt(distSqr);
                if (dist < 0.01f) dist = 0.01f;

                float strength = (1f - dist / sepRadius) * SeparationForce * personalMul * speedMul * Time.deltaTime;
                separationDeltaX += (d.x / dist) * strength;
                separationDeltaZ += (d.z / dist) * strength;
            }
        }

        Vector3 pos = transform.position;
        pos.x += trackingDeltaX + separationDeltaX + wobble;
        pos.z += trackingDeltaZ + separationDeltaZ;   // trackingDeltaZ работает только когда враг позади

        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[Move] {name}: dX={trackingDeltaX:F3}, " +
                      $"speedMul={speedMul:F2}, personalMul={personalMul:F2}, lazy={Time.time < _lazyUntil}", this);
        }

        transform.position = pos;
    }

    private void FaceTarget()
    {
        if (_target == null) return;
        Vector3 dir = GetTargetPoint() - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.0001f) return;
        // Модель Skeleton_110 повёрнута на -190° — компенсируем через LookRotation(-dir).
        Quaternion targetRot = Quaternion.LookRotation(-dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, RotationSpeed * Time.deltaTime);
    }



    private Vector3 GetTargetPoint()
    {
        if (_target == null) return transform.position;
        return _target.transform.position + _targetOffset;
    }

    private void UpdateChase()
    {
        if (_leader == null) return;
        SetAttackMode(false);
        if (_scroller != null) _scroller.enabled = false;

        // Chase-дистанция берётся из SO напрямую (без привязки к старой константе 7).
        // Раньше делили на 7 — при смене WorldSpeed это ломалось: враг стоял слишком близко.
        float chaseDist = _enemy.Data != null ? _enemy.Data.ChaseDistance : 5f;

        if (_target != null && !_target.IsDead && Time.time >= _chaseMinUntil)
        {
            // Если мир замедлился, chaseDist уменьшается, враг подходит ближе к герою.
            // Выходим из Chase и снова бьём ТОЛЬКО если отряд замедлился (скорость упала)
            // или враг оказался в радиусе атаки.
            float currentDistSqr = SqrDistanceXZ(transform.position, _target.transform.position);
            float triggerRange = AttackRange * 1.5f;

            // Выход из Chase — только когда враг подошёл вплотную к цели.
            // Условие "WorldSpeed < 6f" убрано — при новом WorldSpeed=3.5 оно всегда true
            // и моментально ломало Chase.
            if (currentDistSqr <= triggerRange * triggerRange)
            {
                _isChasing = false;
                _chaseOffsetX = 0f;
                _chaseOffsetZ = 0f;
                _targetOffset = Vector3.zero;
                return;
            }
        }

        // Чем медленнее мир — тем ближе враги подходят к отряду.
        float chaosX    = _enemy.Data != null ? _enemy.Data.ChaseChaosX   : 1.5f;

        Vector3 basePos = _target != null ? _target.transform.position : _leader.position;

        Vector3 targetPos = new Vector3(
            basePos.x + _chaseOffsetX,
            transform.position.y,
            basePos.z - chaseDist + _chaseOffsetZ
        );

        Vector3 dir = targetPos - transform.position;
        dir.y = 0;
        float dist = dir.magnitude;

        if (dist > 0.05f)
        {
            float chaseSpeed = _enemy.Data != null ? _enemy.Data.ChaseSpeed : 3f;
            Vector3 move = dir.normalized * chaseSpeed * Time.deltaTime;
            if (move.magnitude > dist) move = dir;

            if (Time.frameCount % 10 == 0)
            {
                Debug.Log($"[SPEED-CHASE] {name}: " +
                          $"chaseSpeed={chaseSpeed:F2} m/s, " +
                          $"dist={dist:F2}", this);
            }

            transform.position += move;
        }

        // Определяем свайп — движется ли отряд по X.
        float leaderDeltaX = _leader.position.x - _prevLeaderX;
        _prevLeaderX = _leader.position.x;

        // Наклон от свайпа: вправо (+X) → +40°, влево → -40°. Нет свайпа → 0.
        const float maxTurn = 40f;
        float turnAngle = 0f;
        if (leaderDeltaX > 0.001f)  turnAngle =  maxTurn;
        if (leaderDeltaX < -0.001f) turnAngle = -maxTurn;

        // База 190° (прямо вперёд) + наклон. Плавно возвращается к 190 когда свайп кончился.
        Quaternion targetRot = Quaternion.Euler(0f, 190f + turnAngle, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, RotationSpeed * Time.deltaTime);

        // Минимальное время в Chase — проверка перенесена в начало метода.
    }

    private static float SqrDistanceXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }
}
