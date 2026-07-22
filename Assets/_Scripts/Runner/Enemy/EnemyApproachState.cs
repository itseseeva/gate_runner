using UnityEngine;

/// <summary>
/// Враг едет к отряду. Двигает его WorldScroller (мир), плюс личный tracking по X,
/// wobble, lazy и separation — это и даёт "живую толпу", а не строй роботов.
/// Выход: дистанция до цели ≤ AttackRange*0.9 → Attack.
/// </summary>
public class EnemyApproachState : EnemyState
{
    public EnemyApproachState(EnemyCombatBase ctrl) : base(ctrl) { }

    public override void Enter()
    {
        Ctrl.SetScroller(true);      // мир везёт врага к отряду
        Ctrl.SetSpeedMultiplier(1f);
        Ctrl.SetAnimatorAttacking(false);
        Ctrl.AllowChaseAgain();
    }

    public override void Tick()
    {
        Ctrl.UpdatePhasing();        // просачивание сквозь застрявших врагов

        if (Ctrl.Target == null) return;

        float distSqr = Ctrl.DistToTargetPointSqr();
        float trigger = Ctrl.AttackTriggerDistance * 0.9f;

        if (distSqr <= trigger * trigger)
        {
            Debug.Log($"[Approach→Attack] {Ctrl.name}: захожу в {Ctrl.AttackStateFor?.GetType().Name}, " +
                      $"dist={Mathf.Sqrt(distSqr):F2}, range={Ctrl.AttackTriggerDistance}", Ctrl);
            if (Ctrl.IsAttackAnimPlaying) return;
            Ctrl.Machine.ChangeState(Ctrl.AttackStateFor);
            return;
        }

        Ctrl.FaceTarget();
        Ctrl.UpdateMovement();
    }

    public override void Exit()
    {
        Ctrl.StopPhasing();
    }
}
