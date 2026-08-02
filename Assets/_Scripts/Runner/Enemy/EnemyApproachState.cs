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
        Ctrl.ResetChaseOffsets();
        // AllowChaseAgain() убран: сброс флага возвращал отступающих
        // обратно в бесконечный цикл «ударил → не пустили в чейз → снова бьёт».
    }

    public override void Tick()
    {
        Ctrl.UpdatePhasing();        // просачивание сквозь застрявших врагов

        // Нет цели — берём ближайшего (поток, без резерва).
        if (Ctrl.Target == null || Ctrl.Target.IsDead || !Ctrl.Target.gameObject.activeSelf)
            Ctrl.RefreshTarget();
        if (Ctrl.Target == null) return;

        // Считаем дистанцию до самого героя, а не до идеальной точки со смещением.
        // Это спасает от ситуаций, когда врага оттерли товарищи по толпе, и он не может встать на свою точку.
        Vector3 heroPos = Ctrl.Target.transform.position;
        float dx = heroPos.x - Ctrl.transform.position.x;
        float dz = heroPos.z - Ctrl.transform.position.z;
        float distSqr = dx * dx + dz * dz;

        // Делаем триггер чуть шире (1.2 вместо 0.9), чтобы в плотной толпе можно было бить из-за спин.
        float trigger = Ctrl.AttackTriggerDistance * 1.2f;

        if (distSqr <= trigger * trigger)
        {
            Debug.Log($"[TrigCheck] {Ctrl.name} dist={Mathf.Sqrt(distSqr):F2} trigger={trigger:F2} animPlaying={Ctrl.IsAttackAnimPlaying}");
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
