using UnityEngine;

/// <summary>
/// Автоатака воина — простой удар без эффектов.
/// </summary>
public class WarriorAutoAttack : MeleeAutoAttackBase
{
    protected override DamageCalculation CalculateDamage(int powerMultiplier)
    {
        return new DamageCalculation
        {
            FinalDamage     = _baseDamage * powerMultiplier,
            WasCritical     = false,
            LifestealAmount = 0,
        };
    }
}
