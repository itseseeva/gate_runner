using UnityEngine;

/// <summary>
/// Автоатака танка — удар со щитом отталкивает врага.
/// Урон небольшой, главный эффект — knockback.
/// </summary>
public class WarriorAutoAttack : MeleeAutoAttackBase
{
    [Header("Отталкивание (только для танка)")]
    [Tooltip("Сила отталкивания от 0 до 1")]
    [Range(0f, 1f)]
    [SerializeField] private float _knockbackForce = 0.8f;

    protected override DamageCalculation CalculateDamage(int powerMultiplier)
    {
        return new DamageCalculation
        {
            FinalDamage     = _baseDamage * powerMultiplier,
            WasCritical     = false,
            LifestealAmount = 0,
        };
    }

    public override HitResult Hit(Enemy target)
    {
        HitResult result = base.Hit(target);

        if (result.Hit && target != null)
        {
            KnockbackReceiver knockback = target.GetComponent<KnockbackReceiver>();
            if (knockback != null)
                // Передаём killedByHit — если враг убит, отлетает вместо смерти
                knockback.ApplyKnockback(Vector3.forward, _knockbackForce, result.Killed);
            else
                Debug.LogWarning($"[WarriorAutoAttack] На {target.name} нет KnockbackReceiver!", this);
        }

        return result;
    }
}
