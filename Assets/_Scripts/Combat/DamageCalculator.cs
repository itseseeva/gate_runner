using UnityEngine;

/// <summary>
/// Централизованный расчёт урона по врагу.
/// Учитывает стихию атакующего и статусы на враге.
///
/// Правила:
/// 1. Базовый урон умножается на ElementBonus (если стихия не None)
/// 2. Если враг Shocked — урон умножается ещё на ShockMultiplier
/// 3. После расчёта — атака может наложить статус соответствующий стихии
/// </summary>
public static class DamageCalculator
{
    // ─── Балансные числа ─────────────────────────────────────────
    // TODO: вынести в RemoteConfig когда подключим UGS

    /// <summary>Бонус-урон при стихийной атаке (Fire/Ice/Lightning).</summary>
    public const float ELEMENT_DAMAGE_BONUS = 1.5f;  // +50%

    /// <summary>Множитель урона по враге со статусом Shocked.</summary>
    public const float SHOCK_DAMAGE_MULTIPLIER = 1.4f;  // +40%

    /// <summary>Длительность всех статусов в секундах.</summary>
    public const float STATUS_DURATION = 3f;

    /// <summary>Урон Burning за тик (% от урона удара который наложил поджог).</summary>
    public const float BURN_DAMAGE_PERCENT = 0.3f;  // 30% от удара

    /// <summary>Сколько раз в секунду тикает Burning.</summary>
    public const float BURN_TICKS_PER_SECOND = 1f;

    /// <summary>Множитель скорости Frozen-врага (1.0 = нормально, 0.2 = в 5 раз медленнее).</summary>
    public const float FROZEN_SPEED_MULTIPLIER = 0.2f;

    // ─── Главный метод расчёта ───────────────────────────────────

    /// <summary>
    /// Рассчитывает финальный урон с учётом стихии и статусов на враге.
    /// </summary>
    /// <param name="baseDamage">Базовый урон удара (уже с PowerMultiplier юнита)</param>
    /// <param name="attackerElement">Стихия атакующего</param>
    /// <param name="enemyStatus">Контроллер статусов врага (может быть null)</param>
    /// <returns>Финальный урон для применения</returns>
    public static int CalculateFinalDamage(
        int baseDamage,
        ElementType attackerElement,
        StatusController enemyStatus)
    {
        float damage = baseDamage;

        // 1. Бонус от стихии атакующего
        if (attackerElement != ElementType.None)
            damage *= ELEMENT_DAMAGE_BONUS;

        // 2. Враг Shocked? — ещё бонус
        if (enemyStatus != null && enemyStatus.HasStatus(StatusEffectType.Shocked))
            damage *= SHOCK_DAMAGE_MULTIPLIER;

        return Mathf.RoundToInt(damage);
    }

    /// <summary>
    /// Возвращает какой статус накладывает стихия.
    /// Fire→Burning, Ice→Frozen, Lightning→Shocked, None→None.
    /// </summary>
    public static StatusEffectType GetStatusFromElement(ElementType element)
    {
        return element switch
        {
            ElementType.Fire      => StatusEffectType.Burning,
            ElementType.Ice       => StatusEffectType.Frozen,
            ElementType.Lightning => StatusEffectType.Shocked,
            _                     => StatusEffectType.None,
        };
    }
}
