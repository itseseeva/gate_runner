using UnityEngine;

public class EnemyChaseAfterAttackBehaviour : StateMachineBehaviour
{
    private bool _hasTriggered = false;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _hasTriggered = false;
        EnemyMeleeCombat combat = animator.GetComponentInParent<EnemyMeleeCombat>();
        if (combat != null)
        {
            combat.IsInAttackStateFlag = true;
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Как только анимация почти закончилась (95%), запускаем уход в Chase.
        // Это разорвёт дедлок: IsAttacking станет false, и аниматор сможет нормально перейти в Run.
        if (!_hasTriggered && stateInfo.normalizedTime >= 0.95f)
        {
            _hasTriggered = true;
            EnemyMeleeCombat combat = animator.GetComponentInParent<EnemyMeleeCombat>();
            if (combat != null)
            {
                combat.EndAttackAndChase();
            }
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        EnemyMeleeCombat combat = animator.GetComponentInParent<EnemyMeleeCombat>();
        if (combat != null)
        {
            combat.IsInAttackStateFlag = false;
        }
    }
}
