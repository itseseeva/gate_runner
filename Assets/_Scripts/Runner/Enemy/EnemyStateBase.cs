using UnityEngine;

/// <summary>
/// Базовый стейт врага. Наследники реализуют логику конкретного состояния.
/// Enter — вход в стейт, Tick — каждый кадр, Exit — выход.
/// </summary>
public abstract class EnemyStateBase
{
    protected readonly EnemyController _ctrl;

    protected EnemyStateBase(EnemyController ctrl)
    {
        _ctrl = ctrl;
    }

    /// <summary>Вызывается один раз при входе в стейт.</summary>
    public virtual void Enter() { }

    /// <summary>Вызывается каждый кадр пока стейт активен.</summary>
    public virtual void Tick() { }

    /// <summary>Вызывается один раз при выходе из стейта.</summary>
    public virtual void Exit() { }
}
