using UnityEngine;

/// <summary>
/// Враг-камикадзе. Едет к отряду как обычный melee, но на дистанции рывка
/// кидается в roll, врезается в отряд и взрывается. Медленнее обычных врагов.
/// </summary>
public class EnemyRollCombat : EnemyCombatBase
{
    // На 30% медленнее обычных врагов.
    public override float MoveSpeedMultiplier => 0.7f;

    // Вместо обычной атаки заходит в рывок.
    public override EnemyState AttackStateFor => RollState;

    // Не прибивается к Z цели — он в неё влетает.
    public override bool SticksToTargetZ => false;

    public override float AttackTriggerDistance => Data != null ? Data.RollTriggerRange : 5f;

    public override void OnAnimationHit() { }   // урон идёт из RollState, не по Animation Event
}
