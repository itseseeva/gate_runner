using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Контроллер воина-легендарки. Поведение: пробегает через волны врагов,
/// нанося один удар каждому, не останавливается. Камера всегда успевает.
/// </summary>
[RequireComponent(typeof(UnitStateMachine))]
[RequireComponent(typeof(Unit))]
public class MeleeUnitController : MonoBehaviour
{
    [Header("Дистанции")]
    [SerializeField] private float _detectionRange   = 5f;
    [SerializeField] private float _attackRange      = 1.8f;
    [SerializeField] private float _returnDistance   = 0.5f;

    [Header("Скорости")]
    [SerializeField] private float _chaseSpeed       = 8f;
    [SerializeField] private float _driftSpeedRatio  = 0.3f;

    [Header("Возврат в строй")]
    [Tooltip("Время плавного возврата в формацию после боя (секунды)")]
    [SerializeField] private float _rejoinDuration = 0.4f;

    private float   _rejoinTimer;
    private bool    _isRejoining;
    private Vector3 _rejoinStartPos;

    [Header("Атака")]
    [SerializeField] private float _hitLingerTime = 0.3f;
    public float HitLingerTime => _hitLingerTime;

    [Tooltip("Компонент с IUnitAttack")]
    [SerializeField] private MonoBehaviour _autoAttackComponent;

    private IUnitAttack _autoAttack;
    private Animator    _animator;
    private bool        _isPlayingRejoin = false;

    // Общий счётчик для воинов/ассассинов (могут наваливаться по 2)
    private static readonly Dictionary<Enemy, int> _claimCount = new();
    private const int MAX_CLAIMS_PER_TARGET = 2;

    // Отдельный счётчик для танков — 1 танк на врага
    private static readonly Dictionary<Enemy, int> _tankClaimCount = new();
    private const int MAX_TANK_CLAIMS_PER_TARGET = 1;

    // ─── Свойства ────────────────────────────────────────────────
    public Transform Leader          { get; private set; }
    public Vector3   FormationOffset { get; set; }
    public bool      IsInFormation   { get; set; } = true;
    public IUnitAttack AutoAttack    => _autoAttack;
    public float DetectionRange      => _detectionRange;
    public float AttackRange         => _attackRange;
    public float ReturnDistance      => _returnDistance;
    public float ChaseSpeed          => _chaseSpeed;
    public float DriftSpeed          => _chaseSpeed * _driftSpeedRatio;

    private Unit _ownerUnit;
    private bool IsTank => _ownerUnit != null && _ownerUnit.HeroType == HeroType.Tank;

    private UnitStateMachine _stateMachine;

    // ─── Состояния ───────────────────────────────────────────────
    public FollowState         FollowState         { get; private set; }
    public StrikeState         StrikeState         { get; private set; }
    public AssassinStrikeState AssassinStrikeState { get; private set; }

    /// <summary>Инициализация — вызывается из SquadController.</summary>
    public void Initialize(Transform leader, Vector3 formationOffset)
    {
        Leader          = leader;
        FormationOffset = formationOffset;

        _stateMachine = GetComponent<UnitStateMachine>();
        _animator     = GetComponentInChildren<Animator>();
        _ownerUnit    = GetComponent<Unit>();

        _autoAttack = _autoAttackComponent as IUnitAttack;
        if (_autoAttack == null)
            Debug.LogError($"[MeleeUnitController] {gameObject.name}: _autoAttackComponent должен реализовывать IUnitAttack!", this);

        FollowState = new FollowState(this);
        StrikeState = new StrikeState(this);

        // Если на объекте есть AssassinAutoAttack — создаём его состояние
        var assassinAttack = GetComponent<AssassinAutoAttack>();
        if (assassinAttack != null)
        {
            AssassinStrikeState = new AssassinStrikeState(this, assassinAttack);
            assassinAttack.SetStrikeState(AssassinStrikeState); // ← связка для Animation Events
        }

        _stateMachine.ChangeState(FollowState);
    }

    public float RejoinDuration => _rejoinDuration;
    public bool  IsRejoining    => _isRejoining;

    public void StartRejoin()
    {
        _rejoinStartPos = transform.position;
        _rejoinTimer    = 0f;
        _isRejoining    = true;
    }

    public void UpdateRejoin()
    {
        if (!_isRejoining) return;

        _rejoinTimer += Time.deltaTime;
        float t = _rejoinTimer / _rejoinDuration;

        if (t >= 1f)
        {
            _isRejoining = false;
            if (_animator != null) _animator.applyRootMotion = false;
            transform.position = Leader.position + FormationOffset;
            return;
        }

        Vector3 targetPos = Leader.position + FormationOffset;
        transform.position = Vector3.Lerp(transform.position, targetPos,
            _rejoinDuration > 0 ? Time.deltaTime / (_rejoinDuration * (1f - t + 0.01f)) : 1f);
    }

