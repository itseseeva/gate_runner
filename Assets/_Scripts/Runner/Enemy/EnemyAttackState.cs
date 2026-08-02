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
        if (Ctrl.Target == null || Ctrl.Target.IsDead || !Ctrl.Target.gameObject.activeSelf)
        {
            // Цель пропала — берём ближайшего, а не вылетаем в approach вслепую.
            Ctrl.RefreshTarget();
            if (Ctrl.Target == null)
            {
                Ctrl.Machine.ChangeState(Ctrl.ApproachState);
                return;
            }
        }

        float distSqr = Ctrl.DistToTargetPointSqr();
        float hysteresis = Ctrl.AttackRange * 1.2f;

        // НЕ выходим, пока клип атаки играет — иначе пинг-понг, клип рестартится,
        // EndAttackAndChase не срабатывает, враг зависает у спины.
        if (distSqr > hysteresis * hysteresis && !Ctrl.IsAttackAnimPlaying)
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

            // Доводим к цели с ПОСТОЯННОЙ скоростью (MoveTowards), а не Lerp —
            // Lerp с большим коэффициентом даёт рывок тем сильнее, чем дальше цель.
            // MoveTowards двигает ровно, без скачка при входе в атаку.
            float stickSpeed = 4f;   // м/сек подтяжки к цели, крути под вкус
            Vector3 flatTarget = new Vector3(targetPos.x, t.position.y, targetPos.z);
            t.position = Vector3.MoveTowards(t.position, flatTarget, stickSpeed * Time.deltaTime);
        }
    }

    public override void Exit()
    {
        Ctrl.SetAnimatorAttacking(false);
    }
}
