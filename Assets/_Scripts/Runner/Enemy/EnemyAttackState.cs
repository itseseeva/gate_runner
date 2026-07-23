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

        // Melee прибивается к цели, чтобы не отвалиться при движении отряда.
        if (Ctrl.SticksToTargetZ)
        {
            Transform t = Ctrl.transform;
            Vector3 targetPos = Ctrl.Target.transform.position;

            // Z и X тянем к цели — иначе при движении отряда вбок враг
            // отваливается по X, вылетает из Attack и не доходит до чейза.
            float newZ = Mathf.Lerp(t.position.z, targetPos.z, 15f * Time.deltaTime);
            float newX = Mathf.Lerp(t.position.x, targetPos.x, 10f * Time.deltaTime);

            t.position = new Vector3(newX, t.position.y, newZ);
        }
    }

    public override void Exit()
    {
        Ctrl.SetAnimatorAttacking(false);
    }
}
