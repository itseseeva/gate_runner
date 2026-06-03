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

    // Цель → сколько юнитов её атакуют. Максимум 2 на одну цель.
    private static readonly Dictionary<Enemy, int> _claimCount = new();
    private const int MAX_CLAIMS_PER_TARGET = 2;

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

            int claims = _claimCount.TryGetValue(e, out int c) ? c : 0;

            if (claims == 0)
                free.Add(e);
            else if (claims < MAX_CLAIMS_PER_TARGET)
                partial.Add(e);
            // claims >= 2 — пропускаем
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

        int current = _claimCount.TryGetValue(target, out int c) ? c : 0;
        if (current >= MAX_CLAIMS_PER_TARGET)
            return false; // на цели уже 2 — занято

        _claimCount[target] = current + 1;
        return true;
    }

    /// <summary>Освобождает цель.</summary>
    public void ReleaseTarget(Enemy target)
    {
        if (target == null) return;

        if (_claimCount.TryGetValue(target, out int c))
        {
            if (c <= 1) _claimCount.Remove(target);
            else        _claimCount[target] = c - 1;
        }
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
