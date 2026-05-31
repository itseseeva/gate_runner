using UnityEngine;

/// <summary>
/// Висит на модели и передаёт Animation Events в компоненты атаки на родителе.
/// Поддерживает танка (WarriorAutoAttack) и воина (WarriorMeleeAttack).
/// </summary>
public class AnimationEventReceiver : MonoBehaviour
{
    private WarriorAutoAttack _tankAttack;
    private WarriorMeleeAttack _warriorAttack;

    private void Awake()
    {
        _tankAttack = GetComponentInParent<WarriorAutoAttack>();
        _warriorAttack = GetComponentInParent<WarriorMeleeAttack>();
    }

    /// <summary>Вызывается через Animation Event — удар танка щитом.</summary>
    public void OnAttackHit()
    {
        _tankAttack?.OnAttackHit();
        _warriorAttack?.OnAttackHit();
    }

    // Заглушки для Animation Events (звуки шагов)
    public void FootL() { }
    public void FootR() { }
}
