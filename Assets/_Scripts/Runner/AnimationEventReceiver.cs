using UnityEngine;

/// <summary>
/// Висит на модели, передаёт Animation Events в компоненты атаки на родителе.
/// </summary>
public class AnimationEventReceiver : MonoBehaviour
{
    private WarriorAutoAttack  _tankAttack;
    private WarriorMeleeAttack _warriorAttack;
    private AssassinAutoAttack _assassinAttack;

    private void Awake()
    {
        _tankAttack     = GetComponentInParent<WarriorAutoAttack>();
        _warriorAttack  = GetComponentInParent<WarriorMeleeAttack>();
        _assassinAttack = GetComponentInParent<AssassinAutoAttack>();
    }

    public void OnAttackHit()
    {
        _tankAttack?.OnAttackHit();
        _warriorAttack?.OnAttackHit();
    }

    // Три взмаха ассасина
    public void OnSlash1() => _assassinAttack?.OnSlash1();
    public void OnSlash2() => _assassinAttack?.OnSlash2();
    public void OnSlash3() => _assassinAttack?.OnSlash3();

    public void FootL() { }
    public void FootR() { }
}
