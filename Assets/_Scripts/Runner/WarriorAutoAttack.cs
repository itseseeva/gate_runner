using UnityEngine;

/// <summary>
/// Автоатака воина — простой удар без эффектов.
/// </summary>
public class WarriorAutoAttack : MeleeAutoAttackBase
{
    protected override DamageCalculation CalculateDamage()
    {
        return new DamageCalculation
        {
            FinalDamage     = _baseDamage,
            WasCritical     = false,
            LifestealAmount = 0,
        };
    }
}
