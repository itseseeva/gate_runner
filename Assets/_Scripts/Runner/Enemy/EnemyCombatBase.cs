using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Базовый класс врага ближнего/дальнего боя. Состояния вынесены в EnemyState-наследников
/// (Approach / Attack / Chase) — симметрично FollowState/StrikeState у героев.
/// Этот класс — владелец данных и общей физики (расталкивание, поиск цели).
/// Урон летит через Animation Event (EnemyAnimationEventReceiver → OnAnimationHit).
/// </summary>
[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(WorldScroller))]
public abstract class EnemyCombatBase : MonoBehaviour
{
    // Константы движения — "чувство" врага, не баланс.
    // TODO: вынести в EnemyDefinitionSO при добавлении разных типов врагов.
    private const float TrackingRange   = 8f;
    private const float TrackingSpeed   = 3.5f;   // согласовано со снижением WorldSpeed
    private const float SeparationForce = 4f;
    private const float WobbleAmount    = 0.3f;
    private const float WobbleSpeed     = 2f;
    private const float LazyChance      = 0.3f;
    private const float LazyDuration    = 0.5f;
    private const float LazyCheckPeriod = 1f;
    private const float RotationSpeed   = 4f;

    // Все живые враги — для взаимного отталкивания и оптимизированной очистки.
    private static readonly List<EnemyCombatBase> _all = new();

    /// <summary>Все живые враги на сцене — для очистки без FindObjectsByType.</summary>
    public static IReadOnlyList<EnemyCombatBase> AllEnemies => _all;

    // Лимит врагов в чейзе — толпа сзади не должна расти бесконечно,
    // иначе десятки врагов держат слоты и жрут FPS.
    // TODO: вынести в конфиг, если понадобится баланс.
    private const int MaxChasingEnemies = 20;

