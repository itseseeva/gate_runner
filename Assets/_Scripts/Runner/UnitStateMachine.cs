using UnityEngine;

/// <summary>
/// Мозг юнита. Держит текущее состояние (IUnitState) и переключает его.
/// Сами состояния — отдельные классы (FollowState, TargetingState, CombatState).
/// </summary>
public class UnitStateMachine : MonoBehaviour
{
    private IUnitState _currentState;

    public IUnitState CurrentState => _currentState;

    /// <summary>Переключает состояние на новое.</summary>
    public void ChangeState(IUnitState newState)
    {
        if (_currentState == newState) return;

        _currentState?.Exit();
        _currentState = newState;
        _currentState?.Enter();
    }

    private void Update()
    {
        _currentState?.Tick();
    }
}
