using UnityEngine;

/// <summary>
/// Мгновенная атака в ближнего врага.
/// Используется Warrior.
/// </summary>
public class MeleeSpell : BaseSpell
{
    protected override void Execute(Enemy target)
    {
        target.TakeDamage(_damage);
        Debug.Log($"[MeleeSpell] Удар! Урон: {_damage}", this);
    }
}
