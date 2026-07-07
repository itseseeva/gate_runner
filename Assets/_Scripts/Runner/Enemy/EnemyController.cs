using UnityEngine;

/// <summary>
/// Стейт-машина врага. Держит текущий стейт, обновляет каждый кадр,
/// переключает при необходимости. Стейты сами решают когда переключаться —
/// через _ctrl.SwitchTo().
/// </summary>
[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(WorldScroller))]
public class EnemyController : MonoBehaviour
{
    [Header("Боевые параметры")]
    [Tooltip("Дистанция на которой враг встаёт и начинает бить")]
    [SerializeField] private float _attackRange = 1.5f;

    [Tooltip("Атак в секунду")]
    [SerializeField] private float _attackSpeed = 1f;

    [Tooltip("Урон за один удар")]
    [SerializeField] private int _damage = 10;

    // Ссылки на компоненты — стейты берут их через свойства.
    private Enemy         _enemy;
    private Animator      _animator;
    private WorldScroller _scroller;

    private EnemyStateBase _currentState;

    // Публичный доступ для стейтов
    public Enemy         Enemy       => _enemy;
    public Animator      Animator    => _animator;
    public WorldScroller Scroller    => _scroller;
    public float         AttackRange => _attackRange;
    public float         AttackSpeed => _attackSpeed;
    public int           Damage      => _damage;
    public EnemyStateBase CurrentState => _currentState;

    private void Awake()
    {
        _enemy    = GetComponent<Enemy>();
        _scroller = GetComponent<WorldScroller>();
        _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
            Debug.LogWarning($"[EnemyController] {name}: нет Animator в дочках!", this);
    }

    private void OnEnable()
    {
        // При каждом взятии из пула — начинаем с Approach.
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
