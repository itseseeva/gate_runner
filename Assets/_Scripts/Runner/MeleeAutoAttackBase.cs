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

    public float Range   => _range;
    public bool  IsReady => Time.time - _lastFireTime >= (1f / _attackSpeed);

    /// <summary>
    /// Главный метод — выполняет удар.
    /// Не наследники — их дело только формула урона.
    /// </summary>
    public HitResult Hit(Enemy target)
    {
        if (target == null) return HitResult.Miss();

        // Расчёт урона делегируется наследнику
        DamageCalculation calc = CalculateDamage();

        // Наносим урон
        bool died = target.TakeDamage(calc.FinalDamage);

        // Обновляем cooldown
        _lastFireTime = Time.time;

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
            DamageDealt  = calc.FinalDamage,
            HealingDone  = healed,
            IsAbility    = false,
        };
    }

    /// <summary>
    /// Наследники реализуют свою формулу.
    /// Возвращают финальный урон + флаг крита + сколько HP вернуть.
    /// </summary>
    protected abstract DamageCalculation CalculateDamage();

    /// <summary>Промежуточная структура для расчёта урона.</summary>
    protected struct DamageCalculation
    {
        public int  FinalDamage;
        public bool WasCritical;
        public int  LifestealAmount;
    }
}
