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
        float minZ = _ctrl.transform.position.z + 1f;
        Enemy enemy = _ctrl.FindRandomEnemyInRange(_ctrl.DetectionRange, minZ: minZ);
        if (enemy != null)
        {
            _ctrl.ClaimTarget(enemy);

            // Ассасин использует своё состояние с 3 ударами
            if (_ctrl.AssassinStrikeState != null)
            {
                var assassinAttack = _ctrl.GetComponent<AssassinAutoAttack>();
                if (assassinAttack != null && !assassinAttack.IsSeriesReady)
                {
                    _ctrl.ReleaseTarget(enemy);
                    return;
                }
                _ctrl.AssassinStrikeState.SetTarget(enemy);
                _ctrl.ChangeState(_ctrl.AssassinStrikeState);
            }
            else
            {
                _ctrl.StrikeState.SetTarget(enemy);
                _ctrl.ChangeState(_ctrl.StrikeState);
            }
        }
    }
}
