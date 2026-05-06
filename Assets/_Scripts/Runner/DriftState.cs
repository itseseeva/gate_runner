using UnityEngine;

/// <summary>
/// Состояние "возврат в строй".
/// Воин движется к своей позиции в формации (лидер + offset) на полной скорости.
/// </summary>
public class DriftState : IUnitState
{
    private readonly MeleeUnitController _ctrl;

    public DriftState(MeleeUnitController controller)
    {
        _ctrl = controller;
    }

    public void Enter() { }
    public void Exit()  { }

    public void Tick()
    {
        if (_ctrl.Leader == null) return;

        // 1. Появился враг? → новый рывок
        // Ищем врага ТОЛЬКО впереди — не возвращаемся бить ту же волну
        float minZ = _ctrl.StrikeState.LastHitZ + 1f;
        Enemy enemy = _ctrl.FindRandomEnemyInRange(_ctrl.DetectionRange, minZ: minZ);
        if (enemy != null)
        {
            _ctrl.ClaimTarget(enemy);
            _ctrl.StrikeState.SetTarget(enemy);
            _ctrl.ChangeState(_ctrl.StrikeState);
            return;
        }

        // 2. Целевая позиция в формации
        Vector3 formationPos = _ctrl.Leader.position + _ctrl.FormationOffset;

        // 3. Близко к строю? → возврат в Follow
        float distToFormation = Vector3.Distance(_ctrl.transform.position, formationPos);
        if (distToFormation <= _ctrl.ReturnDistance)
        {
            _ctrl.ChangeState(_ctrl.FollowState);
            return;
        }

        // 4. Двигаемся к позиции в формации
        Vector3 dir = (formationPos - _ctrl.transform.position).normalized;
        float speed = distToFormation > 3f
            ? _ctrl.ChaseSpeed * 3f   // далеко — летим быстро
            : _ctrl.ChaseSpeed * 1.5f; // близко — плавно
        _ctrl.transform.position += dir * speed * Time.deltaTime;
    }
}