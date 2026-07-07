using UnityEngine;

/// <summary>
/// Переходник между Animation Event на клипе врага и его FSM.
/// Висит на GameObject модели врага (там же где Animator).
/// Ловит событие OnEnemyAttackHit с клипа атаки и пробрасывает
/// в EnemyController.CurrentState если это AttackState.
///
/// Зачем: Animation Event ищет метод на том же GameObject, где Animator.
/// Наш Animator на модели-child, а логика на корне prefab-а —
/// без этого переходника событие не найдёт получателя.
/// </summary>
public class EnemyAnimationEventReceiver : MonoBehaviour
{
    /// <summary>
    /// Вызывается через Animation Event на клипе атаки врага
    /// в момент попадания меча/лапы по цели.
    /// </summary>
    public void OnEnemyAttackHit()
    {
        EnemyController ctrl = GetComponentInParent<EnemyController>();
        if (ctrl == null)
        {
            Debug.LogWarning($"[EnemyAnimEvent] {name}: нет EnemyController в родителях!", this);
            return;
        }

        if (ctrl.CurrentState is EnemyAttackState attackState)
            attackState.OnAnimationHit();
    }
}
