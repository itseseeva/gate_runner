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
        base.OnAnimationHit();

        if (Machine.Current != AttackState) return;

        // Наносим урон, если цель ещё в радиусе.
        if (Target != null && !Target.IsDead
            && DistToTargetPointSqr() <= AttackRange * AttackRange)
        {
            bool killed = Target.TakeDamage(Damage);
            if (killed) ClearTarget();
        }

        // Удар состоялся — атака завершена. В чейз (или retreat, если полон).
        EndAttackAndChase();
    }
}
