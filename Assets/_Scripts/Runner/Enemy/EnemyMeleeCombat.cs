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
    private float _nextTargetReevaluateTime = 0f;
    private bool  _isPhasing = false;
    private float _blockedTimer = 0f;

    public bool IsChasing => _isChasing;
    public bool IsInAttackStateFlag { get; set; }

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

        if (_myCollider != null)
        {
            _myCollider.enabled = !_isPhasing;
        }

        UpdateTarget();

        if (_target == null)
        {
            SetAttackMode(false);
            _isPhasing = false;
            return;
        }

        Vector3 realTargetPoint = GetTargetPoint();
        Vector3 toTarget = realTargetPoint - transform.position;

        // Дистанция до цели по XZ — меряем до реальной точки подхода (с оффсетом).
        // Если offset (0.3-0.7м) больше AttackRange, враг никогда не войдёт в зону атаки!
        Vector3 approachPoint = GetTargetPoint();
        float distSqr = SqrDistanceXZ(transform.position, approachPoint);
        float rangeSqr = AttackRange * AttackRange;

        // ЛОГИКА ПРОСАЧИВАНИЯ (PHASING):
        // Если мы бежим к цели (не в атаке и не в чейзе), но упёрлись в другого врага перед собой:
        if (!_isChasing && !_isAttackMode)
        {
            bool isOverlappingOtherEnemy = false;
            float checkDist = 0.48f; // Чуть меньше minDistance расталкивания (0.5м)
            foreach (EnemyMeleeCombat other in _all)
            {
                if (other == this || other._isChasing) continue;
                if (SqrDistanceXZ(transform.position, other.transform.position) < checkDist * checkDist)
                {
                    isOverlappingOtherEnemy = true;
                    break;
                }
            }

            if (isOverlappingOtherEnemy && distSqr > rangeSqr)
            {
                _blockedTimer += Time.deltaTime;
                if (_blockedTimer > 0.4f) // Если заблокирован дольше 0.4 сек — включаем просачивание
                {
                    _isPhasing = true;
                }
            }
            else
            {
                _blockedTimer = 0f;
            }
        }
        else
        {
            _blockedTimer = 0f;
            _isPhasing = false;
        }

        // Если мы просачивались и уже дошли до радиуса атаки — выключаем фазирование
        if (_isPhasing && distSqr <= rangeSqr * 1.1f)
        {
            _isPhasing = false;
        }

        if (_isChasing)
        {
            if (IsInAttackState())
            {
                // Позволяем анимации атаки (и всем комбо-ударам) завершиться до конца, стоя на месте.
                // НЕ отключаем _isAttackMode полностью, чтобы скроллер оставался выключенным и враг не уезжал от героя.
                // Но аниматору говорим остановиться, чтобы он мог перейти в Run по завершению комбо.
                if (_animator != null) 
                {
                    if (_animator.GetBool("IsAttacking"))
                        Debug.Log($"[{gameObject.name}] Update: IsInAttackState=true. Forcing IsAttacking=false on Animator.");
                    _animator.SetBool("IsAttacking", false);
                }
                
                FaceTarget();
                ResolveOverlap();
                
                // Продлеваем таймер чейза пока проигрывается анимация,
                // чтобы 1.5 секунды начали тикать только когда враг реально побежит.
                _chaseMinUntil = Time.time + 1.5f;
                return;
            }

            Debug.Log($"[{gameObject.name}] Update: IsInAttackState=false. Starting UpdateChase.");
            UpdateChase();
        }
        else
        {
            float triggerDist = AttackRange * 0.9f;
            float hysteresisDist = AttackRange * 1.2f;

            if (distSqr <= triggerDist * triggerDist || (_isAttackMode && distSqr <= hysteresisDist * hysteresisDist))
            {
                SetAttackMode(true);
                FaceTarget();
                
                // Скроллер выключен — вручную синхронизируем Z врага с Z цели.
                // Используем Lerp чтобы движение было плавным, а не телепортом.
                if (_target != null)
                {
                    float targetZ = _target.transform.position.z;
                    float newZ = Mathf.Lerp(transform.position.z, targetZ, 15f * Time.deltaTime);
                    transform.position = new Vector3(transform.position.x, transform.position.y, newZ);
                }
            }
            else
            {
                SetAttackMode(false);
                FaceTarget(); // Смотрим на цель даже во время движения к ней
                UpdateMovement();
            }
        }

        ResolveOverlap();
        // Выталкивание от героев работает всегда (при атаке и движении к цели),
        // и ОТКЛЮЧАЕТСЯ только в чейз моде — чтобы враг мог пройти сквозь отряд назад.
        if (!_isChasing) ResolveHeroOverlap();

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

        if (_isChasing && !IsInAttackState()) return;

        // Ещё раз проверяем дистанцию — цель могла отойти пока анимация играла.
        float distSqr = SqrDistanceXZ(transform.position, GetTargetPoint());
        float rangeSqr = AttackRange * AttackRange;
        if (distSqr > rangeSqr)
        {
            Debug.Log($"[{gameObject.name}] OnAnimationHit: Missed! distSqr={distSqr} > rangeSqr={rangeSqr}");
            return;
        }

        bool killed = _target.TakeDamage(Damage);
        Debug.Log($"[{gameObject.name}] OnAnimationHit: Hit target! Damage={Damage}");
        
        if (killed)
        {
            _squad?.OnUnitDied(_target);
            EnemyTargetRegistry.Unregister(_target);
            _target = null;
        }
    }

    public void EndAttackAndChase()
    {
        if (_isChasing)
        {
            SetAttackMode(false);
        }
        else if (!_hasChased)
        {
            SetAttackMode(false);
            _isChasing = true;
            _hasChased = true;
            _chaseMinUntil = Time.time + 1.5f;

            // При переходе в Chase — сразу разворачиваем врага по направлению отхода.
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
        }
    }

    // ── Приватная логика ─────────────────────────────────────────────

    /// <summary>
    /// Математическое расталкивание (Best Practice). 
    /// Гарантирует минимальное расстояние между центрами врагов,
    /// чтобы их визуал не пересекался, независимо от размера коллайдеров.
    /// </summary>
    private void ResolveOverlap()
    {
        // Если я нахожусь в режиме просачивания, я полностью игнорирую расталкивание
        // с другими врагами, чтобы протиснуться сквозь них вперёд.
        if (_isPhasing) return;

        float minDistance = 0.5f; // Минимальная дистанция между врагами (чтобы модели не слипались)
        float minDistSqr = minDistance * minDistance;

        foreach (EnemyMeleeCombat other in _all)
        {
            if (other == this) continue;
            // Игнорируем других врагов, которые в данный момент просачиваются сквозь толпу
            if (other._isPhasing) continue;

            Vector3 diff = transform.position - other.transform.position;
            diff.y = 0;
            float sqrMag = diff.sqrMagnitude;

            if (sqrMag < minDistSqr && sqrMag > 0.0001f)
            {
                float dist = Mathf.Sqrt(sqrMag);
                float penetration = minDistance - dist;
                Vector3 pushDir = diff / dist;

                // Если я атакую, а другой — нет: я стою как стена (сдвиг 0), а другой сдвигается на все 100%.
                // Если оба атакуют или оба двигаются — сдвигаемся пополам (50/50).
                float weight = 0.5f;
                if (_isAttackMode && !other._isAttackMode)
                {
                    weight = 0f;
                }
                else if (!_isAttackMode && other._isAttackMode)
                {
                    weight = 1f;
                }

                transform.position += pushDir * (penetration * weight);
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
        
        // ВАЖНО: Если мы бежим в чейз мод (за спины героев), нам нужно пройти сквозь отряд!
        // Иначе герои будут бесконечно выталкивать врага обратно, и он застрянет.
        if (_isChasing) return;

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
        bool needsNewTarget = (_target == null || _target.IsDead || !_target.gameObject.activeSelf);
        
        // Если цель жива, но мы ещё не бьём её и не в чейзе — периодически переоцениваем цель.
        // Это позволяет переключаться на более близких героев, если нас растолкали или заблокировали.
        bool canReevaluate = !_isChasing && !_isAttackMode && Time.time >= _nextTargetReevaluateTime;

        if (needsNewTarget || canReevaluate)
        {
            _nextTargetReevaluateTime = Time.time + Random.Range(1f, 1.5f); // Немного рандомизируем интервал

            Unit oldTarget = _target;
            if (oldTarget != null)
            {
                EnemyTargetRegistry.Unregister(oldTarget);
            }

            Unit newTarget = EnemyTargetRegistry.GetLeastAttacked(transform.position, _squad);
            if (newTarget != null)
            {
                _target = newTarget;
                EnemyTargetRegistry.Register(_target);
                
                // Если цель реально сменилась — пересчитываем оффсет подхода
                if (newTarget != oldTarget)
                {
                    float maxOffset = Mathf.Clamp(AttackRange * 0.6f, 0.05f, 0.4f);
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float radius = Random.Range(maxOffset * 0.3f, maxOffset);
                    _targetOffset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                }
            }
            else
            {
                // Если новые цели не найдены, возвращаем старую (если она жива)
                if (oldTarget != null && !oldTarget.IsDead && oldTarget.gameObject.activeSelf)
                {
                    _target = oldTarget;
                    EnemyTargetRegistry.Register(_target);
                }
                else
                {
                    _target = null;
                }
            }
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
        if (_isAttackMode == attacking) return;
        _isAttackMode = attacking;
        
        Debug.Log($"[{gameObject.name}] SetAttackMode: {attacking}");

        // Во время атаки отключаем скроллер — иначе WorldScroller
        // утащит врага назад по -Z и он отвалится от героя.
        // Вместо этого Z позиция врага фиксируется к Z цели каждый кадр (см. Update).
        if (_scroller != null)
            _scroller.enabled = !attacking;

        if (_animator != null)
            _animator.SetBool("IsAttacking", attacking);
    }

    private bool IsInAttackState()
    {
        if (IsInAttackStateFlag) return true;

        if (_animator == null) return false;
        var info = _animator.GetCurrentAnimatorStateInfo(0);
        
        int hash = info.shortNameHash;
        int a = Animator.StringToHash("Attack");
        int a1 = Animator.StringToHash("Attack1");
        int a2 = Animator.StringToHash("Attack2");
        int a3 = Animator.StringToHash("Attack3");
        int s = Animator.StringToHash("slash01");
        int m = Animator.StringToHash("MeleeAttack");

        if (hash == a || hash == a1 || hash == a2 || hash == a3 || hash == s || hash == m)
            return true;
            
        // Также проверяем переход
        if (_animator.IsInTransition(0))
        {
            var nextInfo = _animator.GetNextAnimatorStateInfo(0);
            int nextHash = nextInfo.shortNameHash;
            if (nextHash == a || nextHash == a1 || nextHash == a2 || nextHash == a3 || nextHash == s || nextHash == m)
                return true;
        }

        return false;
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
            Vector3 realTargetPoint = GetTargetPoint();
            Vector3 toTarget = realTargetPoint - transform.position;
            float distXZ = Mathf.Sqrt(toTarget.x * toTarget.x + toTarget.z * toTarget.z);
            if (distXZ < TrackingRange && distXZ > 0.01f)
            {
                float dirX = toTarget.x / distXZ;
                float dirZ = toTarget.z / distXZ;

                trackingDeltaX = dirX * TrackingSpeed * personalMul * Time.deltaTime;

                // Компенсация WorldScroller только когда враг позади (dirZ > 0 — цель "впереди" по +Z).
                if (dirZ > 0f)
                {
                    float currentWorldSpeed = _scroller != null ? (WorldScroller.WorldSpeed * _scroller.SpeedMultiplier) : WorldScroller.WorldSpeed;
                    float scrollerComp = currentWorldSpeed * Time.deltaTime;
                    trackingDeltaZ = scrollerComp + (dirZ * TrackingSpeed * personalMul * Time.deltaTime);
                }
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
        // Смотрим строго на ЦЕНТР героя (не на точку с оффсетом),
        // чтобы взгляд всегда был зафиксирован на персонаже, которого бьём.
        Vector3 dir = _target.transform.position - transform.position;
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

        // Находим задний край отряда (минимальный Z среди живых активных героев)
        float minZ = _leader.position.z;
        if (_squad != null)
        {
            float tempMin = float.MaxValue;
            foreach (Unit u in _squad.AllUnits)
            {
                if (u == null || u.IsDead || !u.gameObject.activeSelf) continue;
                if (u.transform.position.z < tempMin)
                {
                    tempMin = u.transform.position.z;
                }
            }
            if (tempMin != float.MaxValue)
            {
                minZ = tempMin;
            }
        }

        // Chase-дистанция масштабируется по относительной скорости мира и берётся из настроек SO врага.
        float speedRatio = WorldScroller.WorldSpeed / WorldScroller.BaseWorldSpeed;
        float lineDistance = (_enemy.Data != null ? _enemy.Data.ChaseDistance : 0.8f) * speedRatio;

        if (_target != null && !_target.IsDead && Time.time >= _chaseMinUntil)
        {
            // Выходим из Chase и снова бьём ТОЛЬКО если отряд замедлился (скорость упала)
            // или задний край отряда подошёл к нам на дистанцию атаки.
            float distToBack = Mathf.Max(0f, minZ - transform.position.z);
            float triggerRange = AttackRange * 1.5f;

            bool worldSlowed = WorldScroller.WorldSpeed < WorldScroller.BaseWorldSpeed * 0.5f;
            if (worldSlowed || distToBack <= triggerRange)
            {
                _isChasing = false;
                _hasChased = false;
                _chaseOffsetX = 0f;
                _chaseOffsetZ = 0f;
                _targetOffset = Vector3.zero;
                // Возвращаем скроллер — теперь мир двигает врага вместе с героями (как при атаке и движении)
                if (_scroller != null) _scroller.enabled = true;
                return;
            }
        }

        // Чем медленнее мир — тем ближе враги подходят к отряду.
        float chaosX    = _enemy.Data != null ? _enemy.Data.ChaseChaosX   : 1.5f;

        // Враги выстраиваются в одну ровную линию позади отряда.
        Vector3 targetPos = new Vector3(
            _leader.position.x + _chaseOffsetX, // Разброс по X сохраняем
            transform.position.y,
            minZ - lineDistance                 // По Z строго на линии позади самого заднего героя
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

        // Определяем наклон от свайпа. 
        _prevLeaderX = _leader.position.x; // просто обновляем чтобы не ломать логику, если где-то юзается

        const float maxTurn = 40f;
        float turnAngle = 0f;
        
        if (dist > 0.05f)
        {
            if (dir.normalized.x > 0.1f)  turnAngle =  maxTurn;
            if (dir.normalized.x < -0.1f) turnAngle = -maxTurn;
        }

        Vector3 lookDir;
        if (dist > 0.5f)
        {
            // Если ещё бежим к точке чейза, смотрим по ходу движения
            lookDir = dir;
        }
        else
        {
            // В чейз моде смотрим на героя, которого били (как просил пользователь)
            Vector3 lookTarget = _target != null ? _target.transform.position : (_leader != null ? _leader.position : transform.position);
            lookDir = lookTarget - transform.position;
        }
        
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            // Компенсируем поворот модели (-190) так же, как в FaceTarget
            Quaternion baseRot = Quaternion.LookRotation(-lookDir);
            // Добавляем наклон
            Quaternion targetRot = baseRot * Quaternion.Euler(0f, turnAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, RotationSpeed * Time.deltaTime);
        }

        // Минимальное время в Chase — проверка перенесена в начало метода.
    }

    private static float SqrDistanceXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }
}