    public void ChangeState(IUnitState state) => _stateMachine.ChangeState(state);

    public Enemy FindRandomEnemyInRange(float range, float minZ = float.MinValue)
    {
        Enemy[] all = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        List<Enemy> free    = new(); // совсем свободные (0 атакующих)
        List<Enemy> partial = new(); // с 1 атакующим (резерв)

        foreach (Enemy e in all)
        {
            if (e == null || !e.gameObject.activeSelf) continue;
            if (e.transform.position.z < minZ) continue;

            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d > range) continue;

            if (IsTank)
            {
                // Танк: смотрит танковый счётчик — 1 танк на врага
                int tankClaims = _tankClaimCount.TryGetValue(e, out int tc) ? tc : 0;
                if (tankClaims < MAX_TANK_CLAIMS_PER_TARGET)
                    free.Add(e);
                // иначе враг уже занят танком — пропускаем
            }
            else
            {
                // Воин/ассассин: общий счётчик, до 2 на врага
                int claims = _claimCount.TryGetValue(e, out int c) ? c : 0;
                if (claims == 0)
                    free.Add(e);
                else if (claims < MAX_CLAIMS_PER_TARGET)
                    partial.Add(e);
            }
        }

        // Приоритет — свободные цели
        List<Enemy> pool = free.Count > 0 ? free : partial;

        if (pool.Count == 0)
        {
            DiagLogger.RecordFindEmpty();
            return null;
        }

        DiagLogger.RecordFindHit();
        return pool[Random.Range(0, pool.Count)];
    }

    /// <summary>Резервирует цель. Возвращает false если на цели уже максимум юнитов.</summary>
    public bool ClaimTarget(Enemy target)
    {
        if (target == null) return false;

        if (IsTank)
        {
            int cur = _tankClaimCount.TryGetValue(target, out int tc) ? tc : 0;
            if (cur >= MAX_TANK_CLAIMS_PER_TARGET) return false;
            _tankClaimCount[target] = cur + 1;
            return true;
        }
        else
        {
            int current = _claimCount.TryGetValue(target, out int c) ? c : 0;
            if (current >= MAX_CLAIMS_PER_TARGET) return false;
            _claimCount[target] = current + 1;
            return true;
        }
    }

    /// <summary>Освобождает цель.</summary>
    public void ReleaseTarget(Enemy target)
    {
        if (target == null) return;

        if (IsTank)
        {
            if (_tankClaimCount.TryGetValue(target, out int tc))
            {
                if (tc <= 1) _tankClaimCount.Remove(target);
                else         _tankClaimCount[target] = tc - 1;
            }
        }
        else
        {
            if (_claimCount.TryGetValue(target, out int c))
            {
                if (c <= 1) _claimCount.Remove(target);
                else        _claimCount[target] = c - 1;
            }
        }
    }

    private Enemy _myClaimedTarget; // какого врага держит этот танк

    /// <summary>
    /// Для AutoAttacker: можно ли танку бить эту цель.
    /// Танк бьёт только если успел заклеймить врага (1 танк на врага).
    /// Не-танки бьют всегда (их ограничивает обычный claim).
    /// </summary>
    public bool CanAutoAttack(Enemy target)
    {
        if (!IsTank) return true;          // воины/ассассины — без ограничения здесь
        if (target == null) return false;

        // Уже клеймили этого врага этим танком? Тогда можно.
        if (_myClaimedTarget == target) return true;

        // Пытаемся заклеймить. Получилось — цель наша, бьём.
        if (ClaimTarget(target))
        {
            // Освобождаем прошлую цель, если была другая
            if (_myClaimedTarget != null && _myClaimedTarget != target)
                ReleaseTarget(_myClaimedTarget);
            _myClaimedTarget = target;
            return true;
        }

        return false; // врага уже держит другой танк — не бьём
    }

    private void OnDisable() { }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _detectionRange);

        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }

    /// <summary>Обычный бег в формации.</summary>
    public void PlayRun()
    {
        _isPlayingRejoin = false;
        if (_animator != null)
        {
            _animator.ResetTrigger("Attack");
            _animator.Play("Run");
        }
    }

    /// <summary>Рывок к врагу.</summary>
    public void PlayAttackRun()
    {
        _isPlayingRejoin = false;
        if (_animator != null) _animator.Play("AttackRun");
    }

    /// <summary>Возврат в строй.</summary>
    public void PlayRejoin()
    {
        if (_isPlayingRejoin) return;
        _isPlayingRejoin = true;
        if (_animator != null) _animator.Play("Run");
    }

    public AnimatorStateInfo GetAnimatorState() => 
        _animator != null ? _animator.GetCurrentAnimatorStateInfo(0) : default;
}
