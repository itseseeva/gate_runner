using UnityEngine;

/// <summary>
/// Стейт "враг мёртв". Триггерит анимацию смерти, отключает коллайдер,
/// через _deathDuration секунд деактивирует GameObject.
/// Возврат в пул делает существующая логика (SetActive false).
/// </summary>
public class EnemyDeadState : EnemyStateBase
{
    private const float DeathDuration = 1.2f; // TODO: подобрать под длину анимации Death

    private float _deathStartTime;

    public EnemyDeadState(EnemyController ctrl) : base(ctrl) { }

    public override void Enter()
    {
        _deathStartTime = Time.time;

        // Триггерим анимацию смерти
        if (_ctrl.Animator != null)
            _ctrl.Animator.SetTrigger("Die");

        // Отключаем коллайдер — юниты и снаряды больше не будут в него попадать
        Collider col = _ctrl.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Отключаем движение мира — труп не должен ехать
        if (_ctrl.Scroller != null)
            _ctrl.Scroller.enabled = false;
    }

    public override void Tick()
    {
        if (Time.time - _deathStartTime >= DeathDuration)
        {
            // Возвращаем коллайдер для следующего использования из пула
            Collider col = _ctrl.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            _ctrl.gameObject.SetActive(false);
        }
    }
}
