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
    [SerializeField] private float _detectionRange   = 5f;   // радиус видимости врага
    [SerializeField] private float _attackRange      = 1.8f; // дистанция удара
    [SerializeField] private float _returnDistance   = 0.5f; // расстояние до формации для возврата в Follow

    [Header("Скорости")]
    [SerializeField] private float _chaseSpeed       = 8f;   // скорость рывка к врагу
    [SerializeField] private float _driftSpeedRatio  = 0.3f; // доля от ChaseSpeed во время Drift

    [Header("Возврат в строй")]
    [Tooltip("Время плавного возврата в формацию после боя (секунды)")]
    [SerializeField] private float _rejoinDuration = 0.4f;

    private float _rejoinTimer;
    private bool  _isRejoining;
    private Vector3 _rejoinStartPos;

    [Header("Атака")]
    [Tooltip("Компонент с IUnitAttack — вешается на prefab отдельно (WarriorAutoAttack, AssassinAutoAttack)")]
    [SerializeField] private MonoBehaviour _autoAttackComponent;

    private IUnitAttack _autoAttack;

    // ─── Резерв целей между воинами ──────────────────────────────
    // Статический список — общий для всех воинов в сцене.
    // Когда воин выбрал цель — добавляет сюда. Когда ударил — убирает.
    // Другие воины пропускают забронированных при выборе.
    private static readonly HashSet<Enemy> _claimedTargets = new();

    // ─── Свойства ────────────────────────────────────────────────
    public Transform Leader          { get; private set; }
    public Vector3   FormationOffset { get; set; }

    /// <summary>True когда воин в Follow — SquadController может им двигать.</summary>
    public bool IsInFormation { get; set; } = true;

    public IUnitAttack AutoAttack => _autoAttack;

    public float DetectionRange => _detectionRange;
    public float AttackRange    => _attackRange;
    public float ReturnDistance => _returnDistance;
    public float ChaseSpeed     => _chaseSpeed;
    public float DriftSpeed     => _chaseSpeed * _driftSpeedRatio;

    private UnitStateMachine _stateMachine;

    // ─── Состояния ───────────────────────────────────────────────
    public FollowState FollowState { get; private set; }
    public StrikeState StrikeState { get; private set; }

    /// <summary>Инициализация — вызывается из SquadController.</summary>
    public void Initialize(Transform leader, Vector3 formationOffset)
    {
        Leader = leader;
        FormationOffset = formationOffset;

        _stateMachine = GetComponent<UnitStateMachine>();

        // Получаем компонент атаки через интерфейс
        _autoAttack = _autoAttackComponent as IUnitAttack;
        if (_autoAttack == null)
        {
            Debug.LogError($"[MeleeUnitController] {gameObject.name}: _autoAttackComponent должен реализовывать IUnitAttack!", this);
        }

        FollowState = new FollowState(this);
        StrikeState = new StrikeState(this);

        _stateMachine.ChangeState(FollowState);
    }

    public float RejoinDuration => _rejoinDuration;
    public bool  IsRejoining    => _isRejoining;

    /// <summary>Запускает плавный возврат в формацию.</summary>
    public void StartRejoin()
    {
        _rejoinStartPos = transform.position;
        _rejoinTimer    = 0f;
        _isRejoining    = true;
    }

    /// <summary>Обновляет плавный возврат. Вызывается из FollowState каждый кадр.</summary>
    public void UpdateRejoin()
    {
        if (!_isRejoining) return;

        _rejoinTimer += Time.deltaTime;
        float t = _rejoinTimer / _rejoinDuration;

        if (t >= 1f)
        {
            _isRejoining = false;
            return;
        }

        Vector3 formationPos = Leader.position + FormationOffset;
        transform.position = Vector3.Lerp(_rejoinStartPos, formationPos, t);
    }

    public void ChangeState(IUnitState state) => _stateMachine.ChangeState(state);

    /// <summary>
    /// Возвращает рандомного НЕ забронированного врага в радиусе.
    /// null если никого подходящего нет.
    /// </summary>
    public Enemy FindRandomEnemyInRange(float range, float minZ = float.MinValue)
    {
        Enemy[] all = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        List<Enemy> candidates = new();
        foreach (Enemy e in all)
        {
            if (e == null || !e.gameObject.activeSelf) continue;
            if (_claimedTargets.Contains(e)) continue;
            if (e.transform.position.z < minZ) continue;  // ← фильтр по Z
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d <= range) candidates.Add(e);
        }

        if (candidates.Count == 0)
        {
            DiagLogger.RecordFindEmpty();
            return null;
        }

        DiagLogger.RecordFindHit();
        return candidates[Random.Range(0, candidates.Count)];
    }

    /// <summary>Резервирует цель — другие воины её не выберут.</summary>
    public void ClaimTarget(Enemy target)
    {
        if (target != null)
        {
            bool added = _claimedTargets.Add(target);
            if (!added) DiagLogger.RecordClaimCollision();
        }
    }

    /// <summary>Освобождает цель — снова доступна для выбора.</summary>
    public void ReleaseTarget(Enemy target)
    {
        if (target != null)
        {
            _claimedTargets.Remove(target);
        }
    }

    // Не чистим тут! Если воин деактивируется во время Strike,
    // его текущая цель освобождается через StrikeState.Exit()
    private void OnDisable()
    {
        // намеренно пусто
    }

    // ─── Debug Gizmos в редакторе ────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _detectionRange);

        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
}
