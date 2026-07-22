using UnityEngine;

/// <summary>
/// Всё, что может получить урон от снаряда: враги, герои, в будущем — ворота и препятствия.
/// Снаряд не должен знать, в кого летит — он бьёт по маске слоёв и работает через этот интерфейс.
/// </summary>
public interface IDamageable
{
    /// <summary>Мёртв ли объект — снаряд не бьёт трупы.</summary>
    bool IsDead { get; }

    /// <summary>Transform цели — для спавна эффектов и AoE-поиска.</summary>
    Transform transform { get; }

    /// <summary>
    /// Наносит урон. Возвращает true, если объект погиб от этого удара.
    /// </summary>
    /// <param name="amount">Финальный урон (уже с учётом стихии и статусов)</param>
    /// <param name="showDamageNumber">Показывать ли цифру урона</param>
    /// <param name="numberType">Стиль цифры</param>
    bool TakeDamage(int amount, bool showDamageNumber = true,
                    DamageNumberType numberType = DamageNumberType.Normal);
}
