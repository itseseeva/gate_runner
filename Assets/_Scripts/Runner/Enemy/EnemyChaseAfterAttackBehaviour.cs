using UnityEngine;

/// <summary>
/// Лочит окончание клипа атаки и сообщает EnemyCombatBase, что пора в Chase.
/// Состояния НЕ хранит — источник правды только в EnemyCombatBase.Machine.
/// </summary>
public class EnemyChaseAfterAttackBehaviour : StateMachineBehaviour
{
    private bool _hasTriggered;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _hasTriggered = false;
        var combat = animator.GetComponentInParent<EnemyCombatBase>();
        if (combat != null) combat.IsAttackAnimPlaying = true;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_hasTriggered) return;
        if (stateInfo.normalizedTime < 0.95f) return;

        _hasTriggered = true;
        EnemyCombatBase combat = animator.GetComponentInParent<EnemyCombatBase>();
        if (combat != null) combat.EndAttackAndChase();
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var combat = animator.GetComponentInParent<EnemyCombatBase>();
        if (combat != null) combat.IsAttackAnimPlaying = false;
    }
}
