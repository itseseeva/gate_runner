using UnityEngine;

/// <summary>
/// Висит на модели и передаёт Animation Events в WarriorAutoAttack на родителе.
/// Нужен потому что Animation Event ищет метод только на том же GameObject где Animator.
/// </summary>
public class AnimationEventReceiver : MonoBehaviour
{
    private WarriorAutoAttack _warriorAttack;

    private void Awake()
    {
        _warriorAttack = GetComponentInParent<WarriorAutoAttack>();
    }

    /// <summary>Вызывается через Animation Event в момент удара щитом.</summary>
    public void OnAttackHit()
    {
        if (_warriorAttack != null)
            _warriorAttack.OnAttackHit();
    }
}
