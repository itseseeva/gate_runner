using UnityEngine;

/// <summary>
/// Отход без чейза — для врагов сверх лимита толпы.
/// Враг едет назад со скроллером, уходит за камеру и возвращается в пул.
/// Отличие от Chase: не занимает слот в строю и не ждёт замедления мира.
/// </summary>
public class EnemyRetreatState : EnemyState
{
    // Скорость отхода назад — едем с миром (1.0), чтобы враг динамично и без задержек уходил за камеру.
    private const float LagMultiplier = 1.0f;

    private Unit _lookTarget;

    public EnemyRetreatState(EnemyCombatBase ctrl) : base(ctrl) { }

    public override void Enter()
    {
        // Скроллер включён — мир везёт врага назад за камеру.
        Ctrl.SetScroller(true);
        Ctrl.SetSpeedMultiplier(LagMultiplier);
        Ctrl.SetAnimatorAttacking(false);
        Ctrl.SetPhasing(true);   // Отключаем коллайдер и физическое расталкивание, чтобы просачиваться назад без помех

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

        // Ушёл позади порога DespawnZ — возвращаем в пул с чисткой реестра.
        if (Ctrl.transform.position.z < Ctrl.DespawnZ)
            Ctrl.DespawnToPool();
    }

    public override void Exit()
    {
        Ctrl.SetSpeedMultiplier(1f);
        Ctrl.SetPhasing(false);
    }
}
