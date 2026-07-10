using UnityEngine;

/// <summary>
/// Ждёт окончания клипа Death и вызывает деактивацию врага.
/// Используем OnStateUpdate, а не OnStateExit — из Death нет перехода,
/// значит Exit никогда не наступит.
/// </summary>
public class EnemyDeathStateBehaviour : StateMachineBehaviour
{
    private bool _fired;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _fired = false;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_fired) return;
        if (stateInfo.normalizedTime < 1f) return;

        _fired = true;

        Enemy enemy = animator.GetComponentInParent<Enemy>();
        if (enemy != null)
            enemy.OnDeathAnimationEnd();
    }
}
