using UnityEngine;

/// <summary>
/// Состояние "в строю". При плавном возврате после боя — Lerp к позиции.
/// Иначе — SquadController двигает воина как часть толпы.
/// </summary>
public class FollowState : IUnitState
{
    private readonly MeleeUnitController _ctrl;

    public FollowState(MeleeUnitController controller)
    {
        _ctrl = controller;
    }

    public void Enter()
    {
        if (_ctrl.IsRejoining)
            _ctrl.PlayRejoin();
        else
            _ctrl.PlayRun();

        // Пока идёт rejoin — НЕ позволяем SquadController двигать
        // (мы сами Lerp-им). После завершения rejoin — IsInFormation = true
        _ctrl.IsInFormation = !_ctrl.IsRejoining;
    }

    public void Exit()
    {
        _ctrl.IsInFormation = false;
    }

    public void Tick()
    {
        if (_ctrl.Leader == null) return;

        // Плавный возврат идёт?
        if (_ctrl.IsRejoining)
        {
            _ctrl.UpdateRejoin();

            // Завершился rejoin — отдаём управление SquadController и играем Run
            if (!_ctrl.IsRejoining)
            {
                _ctrl.IsInFormation = true;
                _ctrl.PlayRun();
            }

            return; // во время rejoin не ищем врагов
        }



        // Обычный режим — ищем рандомного врага
        // Ищем врага только впереди по Z (не возвращаемся к мёртвой волне)

        // Танк не бегает рывком — стоит в строю и бьёт через AutoAttacker
        if (_ctrl.IsTankUnit) return;

        float minZ = _ctrl.transform.position.z + 1f;
        Enemy enemy = _ctrl.FindRandomEnemyInRange(_ctrl.DetectionRange, minZ: minZ);
        if (enemy != null)
        {
            // Пытаемся заклеймить. Не вышло (занято) — не идём в бой, ждём.
            if (!_ctrl.ClaimTarget(enemy))
                return;

            _ctrl.StrikeState.SetTarget(enemy);
            _ctrl.ChangeState(_ctrl.StrikeState);
        }
    }
}
