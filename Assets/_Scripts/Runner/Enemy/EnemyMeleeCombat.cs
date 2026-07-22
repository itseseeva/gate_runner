using UnityEngine;

/// <summary>
/// Враг ближнего боя. Подходит вплотную и бьёт напрямую.
/// Вся логика подхода, чейза и расталкивания — в EnemyCombatBase.
/// </summary>
public class EnemyMeleeCombat : EnemyCombatBase
{
    private int Damage => Data != null ? Data.AttackDamage : 10;

    public override void OnAnimationHit()
    {
        Debug.Log($"[Hit] {name}: OnAnimationHit! state={Machine.Current?.GetType().Name}, " +
                  $"target={(Target != null ? Target.name : "NULL")}", this);

        if (Target == null || Target.IsDead) return;
        if (Machine.Current != AttackState) return;

        // Цель могла отойти, пока играла анимация.
        if (DistToTargetPointSqr() > AttackRange * AttackRange) return;

        bool killed = Target.TakeDamage(Damage);
        if (killed) ClearTarget();
    }
}