    /// <summary>Сколько врагов сейчас в чейзе.</summary>
    public static int ChasingCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < _all.Count; i++)
                if (_all[i] != null && _all[i].IsChasing) count++;
            return count;
        }
    }

    /// <summary>Есть ли свободное место в толпе чейза.</summary>
    public static bool CanEnterChase => ChasingCount < MaxChasingEnemies;

    [Header("Слои")]
    [Tooltip("Слой, на котором находятся герои — для рывка/AoE.")]
    [SerializeField] private LayerMask _heroLayerMask;

    private Enemy           _enemy;
    private WorldScroller   _scroller;
    private Animator        _animator;
    private SquadController _squad;
    private CapsuleCollider _myCollider;

    private Unit    _target;
    private Vector3 _targetOffset;

    private float _personalSpeedFactor;
    private float _wobblePhase;
    private float _lazyUntil;
    private float _nextLazyCheck;
    private int   _chaseSlot = -1;   // индекс места в линии чейза
    private float _chaseChaosX;      // личное статичное смещение по X
    private float _chaseDriftPhase;  // фаза живого дрейфа — у каждого своя
    private float _chaseDriftSpeed;  // скорость дрейфа — тоже личная
    private bool  _hasChased;
    private Transform _leader;
    private float _nextTargetReevaluateTime;
    private bool  _isPhasing;
    private float _blockedTimer;

    // Личный "сбой темпа" в чейзе — враг иногда отваливается назад и подтягивается.
    private float _lagAmount;      // текущее отставание в метрах
    private float _lagTarget;      // к чему стремимся
    private float _nextLagCheck;   // когда следующий бросок

    // ─── Состояния ───────────────────────────────────────────────
    public EnemyStateMachine   Machine       { get; private set; }
    public EnemyApproachState  ApproachState { get; private set; }
    public EnemyAttackState    AttackState   { get; private set; }
    public EnemyRangedAttackState RangedAttackState { get; private set; }
    public EnemyRollState      RollState     { get; private set; }
    public EnemyChaseState     ChaseState    { get; private set; }
    public EnemyRetreatState   RetreatState  { get; private set; }

    // ─── Публичный доступ для состояний ──────────────────────────
    public Unit               Target             => _target;
    public Transform          Leader             => _leader;
    public EnemyDefinitionSO  Data               => _enemy != null ? _enemy.Data : null;
    public float              RotationSpeedValue => RotationSpeed;
    public bool               IsChasing          => Machine != null && Machine.Current == ChaseState;

    /// <summary>True, пока аниматор ещё проигрывает комбо атаки.</summary>
    public bool IsAttackAnimPlaying { get; set; }

    // Балансные числа из SO через Enemy.Data
    public  float AttackRange     => Data != null ? Data.AttackRange      : 0.7f;
    public  float AttackSpeed     => Data != null ? Data.AttackSpeed      : 1f;
    public  float AttackCooldown  => Data != null ? Data.AttackCooldown   : 1.5f;
    private float SeparationRadius => Data != null ? Data.SeparationRadius : 0.5f;

    public LayerMask HeroLayerMask => _heroLayerMask;

    /// <summary>Радиус капсулы врага — для точного детекта столкновения в рывке.</summary>
    public float CombatColliderRadius => _myCollider != null ? _myCollider.radius : 0.1f;

    /// <summary>Множитель скорости движения врага. Heavy медленнее.</summary>
    public virtual float MoveSpeedMultiplier => 1f;

    /// <summary>Дистанция, с которой враг переходит к атаке. Роллер переопределяет.</summary>
    public virtual float AttackTriggerDistance => AttackRange;

    /// <summary>Состояние, с которого враг начинает. Melee идёт к цели, ranged сразу стреляет.</summary>
    public virtual EnemyState StartState => ApproachState;

    /// <summary>В какое состояние идёт враг, дойдя до дистанции атаки.</summary>
    public virtual EnemyState AttackStateFor => AttackState;

    /// <summary>
    /// Прибивает ли враг свой Z к Z цели во время атаки.
    /// Melee — да, стоит вплотную. Ranged — нет, держит дистанцию.
    /// </summary>
    public virtual bool SticksToTargetZ => true;

    // Доступ для наследников
    protected Enemy           EnemyRef => _enemy;
    protected SquadController Squad    => _squad;

    private void Awake()
    {
        _enemy      = GetComponent<Enemy>();
        _scroller   = GetComponent<WorldScroller>();
        _animator   = GetComponentInChildren<Animator>();
        _myCollider = GetComponent<CapsuleCollider>();

        Machine           = new EnemyStateMachine();
        ApproachState     = new EnemyApproachState(this);
        AttackState       = new EnemyAttackState(this);
        RangedAttackState = new EnemyRangedAttackState(this);
        RollState         = new EnemyRollState(this);
        ChaseState        = new EnemyChaseState(this);
        RetreatState      = new EnemyRetreatState(this);
    }

    /// <summary>
    /// Тихо убирает врага в пул — без смерти, без цифр урона и событий.
    /// Для отступающих сверх лимита толпы: они уходят за камеру и исчезают невидимо.
    /// </summary>
    public void DespawnSelf()
    {
        if (_enemy != null) _enemy.ReturnToPool();
        else gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        _all.Add(this);
        _personalSpeedFactor = Random.Range(0.7f, 1.3f);
        _wobblePhase         = Random.Range(0f, Mathf.PI * 2f);
        _nextLazyCheck       = 0f;
        _lazyUntil           = 0f;
        _hasChased           = false;
        _isPhasing           = false;
        _blockedTimer        = 0f;

        // Статичный разброс — только по X. По Z хаоса быть не должно:
        // Z — это ChaseDistance, геймплейный параметр, его нельзя размывать.
        _chaseChaosX = Random.Range(-0.35f, 0.35f);

        // Живой дрейф — толпа дышит, а не стоит по стойке смирно.
        _chaseDriftPhase = Random.Range(0f, Mathf.PI * 2f);
        _chaseDriftSpeed = Random.Range(0.4f, 0.9f);

        _lagAmount    = 0f;
        _lagTarget    = 0f;
        _nextLagCheck = 0f;

        // Стартовое состояние. Machine создан в Awake, но OnEnable из пула
        // может прийти раньше первого Update — поэтому ставим тут.
        Machine?.ChangeState(StartState);
    }

    private void OnDisable()
    {
        _all.Remove(this);

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
        if (_enemy == null || _squad == null) return;
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying) return;

        if (_myCollider != null) _myCollider.enabled = !_isPhasing;

        UpdateTarget();

        Machine.Tick();

        if (Time.frameCount % 30 == 0 && _animator != null)
        {
            var info = _animator.GetCurrentAnimatorStateInfo(0);
            {}
        }

        ResolveOverlap();
        // Выталкивание героями отключено в Chase — иначе враг не пройдёт сквозь отряд назад.
        if (!IsChasing) ResolveHeroOverlap();

        // Граница дороги и фиксированная высота по Y.
        const float roadHalfWidth = 3.3f;
        Vector3 clamped = transform.position;
        clamped.x = Mathf.Clamp(clamped.x, -roadHalfWidth, roadHalfWidth);
        if (_enemy != null) clamped.y = _enemy.SpawnHeight;
        transform.position = clamped;
    }

    // ─── API для состояний ───────────────────────────────────────

    /// <summary>Включает/выключает WorldScroller. Вызывается только из Enter() состояний.</summary>
    public void SetScroller(bool on)
    {
        if (_scroller != null && _scroller.enabled != on)
            _scroller.enabled = on;
    }

    /// <summary>
    /// Локальное замедление врага относительно мира.
    /// 1.0 = едет вровень с отрядом, 0.7 = отстаёт на 30%.
    /// </summary>
    public void SetSpeedMultiplier(float m)
    {
        if (_scroller != null) _scroller.SpeedMultiplier = m;
    }

    /// <summary>Выставляет флаг IsAttacking аниматору. Только из Enter()/Exit() состояний.</summary>
    public void SetAnimatorAttacking(bool attacking)
    {
        if (_animator != null && _animator.GetBool("IsAttacking") != attacking)
            _animator.SetBool("IsAttacking", attacking);
    }

    /// <summary>Разовый проигрыш анимации атаки — триггер, не bool.</summary>
    public void TriggerAttackAnim()
    {
        if (_animator != null) _animator.SetTrigger("Attack");
    }

    public void TriggerRoll()    { if (_animator != null) _animator.SetTrigger("Roll"); }
    public void TriggerRollEnd() { if (_animator != null) _animator.SetTrigger("RollEnd"); }

    /// <summary>Убивает врага (рывок-камикадзе). Идёт через обычную смерть Enemy.</summary>
    public void KillSelf()
    {
        // Урон по себе не показываем — это техническая смерть камикадзе, не боевой удар.
        if (_enemy != null) _enemy.TakeDamage(999999, false);
    }

    /// <summary>Квадрат дистанции по XZ до точки подхода (центр цели + личный оффсет).</summary>
    public float DistToTargetPointSqr() => SqrDistanceXZ(transform.position, GetTargetPoint());

    /// <summary>Z заднего края отряда — минимальный Z среди живых героев.</summary>
    public float GetSquadBackZ()
    {
        float minZ = _leader != null ? _leader.position.z : transform.position.z;
        if (_squad == null) return minZ;

        float tempMin = float.MaxValue;
        foreach (Unit u in _squad.AllUnits)
        {
            if (u == null || u.IsDead || !u.gameObject.activeSelf) continue;
            if (u.transform.position.z < tempMin) tempMin = u.transform.position.z;
        }
        return tempMin != float.MaxValue ? tempMin : minZ;
    }

    // Параметры строя чейза.
    // TODO: вынести в EnemyDefinitionSO когда будешь балансить.
    private const float ChaseLineWidth   = 6f;    // ширина дороги
    private const float ChaseSlotSpacing = 0.8f;  // шаг между врагами в ряду
    private const float ChaseRowSpacing  = 0.9f;  // шаг между рядами в глубину

    /// <summary>
    /// Позиция врага в строю чейза: X внутри ряда, Z-смещение назад по номеру ряда.
    /// Слоты раздаются по индексу среди чейзящих — детерминированно,
    /// иначе враги сбиваются в кучу или растягиваются за пределы дороги.
    /// </summary>
    /// <param name="leaderX">X лидера отряда — строй центрируется по нему.</param>
    /// <returns>x — позиция в ряду, z — смещение НАЗАД от базовой линии (≥ 0).</returns>
    public Vector2 GetChaseSlot(float leaderX)
    {
        int myIndex = 0;
        int total = 0;
        foreach (EnemyCombatBase e in _all)
        {
            if (!e.IsChasing) continue;
            if (e == this) myIndex = total;
            total++;
        }

        // Сколько врагов влезает в один ряд по ширине дороги
        int perRow = Mathf.Max(1, Mathf.FloorToInt(ChaseLineWidth / ChaseSlotSpacing) + 1);

        int row       = myIndex / perRow;   // номер ряда: 0 — первый, 1 — второй...
        int posInRow  = myIndex % perRow;

        // Сколько человек реально в моём ряду (последний ряд может быть неполным)
        int countInRow = Mathf.Min(perRow, total - row * perRow);

        float x;
        if (countInRow <= 1)
        {
            x = leaderX;
        }
        else
        {
            // Центрируем ряд относительно лидера
            float rowWidth = ChaseSlotSpacing * (countInRow - 1);
            x = leaderX - rowWidth * 0.5f + ChaseSlotSpacing * posInRow;
        }

        // Шахматка: нечётные ряды смещены на полшага — так толпа выглядит
        // естественнее, чем колоннами строго друг за другом.
        if (row % 2 == 1) x += ChaseSlotSpacing * 0.5f;

        float zBack = row * ChaseRowSpacing;

        // Живой дрейф ТОЛЬКО по X — вбок. По Z нельзя: там ChaseDistance,
        // качание вперёд-назад ломает дистанцию до отряда.
        float driftX = Mathf.Sin(Time.time * _chaseDriftSpeed + _chaseDriftPhase) * 0.25f;

        return new Vector2(
            x + _chaseChaosX + driftX,
            zBack);   // Z строго по ряду, без хаоса
    }

    /// <summary>
    /// Личное отставание врага в чейзе. Раз в 2-4 секунды бросается кубик:
    /// с шансом 35% враг отваливается назад на 0.3-0.8м, иначе возвращается в строй.
    /// Даёт "дыхание" толпы — не все идут ровно, кто-то отстаёт и догоняет.
    /// TODO: вероятность и амплитуду вынести в EnemyDefinitionSO.
    /// </summary>
    public float GetChaseLag()
    {
        if (Time.time >= _nextLagCheck)
        {
            _nextLagCheck = Time.time + Random.Range(2f, 4f);
            _lagTarget = Random.value < 0.35f
                ? Random.Range(0.3f, 0.8f)   // отстаём
                : 0f;                         // возвращаемся в строй
        }

        // Плавно едем к целевому отставанию — рывков быть не должно.
        _lagAmount = Mathf.Lerp(_lagAmount, _lagTarget, 1.2f * Time.deltaTime);
        return _lagAmount;
    }

    /// <summary>Сбрасывает флаг chase, позволяя врагу снова уйти в погоню после следующей атаки.</summary>
    public void AllowChaseAgain()
    {
        _hasChased = false;
    }

    /// <summary>Сбрасывает параметры локации в чейзе при смене состояния.</summary>
    public void ResetChaseOffsets()
    {
        _chaseSlot = -1;
        _lagAmount = 0f;
        _lagTarget = 0f;
    }

    /// <summary>Выключает режим просачивания.</summary>
    public void StopPhasing()
    {
        _isPhasing = false;
        _blockedTimer = 0f;
    }

    /// <summary>
    /// Просачивание: если враг упёрся в другого и не может дойти до цели дольше 0.4с —
    /// временно отключаем его коллайдер, чтобы он протиснулся сквозь толпу.
    /// </summary>
    public void UpdatePhasing()
    {
        if (_target == null) { StopPhasing(); return; }

        float distSqr = DistToTargetPointSqr();
        float rangeSqr = AttackRange * AttackRange;

        bool blocked = false;
        const float checkDist = 0.48f;   // чуть меньше minDistance расталкивания (0.5м)
        foreach (EnemyCombatBase other in _all)
        {
            if (other == this || other.IsChasing) continue;
            if (SqrDistanceXZ(transform.position, other.transform.position) < checkDist * checkDist)
            {
                blocked = true;
                break;
            }
        }

        if (blocked && distSqr > rangeSqr)
        {
            _blockedTimer += Time.deltaTime;
            if (_blockedTimer > 0.4f) _isPhasing = true;
        }
        else
        {
            _blockedTimer = 0f;
        }

        // Дошли до радиуса атаки — просачивание больше не нужно.
        if (_isPhasing && distSqr <= rangeSqr * 1.1f) _isPhasing = false;
    }

    /// <summary>Поворот лицом к цели. Модель Skeleton_110 повёрнута на -190° — компенсируем.</summary>
    public void FaceTarget()
    {
        if (_target == null) return;
        Vector3 dir = _target.transform.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion targetRot = Quaternion.LookRotation(-dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, RotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Движение к отряду: tracking по X всегда, по Z только если враг позади
    /// (компенсация WorldScroller). Плюс wobble, lazy и separation — "живая толпа".
    /// </summary>
    public void UpdateMovement()
    {
        float speedMul = _scroller != null ? _scroller.SpeedMultiplier : 1f;

        // Lazy — иногда враг "ленится" на 0.5 сек
        if (Time.time >= _nextLazyCheck)
        {
            _nextLazyCheck = Time.time + LazyCheckPeriod;
            if (Random.value < LazyChance) _lazyUntil = Time.time + LazyDuration;
        }

        float personalMul = _personalSpeedFactor * MoveSpeedMultiplier;

        // Lazy штраф ТОЛЬКО когда близко к цели — не когда ещё догоняем.
        bool isLazyClose = _target != null &&
            SqrDistanceXZ(transform.position, _target.transform.position) < 4f;
        if (Time.time < _lazyUntil && isLazyClose) personalMul *= 0.3f;

        float wobble = Mathf.Sin(Time.time * WobbleSpeed + _wobblePhase)
                     * WobbleAmount * speedMul * Time.deltaTime;

        float trackingDeltaX = 0f;
        float trackingDeltaZ = 0f;

        if (_target != null)
        {
            Vector3 tp = GetTargetPoint();
            float dirX = tp.x - transform.position.x;

            // Плавное следование по X — враг тянется к линии героя с отставанием,
            // а не копирует движение один-в-один. Коэффициент < 1 = ленивее.
            float followFactor = 0.1f;   // 1 = мгновенно как раньше, меньше = ленивее
            trackingDeltaX = dirX * followFactor * TrackingSpeed * personalMul * Time.deltaTime;

            // Tracking по Z — только вблизи и только если враг позади.
            float distToTargetSqr = SqrDistanceXZ(transform.position, tp);
            if (distToTargetSqr < TrackingRange * TrackingRange)
            {
                float dirZ = tp.z - transform.position.z;
                if (dirZ > 0f)
                {
                    float currentWorldSpeed = _scroller != null
                        ? WorldScroller.WorldSpeed * _scroller.SpeedMultiplier
                        : WorldScroller.WorldSpeed;
                    float scrollerComp = currentWorldSpeed * Time.deltaTime;
                    trackingDeltaZ = scrollerComp + (dirZ * TrackingSpeed * personalMul * Time.deltaTime);
                }
            }
        }

        // Отталкивание от других врагов — только пока далеко от цели,
        // иначе застрянем в куче у самого героя.
        float separationDeltaX = 0f;
        float separationDeltaZ = 0f;

        bool closeToTarget = false;
        if (_target != null)
        {
            float noSepRange = AttackRange * 2f;
            closeToTarget = DistToTargetPointSqr() < noSepRange * noSepRange;
        }

        if (!closeToTarget)
        {
            float sepRadius = SeparationRadius;
            float sepRadSqr = sepRadius * sepRadius;
            Vector3 myPos = transform.position;

            foreach (EnemyCombatBase other in _all)
            {
                if (other == this) continue;
                Vector3 d = myPos - other.transform.position;
                float dSqr = d.x * d.x + d.z * d.z;
                if (dSqr > sepRadSqr) continue;

                float dist = Mathf.Sqrt(dSqr);
                if (dist < 0.01f) dist = 0.01f;

                float strength = (1f - dist / sepRadius) * SeparationForce
                               * personalMul * speedMul * Time.deltaTime;
                separationDeltaX += (d.x / dist) * strength;
                separationDeltaZ += (d.z / dist) * strength;
            }
        }

        Vector3 pos = transform.position;
        pos.x += trackingDeltaX + separationDeltaX + wobble;
        pos.z += trackingDeltaZ + separationDeltaZ;
        transform.position = pos;
    }

    // ─── Animation Events ────────────────────────────────────────

    /// <summary>
    /// Вызывается через Animation Event на клипе атаки.
    /// EnemyAnimationEventReceiver пробрасывает вызов сюда.
    /// </summary>
    public virtual void OnAnimationHit() { }

    /// <summary>Пускает снаряд. Реализуется в EnemyRangedCombat.</summary>
    public virtual void FireProjectile() { }

    /// <summary>Убирает цель после её смерти — вызывается наследниками.</summary>
    protected void ClearTarget()
    {
        if (_target != null)
        {
            _squad?.OnUnitDied(_target);
            EnemyTargetRegistry.Unregister(_target);
            _target = null;
        }
    }

    /// <summary>
    /// Вызывается из EnemyChaseAfterAttackBehaviour в конце клипа атаки.
    /// Единственная точка перехода Attack → Chase.
    /// </summary>
    public void EndAttackAndChase()
    {
        if (Machine.Current != AttackState) return;
        if (_hasChased) return;

        _hasChased = true;

        // Мест в толпе чейза нет — уходим в отступление и исчезаем за камерой.
        Machine.ChangeState(CanEnterChase ? ChaseState : RetreatState);
    }

    // ─── Общая физика ────────────────────────────────────────────

    /// <summary>
    /// Математическое расталкивание врагов между собой.
    /// Гарантирует минимальную дистанцию между центрами независимо от коллайдеров.
    /// </summary>
    private void ResolveOverlap()
    {
        if (_isPhasing) return;

        const float minDistance = 0.5f;
        const float minDistSqr = minDistance * minDistance;

        bool iAttack = Machine.Current == AttackState;
        bool iChase  = IsChasing;

        foreach (EnemyCombatBase other in _all)
        {
            if (other == this || other._isPhasing) continue;

            Vector3 diff = transform.position - other.transform.position;
            diff.y = 0;
            float sqrMag = diff.sqrMagnitude;

            if (sqrMag >= minDistSqr || sqrMag <= 0.0001f) continue;

            float dist = Mathf.Sqrt(sqrMag);
            float penetration = minDistance - dist;
            Vector3 pushDir = diff / dist;

            bool otherAttack = other.Machine != null && other.Machine.Current == other.AttackState;

            float weight = 0.5f;
            if (iAttack && !otherAttack)      weight = 0f;
            else if (!iAttack && otherAttack) weight = 1f;

            // В чейзе расталкиваемся мягче — слоты уже разводят врагов,
            // полная сила боролась бы со строем и сбивала с линии.
            if (iChase) weight *= 0.35f;

            transform.position += pushDir * (penetration * weight);
        }
    }

    /// <summary>
    /// Разводит капсулу врага и капсулы героев через Physics.ComputePenetration.
    /// Unity сам считает точное перекрытие и выдаёт направление/глубину.
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
                transform.position += dir * dist;
            }
        }
    }

    private void UpdateTarget()
    {
        bool needsNewTarget = _target == null || _target.IsDead || !_target.gameObject.activeSelf;

        // Переоцениваем цель только в Approach — чтобы не менять её посреди удара или отхода.
        bool canReevaluate = Machine.Current == ApproachState
                          && Time.time >= _nextTargetReevaluateTime;

        if (!needsNewTarget && !canReevaluate) return;

        _nextTargetReevaluateTime = Time.time + Random.Range(1f, 1.5f);

        Unit oldTarget = _target;
        if (oldTarget != null) EnemyTargetRegistry.Unregister(oldTarget);

        Unit newTarget = EnemyTargetRegistry.GetLeastAttacked(transform.position, _squad);

        if (newTarget != null)
        {
            _target = newTarget;
            EnemyTargetRegistry.Register(_target);

            if (newTarget != oldTarget)
            {
                float maxOffset = Mathf.Clamp(AttackRange * 0.6f, 0.05f, 0.4f);
                float angle  = Random.Range(0f, Mathf.PI * 2f);
                float radius = Random.Range(maxOffset * 0.3f, maxOffset);
                _targetOffset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            }
        }
        else if (oldTarget != null && !oldTarget.IsDead && oldTarget.gameObject.activeSelf)
        {
            _target = oldTarget;
            EnemyTargetRegistry.Register(_target);
        }
        else
        {
            _target = null;
        }
    }

    private Vector3 GetTargetPoint()
    {
        if (_target == null) return transform.position;
        return _target.transform.position + _targetOffset;
    }

    private static float SqrDistanceXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }
}
