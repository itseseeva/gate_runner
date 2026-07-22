using UnityEngine;

/// <summary>
/// После удара враг отступает за спины отряда, держит строй позади и
/// всё время смотрит на героя, которого бил — читается как догоняющий,
/// а не как убегающий. Скроллер выключен, враг едет сам.
/// Выход: мир замедлился (чит сейчас, ворота позже).
/// </summary>
public class EnemyChaseState : EnemyState
{
    private const float SmoothTimeX = 0.4f;   // инерция по X
    private const float BlendInTime = 0.8f;   // разгон входа в строй

    private float _enteredAt;
    private float _velX;
    private Unit  _lookTarget;

    public EnemyChaseState(EnemyCombatBase ctrl) : base(ctrl) { }

    public override void Enter()
    {
        Ctrl.SetScroller(false);      // ← Z полностью под нашим контролем
        Ctrl.SetAnimatorAttacking(false);
        _velX = 0f;
        _lookTarget = Ctrl.Target;
        _enteredAt = Time.time;
    }

    public override void Tick()
    {
        if (Ctrl.Leader == null) return;

        // ── Выход ────────────────────────────────────────────────────
        bool worldSlowed = WorldScroller.WorldSpeed < WorldScroller.BaseWorldSpeed * 0.5f;
        if (worldSlowed)
        {
            Ctrl.Machine.ChangeState(Ctrl.ApproachState);
            return;
        }

        float minZ = Ctrl.GetSquadBackZ();
        float speedRatio = WorldScroller.WorldSpeed / WorldScroller.BaseWorldSpeed;
        float lineDistance = (Ctrl.Data != null ? Ctrl.Data.ChaseDistance : 0.8f) * speedRatio;

        Vector2 slot = Ctrl.GetChaseSlot(Ctrl.Leader.position.x);

        // Личное отставание поверх слота — толпа дышит, а не марширует.
        float lag = Ctrl.GetChaseLag();

        float targetZ = minZ - lineDistance - slot.y - lag;

        // ── Z: держим ChaseDistance точно, но входим плавно ───────────
        // Первые BlendInTime секунд Lerp медленный — враг мягко отваливается назад.
        // Дальше жёсткий — дистанция держится ровно, отряд не убегает.
        float blend = Mathf.Clamp01((Time.time - _enteredAt) / BlendInTime);
        float zSpeed = Mathf.Lerp(1.2f, 8f, Mathf.SmoothStep(0f, 1f, blend));

        float newZ = Mathf.Lerp(Ctrl.transform.position.z, targetZ, zSpeed * Time.deltaTime);
        Ctrl.transform.position = new Vector3(
            Ctrl.transform.position.x, Ctrl.transform.position.y, newZ);

        // ── X: разъезд по слоту с инерцией ───────────────────────────
        float chaseSpeed = Ctrl.Data != null ? Ctrl.Data.ChaseSpeed : 3f;
        float newX = Mathf.SmoothDamp(
            Ctrl.transform.position.x, slot.x,
            ref _velX, SmoothTimeX, chaseSpeed, Time.deltaTime);

        Ctrl.transform.position = new Vector3(
            newX, Ctrl.transform.position.y, Ctrl.transform.position.z);

        // ── Взгляд ───────────────────────────────────────────────────
        // ВСЕГДА на героя, которого бил — враг отстаёт лицом к отряду.
        Transform look;
        if (_lookTarget != null && !_lookTarget.IsDead && _lookTarget.gameObject.activeSelf)
            look = _lookTarget.transform;
        else if (Ctrl.Target != null)
            look = Ctrl.Target.transform;
        else
            look = Ctrl.Leader;

        Vector3 lookDir = look.position - Ctrl.transform.position;
        lookDir.y = 0;

        if (lookDir.sqrMagnitude > 0.0001f)
        {
            // Компенсируем поворот модели Skeleton_110 (-190°), как в FaceTarget
            Quaternion targetRot = Quaternion.LookRotation(-lookDir);
            Ctrl.transform.rotation = Quaternion.Slerp(
                Ctrl.transform.rotation, targetRot, Ctrl.RotationSpeedValue * Time.deltaTime);
        }
    }
}
