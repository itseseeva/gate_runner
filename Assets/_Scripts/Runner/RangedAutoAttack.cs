using UnityEngine;

/// <summary>
/// Дальнобойная атака. Выпускает снаряд вперёд по Z.
/// Наследует MeleeAutoAttackBase для совместимости с IUnitAttack.
/// </summary>
public class RangedAutoAttack : MeleeAutoAttackBase
{
    [Header("Дальнобойная атака")]
    [Tooltip("Высота спавна снаряда относительно юнита")]
    [SerializeField] private float _spawnHeightOffset = 0.5f;

    protected override DamageCalculation CalculateDamage(int powerMultiplier)
    {
        return new DamageCalculation
        {
            FinalDamage     = _baseDamage * powerMultiplier,
            WasCritical     = false,
            LifestealAmount = 0,
        };
    }

    /// <summary>
    /// Переопределяем Hit — не бьём напрямую, а спавним снаряд.
    /// </summary>
    public override HitResult Hit(Enemy target)
    {
        if (!IsReady) return HitResult.Miss();

        // Расчёт урона делегируется наследнику
        int multiplier = OwnerUnit != null ? OwnerUnit.PowerMultiplier : 1;
        DamageCalculation calc = CalculateDamage(multiplier);

        // Спавним снаряд из пула
        Vector3 spawnPos = transform.position + Vector3.up * _spawnHeightOffset;
        Projectile p = ProjectilePool.Instance.Get(spawnPos, Quaternion.identity);
        // Получаем стихию юнита
        ElementType element = OwnerUnit != null ? OwnerUnit.Element : ElementType.None;
        p.Launch(calc.FinalDamage, _range, element);

        // Обновляем cooldown через reflection базового класса
        UpdateCooldown();

        return new HitResult
        {
            Hit         = true,
            Killed      = false, // не знаем — снаряд ещё летит
            WasCritical = false,
            DamageDealt = calc.FinalDamage,
            IsAbility   = false,
        };
    }
}
