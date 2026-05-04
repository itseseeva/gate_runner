using UnityEngine;

/// <summary>
/// Состояние "в строю".
/// Воин НЕ двигает себя — за него это делает SquadController.
/// State только сканирует врагов и переходит в Strike при появлении.
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
        // Разрешаем SquadController нас двигать
        _ctrl.IsInFormation = true;
    }

    public void Exit()
    {
        // Запрещаем SquadController нас двигать — теперь рулим сами
        _ctrl.IsInFormation = false;
    }

    public void Tick()
    {
        if (_ctrl.Leader == null) return;

        // Ищем рандомного врага в радиусе
        Enemy enemy = _ctrl.FindRandomEnemyInRange(_ctrl.DetectionRange);
        if (enemy != null)
        {
            // Бронируем и переходим в рывок
            _ctrl.ClaimTarget(enemy);
            _ctrl.StrikeState.SetTarget(enemy);
            _ctrl.ChangeState(_ctrl.StrikeState);
        }
    }
}
