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
    private const float TrackingSpeed   = 7f;
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
    private float _chaseMinUntil;
    private Transform _leader;         // отряд-якорь
    private float _prevLeaderX;

    // Балансные числа из SO через Enemy.Data
    private float AttackRange      => _enemy.Data != null ? _enemy.Data.AttackRange      : 0.7f;
    private int   Damage           => _enemy.Data != null ? _enemy.Data.AttackDamage     : 10;
    private float SeparationRadius => _enemy.Data != null ? _enemy.Data.SeparationRadius : 0.5f;

    private void Awake()
    {
        _enemy    = GetComponent<Enemy>();
        _scroller = GetComponent<WorldScroller>();
        _animator = GetComponentInChildren<Animator>();
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
        if (Time.frameCount % 30 == 0)
            Debug.Log($"[Update] {name}: isChasing={_isChasing}, target={_target?.name}", this);

        if (_enemy == null || _squad == null) return;
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying) return;

        UpdateTarget();

        if (_target == null)
        {
            SetAttackMode(false);
            return;
        }

        // Дистанция до цели по XZ
        float distSqr = SqrDistanceXZ(transform.position, GetTargetPoint());
        float rangeSqr = AttackRange * AttackRange;

        if (_isChasing)
        {
            UpdateChase();
        }
        else if (distSqr <= rangeSqr)
        {
            // В зоне атаки — стоим и бьём
            SetAttackMode(true);
            FaceTarget();
        }
        else
        {
            // Первичное преследование — идём к отряду
            SetAttackMode(false);
            UpdateMovement();
        }

        ResolveOverlap();
    }

    private void LateUpdate()
    {
        if (_isChasing)
            Debug.Log($"[ROT LATE] {name}: rotY={transform.rotation.eulerAngles.y:F0}");
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
        _isChasing = true;
        _chaseMinUntil = Time.time + 1.5f;

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
    /// Жёсткий предел пересечения — если враг ближе MinEnemyGap к другому,
    /// выталкивает обоих на минимальную дистанцию по XZ.
    /// Работает во ВСЕХ фазах (атака/чейз/движение) — единая точка ответственности.
    /// </summary>
    private void ResolveOverlap()
    {
        Vector3 myPos = transform.position;
        float minGapSqr = MinEnemyGap * MinEnemyGap;

        foreach (EnemyMeleeCombat other in _all)
        {
            if (other == this) continue;

            Vector3 d = myPos - other.transform.position;
            d.y = 0;
            float distSqr = d.x * d.x + d.z * d.z;

            if (distSqr >= minGapSqr || distSqr < 0.0001f) continue;

            float dist = Mathf.Sqrt(distSqr);
            float overlap = MinEnemyGap - dist;

            // Выталкиваем себя на половину пересечения (второй враг вытолкнет себя сам).
            Vector3 push = (d / dist) * (overlap * 0.5f);
            myPos.x += push.x;
            myPos.z += push.z;
        }

        transform.position = myPos;
    }

    private void UpdateTarget()
    {
        // Цель жива и активна? Оставляем.
        if (_target != null && !_target.IsDead && _target.gameObject.activeSelf) return;

        // Освобождаем в реестре старую (если была)
        if (_target != null)
        {
            EnemyTargetRegistry.Unregister(_target);
            _target = null;
        }

        // Выбираем новую наименее заклеймленную цель
        _target = EnemyTargetRegistry.GetLeastAttacked(transform.position, _squad);
        if (_target != null)
        {
            EnemyTargetRegistry.Register(_target);
            // Разброс точки подхода — каждый враг стоит на своём "радиусе" вокруг цели.
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(0.3f, 0.7f);
            _targetOffset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }
    }

    private void SetAttackMode(bool attacking)
    {
        if (_isAttackMode == attacking) return;
        _isAttackMode = attacking;

        if (_scroller != null)
            _scroller.enabled = !attacking; // в атаке мир нас не тащит

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
        if (Time.time < _lazyUntil) personalMul *= 0.3f;

        // Wobble — покачивание по X с личной фазой
        float wobble = Mathf.Sin(Time.time * WobbleSpeed + _wobblePhase)
                     * WobbleAmount * speedMul * Time.deltaTime;

        // Tracking — тянемся к цели по X
        float trackingDeltaX = 0f;
        if (_target != null)
        {
            Vector3 toTarget = GetTargetPoint() - transform.position;
            float distXZ = Mathf.Sqrt(toTarget.x * toTarget.x + toTarget.z * toTarget.z);
            if (distXZ < TrackingRange && distXZ > 0.01f)
            {
                float dirX = toTarget.x / distXZ;
                trackingDeltaX = dirX * TrackingSpeed * personalMul * speedMul * Time.deltaTime;
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
        pos.z += separationDeltaZ;
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
        float chaseDist = _enemy.Data != null ? _enemy.Data.ChaseDistance : 5f;
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

        // Минимальное время в Chase — не выходим раньше 1.5 сек после удара.
        if (Time.time < _chaseMinUntil) return;

        // Если игрок замедлился и мы догнали цель — снова в атаку.
        if (_target != null)
        {
            float toTargetSqr = SqrDistanceXZ(transform.position, _target.transform.position);
            float rangeSqr = AttackRange * AttackRange;
            if (toTargetSqr <= rangeSqr) _isChasing = false;
        }

        Debug.Log($"[ROT CHECK] {name}: rotY={transform.rotation.eulerAngles.y:F0}, leaderPos={_leader.position:F1}, myPos={transform.position:F1}");

        if (Time.frameCount % 20 == 0)
        {
            float moveAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            Debug.Log($"[CHASE ROT] {name}: rotY={transform.rotation.eulerAngles.y:F0}, " +
                      $"moveDir_angle={moveAngle:F0}, dist={dist:F2}, isChasing={_isChasing}", this);
        }
    }

    private static float SqrDistanceXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }
}
