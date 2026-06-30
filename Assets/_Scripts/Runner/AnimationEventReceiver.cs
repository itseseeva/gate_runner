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
        var target = _assassinAttack != null ? _assassinAttack.GetCurrentTarget() : null;
        string tName = target != null ? target.name : "NULL";
        Debug.Log($"[Event] OnAttackHit на {gameObject.name}, currentTarget={tName}, frame={Time.frameCount}", this);
        _tankAttack?.OnAttackHit();
        _warriorAttack?.OnAttackHit();
        _assassinAttack?.OnAttackHit();
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
}
