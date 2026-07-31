using UnityEngine;

/// <summary>
/// Враг-камикадзе. Едет к отряду как обычный melee, но на дистанции рывка
/// кидается в roll, врезается в отряд и взрывается. Медленнее обычных врагов.
/// </summary>
public class EnemyRollCombat : EnemyCombatBase
{
    // Вместо обычной атаки заходит в рывок.
    public override EnemyState AttackStateFor => RollState;

    // Не прибивается к Z цели — он в неё влетает.
    public override bool SticksToTargetZ => false;

    public override float AttackTriggerDistance => Data != null ? Data.RollTriggerRange : 5f;

    public override void OnAnimationHit() { }   // урон идёт из RollState, не по Animation Event

    /// <summary>
    /// Роллер не встаёт в очередь распределения — летит в ближайшего героя
    /// первого ряда и взрывается в него. Лимиты реестра игнорирует.
    /// </summary>
    protected override Unit SelectTarget()
    {
        if (Squad == null) return null;

        Unit nearest = null;
        float minDistSqr = float.MaxValue;
        Vector3 myPos = transform.position;

        foreach (Unit u in Squad.AllUnits)
        {
            if (u == null || u.IsDead || !u.gameObject.activeSelf) continue;

            float dx = u.transform.position.x - myPos.x;
            float dz = u.transform.position.z - myPos.z;
            float distSqr = dx * dx + dz * dz;

            if (distSqr < minDistSqr)
            {
                minDistSqr = distSqr;
                nearest = u;
            }
        }
        return nearest;
    }
}
