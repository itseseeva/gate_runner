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
    private bool  _isChasing;         // после удара — в режиме преследования
    private float _chaseMinUntil;
    private Transform _leader;         // отряд-якорь

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
        _chaseOffsetX        = Random.Range(-1f, 1f); // единичный, потом умножим на _enemy.Data.ChaseChaosX
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
    }

    private void Update()
    {
        Debug.Log($"[UPDATE START] {name}: isChasing={_isChasing}, rotY={transform.rotation.eulerAngles.y:F1}", this);
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
                transform.rotation = Quaternion.LookRotation(-initDir);
        }

        Debug.Log($"[HIT AFTER SET] {name}: setRotY={transform.rotation.eulerAngles.y:F1}", this);
        Debug.Log($"[HIT] {name}: _isChasing set to TRUE, target={_target?.name}", this);
        Debug.Log($"[HIT→CHASE ROT] {name}: newRotY={transform.rotation.eulerAngles.y:F1}, " +
                  $"myPos={transform.position:F3}, targetPos={_target?.transform.position:F3}", this);
    }

    // ── Приватная логика ─────────────────────────────────────────────

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

        // Отходим за СВОЮ цель на chaseDist — тогда враг стоит рядом с юнитом
        // которого атаковал, а не за общим "лидером отряда".
        Vector3 basePos = _target != null ? _target.transform.position : _leader.position;
        Vector3 targetPos = new Vector3(
            basePos.x + _chaseOffsetX * chaosX,
            transform.position.y,
            basePos.z - chaseDist
        );

        Vector3 dir = targetPos - transform.position;
        dir.y = 0;
        float dist = dir.magnitude;

        if (Time.frameCount % 30 == 0)
            Debug.Log($"[Chase] {name}: chaseDist={chaseDist}, targetPos={targetPos:F2}, " +
                      $"myPos={transform.position:F2}, dist={dist:F2}, leader={_leader.position:F2}", this);

        if (dist > 0.05f)
        {
            float chaseSpeed = _enemy.Data != null ? _enemy.Data.ChaseSpeed : 3f;
            Vector3 move = dir.normalized * chaseSpeed * Time.deltaTime;
            if (move.magnitude > dist) move = dir;
            transform.position += move;
        }

        // Крутимся только пока движемся. Если стоим на точке — сохраняем поворот
        // от момента прибытия. Иначе Slerp начинает вертеть врaга в jitter-шум позиции.
        if (dist > 0.05f && dir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(-dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, RotationSpeed * Time.deltaTime);
        }

        // Минимальное время в Chase — не выходим раньше 1.5 сек после удара.
        if (Time.time < _chaseMinUntil) return;

        // Если игрок замедлился и мы догнали цель — снова в атаку.
        if (_target != null)
        {
            float toTargetSqr = SqrDistanceXZ(transform.position, _target.transform.position);
            float rangeSqr = AttackRange * AttackRange;
            if (toTargetSqr <= rangeSqr) _isChasing = false;
        }

        if (Time.frameCount % 15 == 0)
            Debug.Log($"[CHASE MOVE] {name}: rotY={transform.rotation.eulerAngles.y:F1}, " +
                      $"dir={dir:F3}, dist={dist:F2}, moving={dist > 0.05f}", this);
    }

    private static float SqrDistanceXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }
}
