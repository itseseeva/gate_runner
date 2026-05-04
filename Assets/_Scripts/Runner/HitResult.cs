/// <summary>
/// Результат одного удара по цели. Содержит всю информацию о том что произошло.
/// Используется для UI (показ "CRIT!"), статистики и логики StrikeState.
/// </summary>
public struct HitResult
{
    public bool Hit;            // Попало ли (false = промах, например мана не позволила)
    public bool Killed;         // Умерла ли цель от этого удара
    public bool WasCritical;    // Был ли это критический удар
    public int  DamageDealt;    // Сколько урона нанесли
    public int  HealingDone;    // Сколько HP восстановили атакующему (lifesteal)
    public bool IsAbility;      // Это способность (true) или автоатака (false)

    /// <summary>Создаёт результат "промах" — удар не состоялся.</summary>
    public static HitResult Miss() => new HitResult { Hit = false };
}
