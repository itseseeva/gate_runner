/// <summary>
/// Хранит текущее состояние врага и переключает их.
/// Обычный C#-класс, не MonoBehaviour — не нужен свой Update,
/// тикает его EnemyCombatBase. Меньше оверхеда на мобилках.
/// </summary>
public class EnemyStateMachine
{
    /// <summary>Текущее активное состояние.</summary>
    public EnemyState Current { get; private set; }

    /// <summary>Переключает состояние: Exit старого → Enter нового.</summary>
    public void ChangeState(EnemyState next)
    {
        if (next == Current) return;
        Current?.Exit();
        Current = next;
        Current?.Enter();
    }

    /// <summary>Тик текущего состояния. Вызывается из Update владельца.</summary>
    public void Tick() => Current?.Tick();
}
