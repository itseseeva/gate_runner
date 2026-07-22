using UnityEngine;

/// <summary>
/// Переходник между Animation Event на модели врага и его логикой боя.
/// Ловит OnEnemyAttackHit с клипа атаки и пробрасывает в EnemyCombatBase на корне prefab-а.
/// </summary>
public class EnemyAnimationEventReceiver : MonoBehaviour
{
    public void OnEnemyAttackHit()
    {
        EnemyCombatBase combat = GetComponentInParent<EnemyCombatBase>();
        if (combat == null) return;
        combat.OnAnimationHit();
    }

    public void OnEnemyDeathEnd()
    {
        Enemy enemy = GetComponentInParent<Enemy>();
        if (enemy == null) return;
        enemy.OnDeathAnimationEnd();
    }
}
