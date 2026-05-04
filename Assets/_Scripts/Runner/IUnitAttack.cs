using UnityEngine;

/// <summary>
/// Контракт любой атаки юнита. Реализуется компонентами на prefab.
/// Один и тот же интерфейс для авто-атак и способностей — различаются только
/// числами кулдауна и реализацией Hit().
/// </summary>
public interface IUnitAttack
{
    /// <summary>Дальность с которой можно бить. Метры.</summary>
    float Range { get; }

    /// <summary>Готова ли атака (прошёл ли cooldown).</summary>
    bool IsReady { get; }

    /// <summary>
    /// Наносит удар по цели. Сам обновит cooldown.
    /// Возвращает структуру с полной информацией о результате.
    /// </summary>
    HitResult Hit(Enemy target);
}
