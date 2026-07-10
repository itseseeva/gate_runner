using UnityEngine;

/// <summary>
/// Переходник между Animation Event на модели врага и его логикой боя.
/// Ловит OnEnemyAttackHit с клипа атаки и пробрасывает в EnemyMeleeCombat на корне prefab-а.
/// </summary>
public class EnemyAnimationEventReceiver : MonoBehaviour
{
    public void OnEnemyAttackHit()
    {
        EnemyMeleeCombat combat = GetComponentInParent<EnemyMeleeCombat>();
        if (combat == null)
        {
            Debug.LogWarning($"[EnemyAnimEvent] {name}: нет EnemyMeleeCombat в родителях!", this);
            return;
        }
        combat.OnAnimationHit();
    }
    public void OnEnemyDeathEnd()
    {
        Enemy enemy = GetComponentInParent<Enemy>();
        if (enemy == null)
        {
            Debug.LogWarning($"[EnemyAnimEvent] {name}: нет Enemy в родителях!", this);
            return;
        }
        enemy.OnDeathAnimationEnd();
    }
}
