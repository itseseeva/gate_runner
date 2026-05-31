using UnityEngine;

/// <summary>
/// Висит на модели и передаёт Animation Events в компоненты атаки на родителе.
/// </summary>
public class AnimationEventReceiver : MonoBehaviour
{
    private WarriorAutoAttack  _tankAttack;
    private WarriorMeleeAttack _warriorAttack;

    private void Awake()
    {
        _tankAttack    = GetComponentInParent<WarriorAutoAttack>();
        _warriorAttack = GetComponentInParent<WarriorMeleeAttack>();
    }

    /// <summary>Удар танка/воина.</summary>
    public void OnAttackHit()
    {
        _tankAttack?.OnAttackHit();
        _warriorAttack?.OnAttackHit();
    }

    // Заглушки для звуков шагов
    public void FootL() { }
    public void FootR() { }
}

