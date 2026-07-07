using UnityEngine;

/// <summary>
/// Висит на модели, передаёт Animation Events в компоненты атаки на родителе.
/// </summary>
public class AnimationEventReceiver : MonoBehaviour
{
    private WarriorAutoAttack  _tankAttack;
    private WarriorMeleeAttack _warriorAttack;
    private AssassinAutoAttack _assassinAttack;
    private RangedAutoAttack   _rangedAttack;

    private void Awake()
    {
        _tankAttack     = GetComponentInParent<WarriorAutoAttack>();
        _warriorAttack  = GetComponentInParent<WarriorMeleeAttack>();
        _assassinAttack = GetComponentInParent<AssassinAutoAttack>();
        _rangedAttack   = GetComponentInParent<RangedAutoAttack>();
    }

    public void OnAttackHit()
    {
        _tankAttack?.OnAttackHit();
        _warriorAttack?.OnAttackHit();

        // Ассасин: событие идёт через StrikeState (он знает цель и дистанцию)
        if (_assassinAttack != null)
        {
            var ctrl = GetComponentInParent<MeleeUnitController>();
            ctrl?.StrikeState?.OnAnimationHit();
        }
    }

    // Момент выстрела лучника/мага
    public void OnShoot()
    {
        _rangedAttack?.OnShoot();
    }

    // Момент вспышки мазла — отдельный event для точного тайминга
    public void mazy() => _rangedAttack?.SpawnMuzzle();

    public void FootL() { }
    public void FootR() { }

    /// <summary>
    /// Вызывается через Animation Event на анимации атаки врага.
    /// Пробрасывает в текущий стейт EnemyController (если это AttackState).
    /// </summary>
    public void OnEnemyAttackHit()
    {
        Debug.Log($"[AnimEvent] OnEnemyAttackHit ВЫЗВАН на {name}", this);

        EnemyController ctrl = GetComponentInParent<EnemyController>();
        if (ctrl == null)
        {
            Debug.LogError($"[AnimEvent] {name}: НЕТ EnemyController в родителях!", this);
            return;
        }

        Debug.Log($"[AnimEvent] Текущий стейт: {ctrl.CurrentState?.GetType().Name}", this);

        if (ctrl.CurrentState is EnemyAttackState attackState)
            attackState.OnAnimationHit();
        else
            Debug.LogWarning($"[AnimEvent] Стейт НЕ EnemyAttackState — урон не пойдёт", this);
    }
}
