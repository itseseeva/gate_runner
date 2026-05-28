using UnityEngine;

/// <summary>
/// Базовый класс ближней автоатаки.
/// Хранит AttackSpeed, считает cooldown, вызывает абстрактный CalculateDamage.
/// Наследники переопределяют только формулу урона.
/// </summary>
public abstract class MeleeAutoAttackBase : MonoBehaviour, IUnitAttack
{
    [Header("Базовые параметры")]
    [Tooltip("Атак в секунду. 1 = одна атака за секунду, 2 = две.")]
    [SerializeField] protected float _attackSpeed = 1.5f;

    [Tooltip("Дальность удара в метрах")]
    [SerializeField] protected float _range = 1.8f;

    [Tooltip("Базовый урон без модификаторов")]
    [SerializeField] protected int _baseDamage = 20;

    private float _lastFireTime = -999f;

    /// <summary>Обновляет время последнего выстрела. Вызывается наследниками.</summary>
    protected void UpdateCooldown() => _lastFireTime = Time.time;
    private Unit _unit;
    private Animator _animator;
    protected Animator Animator => _animator;
    protected Unit OwnerUnit => _unit;

    protected virtual void Awake()
    {
        _unit = GetComponent<Unit>();
        _animator = GetComponentInChildren<Animator>();
        if (_unit == null)
            Debug.LogError($"[MeleeAutoAttackBase] {gameObject.name}: нет компонента Unit!", this);
    }

    public float Range   => _range;
    public bool  IsReady => Time.time - _lastFireTime >= (1f / _attackSpeed);

    /// <summary>
    /// Главный метод — выполняет удар.
    /// Не наследники — их дело только формула урона.
    /// </summary>
    public virtual HitResult Hit(Enemy target)
    {
        if (target == null) return HitResult.Miss();

        // Расчёт урона делегируется наследнику
        int multiplier = _unit != null ? _unit.PowerMultiplier : 1;
        DamageCalculation calc = CalculateDamage(multiplier);

        // Применяем стихию через DamageCalculator
        ElementType element       = _unit != null ? _unit.Element : ElementType.None;
        StatusController status   = target.GetComponent<StatusController>();
        int finalDamage           = DamageCalculator.CalculateFinalDamage(calc.FinalDamage, element, status);

        bool died = target.TakeDamage(finalDamage);

        // Накладываем статус если враг жив и есть стихия
        if (!died && element != ElementType.None && status != null)
        {
            StatusEffectType statusToApply = DamageCalculator.GetStatusFromElement(element);
            status.ApplyStatus(statusToApply, finalDamage);
        }

        if (_animator != null)
            _animator.SetTrigger("Attack");

        // Обновляем cooldown
        UpdateCooldown();

        // Лайфстил: восстанавливаем HP юниту-владельцу
        int healed = 0;
        if (calc.LifestealAmount > 0)
        {
            // TODO: добавить ApplyHeal метод в Unit когда будем делать урон по юнитам
            healed = calc.LifestealAmount;
        }

        return new HitResult
        {
            Hit          = true,
            Killed       = died,
            WasCritical  = calc.WasCritical,
            DamageDealt  = finalDamage,
            HealingDone  = healed,
            IsAbility    = false,
        };
    }

    /// <summary>
    /// Наследники реализуют свою формулу.
    /// Возвращают финальный урон + флаг крита + сколько HP вернуть.
    /// </summary>
    protected abstract DamageCalculation CalculateDamage(int powerMultiplier);

    /// <summary>Промежуточная структура для расчёта урона.</summary>
    protected struct DamageCalculation
    {
        public int  FinalDamage;
        public bool WasCritical;
        public int  LifestealAmount;
    }
}
