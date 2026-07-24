using UnityEngine;

/// <summary>
/// Отход без чейза — для врагов сверх лимита толпы.
/// Враг едет назад со скроллером, уходит за камеру и возвращается в пул.
/// Отличие от Chase: не занимает слот в строю и не ждёт замедления мира.
/// </summary>
public class EnemyRetreatState : EnemyState
{
    // Насколько медленнее мира едет отступающий — отряд от него отрывается.
    private const float LagMultiplier = 0.4f;

    // За сколько метров позади отряда враг уходит в пул.
    private const float DespawnBehind = 12f;

    private Unit _lookTarget;

    public EnemyRetreatState(EnemyCombatBase ctrl) : base(ctrl) { }

    public override void Enter()
    {
        // Скроллер включён, но замедленный — отряд уезжает вперёд сам,
        // враг не гребёт назад, а просто отстаёт.
        Ctrl.SetScroller(true);
        Ctrl.SetSpeedMultiplier(LagMultiplier);
        Ctrl.SetAnimatorAttacking(false);

        _lookTarget = Ctrl.Target;
    }

    public override void Tick()
    {
        // Смотрим на отряд, пока отстаём — как в чейзе, чтобы не убегал спиной.
        Transform look = null;
        if (_lookTarget != null && !_lookTarget.IsDead && _lookTarget.gameObject.activeSelf)
            look = _lookTarget.transform;
        else if (Ctrl.Leader != null)
            look = Ctrl.Leader;

        if (look != null)
        {
            Vector3 lookDir = look.position - Ctrl.transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(-lookDir);
                Ctrl.transform.rotation = Quaternion.Slerp(
                    Ctrl.transform.rotation, targetRot, Ctrl.RotationSpeedValue * Time.deltaTime);
            }
        }

        // Ушёл достаточно далеко назад — в пул.
        float backZ = Ctrl.GetSquadBackZ();
        if (Ctrl.transform.position.z < backZ - DespawnBehind)
            Ctrl.DespawnSelf();
    }

    public override void Exit()
    {
        Ctrl.SetSpeedMultiplier(1f);
    }
}
