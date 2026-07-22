/// <summary>
/// Базовое состояние врага ближнего боя.
/// Каждое состояние владеет своим куском логики и само настраивает
/// скроллер/аниматор в Enter() — это гарантирует, что настройка
/// происходит ровно один раз за переход, а не каждый кадр из разных мест.
/// </summary>
public abstract class EnemyState
{
    protected readonly EnemyCombatBase Ctrl;

    protected EnemyState(EnemyCombatBase ctrl) => Ctrl = ctrl;

    /// <summary>Вызывается один раз при входе в состояние.</summary>
    public virtual void Enter() { }

    /// <summary>Вызывается каждый кадр, пока состояние активно.</summary>
    public abstract void Tick();

    /// <summary>Вызывается один раз при выходе из состояния.</summary>
    public virtual void Exit() { }
}
