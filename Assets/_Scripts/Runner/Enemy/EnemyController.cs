using UnityEngine;

/// <summary>
/// Стейт-машина врага. Держит текущий стейт, обновляет каждый кадр,
/// переключает при необходимости. Стейты сами решают когда переключаться —
/// через _ctrl.SwitchTo().
/// Балансные числа (AttackRange, Damage и т.д.) берутся из EnemyDefinitionSO.
/// </summary>
[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(WorldScroller))]
public class EnemyController : MonoBehaviour
{
    // Ссылки на компоненты — стейты берут их через свойства.
    private Enemy         _enemy;
    private Animator      _animator;
    private WorldScroller _scroller;

    private EnemyStateBase _currentState;

    // Публичный доступ для стейтов
    public Enemy         Enemy       => _enemy;
    public Animator      Animator    => _animator;
    public WorldScroller Scroller    => _scroller;

    // Балансные параметры — из EnemyDefinitionSO через Enemy.Data
    public float AttackRange            => _enemy.Data != null ? _enemy.Data.AttackRange            : 0.7f;
    public float AttackSpeed            => _enemy.Data != null ? _enemy.Data.AttackSpeed            : 1f;
    public int   Damage                 => _enemy.Data != null ? _enemy.Data.AttackDamage           : 10;
    public float SeparationRadius       => _enemy.Data != null ? _enemy.Data.SeparationRadius       : 0.5f;
    public float SeparationTargetRadius => _enemy.Data != null ? _enemy.Data.SeparationTargetRadius : 0.4f;

    public EnemyStateBase CurrentState => _currentState;

    private void Awake()
    {
        _enemy    = GetComponent<Enemy>();
        _scroller = GetComponent<WorldScroller>();
        _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
            Debug.LogWarning($"[EnemyController] {name}: нет Animator в дочках!", this);

        if (_enemy.Data == null)
            Debug.LogWarning($"[EnemyController] {name}: нет Data (EnemyDefinitionSO) в Enemy!", this);
    }

    private void OnEnable()
    {
        SwitchTo(new EnemyApproachState(this));
    }

    private void Update()
    {
        _currentState?.Tick();
    }

    /// <summary>Переключить активный стейт. Вызывается самими стейтами.</summary>
    public void SwitchTo(EnemyStateBase next)
    {
        _currentState?.Exit();
        _currentState = next;
        _currentState?.Enter();
    }
}
