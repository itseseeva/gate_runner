using UnityEngine;

/// <summary>
/// Враг бьёт цель. Скроллер выключен — иначе мир утащит врага по -Z
/// и он отвалится от героя. Вместо этого Z прибивается к Z цели через Lerp.
/// Урон летит через Animation Event → EnemyCombatBase.OnAnimationHit().
/// Выход: цель ушла за гистерезис → Approach; клип атаки доиграл → Chase.
/// </summary>
public class EnemyAttackState : EnemyState
{
    public EnemyAttackState(EnemyCombatBase ctrl) : base(ctrl) { }

    public override void Enter()
    {
        Ctrl.SetScroller(false);
        Ctrl.SetAnimatorAttacking(true);
    }

    public override void Tick()
    {
        if (Ctrl.Target == null)
        {
            Ctrl.Machine.ChangeState(Ctrl.ApproachState);
            return;
        }

        float distSqr = Ctrl.DistToTargetPointSqr();
        float hysteresis = Ctrl.AttackRange * 1.2f;

        // Гистерезис: выходим по большей дистанции, чем входили (0.9 vs 1.2),
        // иначе враг на границе будет дребезжать между Approach и Attack.
        if (distSqr > hysteresis * hysteresis)
        {
            Ctrl.Machine.ChangeState(Ctrl.ApproachState);
            return;
        }

        Ctrl.FaceTarget();

        // Melee прибивается к Z цели, чтобы не отвалиться. Ranged держит дистанцию.
        if (Ctrl.SticksToTargetZ)
        {
            Transform t = Ctrl.transform;
            float targetZ = Ctrl.Target.transform.position.z;
            float newZ = Mathf.Lerp(t.position.z, targetZ, 15f * Time.deltaTime);
            t.position = new Vector3(t.position.x, t.position.y, newZ);
        }
    }

    public override void Exit()
    {
        Ctrl.SetAnimatorAttacking(false);
    }
}
