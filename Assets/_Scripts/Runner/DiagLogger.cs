using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Считает статистику по ударам и резерву целей.
/// В конце игры (или по запросу) выводит сводку в Console.
/// </summary>
public static class DiagLogger
{
    // Кто кого ударил и в какой кадр
    private static Dictionary<int, List<int>> _hitsByEnemyId = new();
    // Сколько раз каждый воин выбрал ту цель которая уже была занята
    private static int _claimCollisions = 0;
    // Сколько раз Find возвращал null (никого нет)
    private static int _findEmpty = 0;
    // Сколько раз Find возвращал кандидатов
    private static int _findHits = 0;

    public static void RecordHit(int warriorId, int enemyId)
    {
        if (!_hitsByEnemyId.ContainsKey(enemyId))
            _hitsByEnemyId[enemyId] = new List<int>();
        _hitsByEnemyId[enemyId].Add(warriorId);
    }

    public static void RecordClaimCollision() => _claimCollisions++;
    public static void RecordFindEmpty() => _findEmpty++;
    public static void RecordFindHit() => _findHits++;

    /// <summary>Печатает сводку в Console и сбрасывает счётчики.</summary>
    public static void PrintSummary()
    {
        int totalHits = 0;
        int uniqueEnemies = _hitsByEnemyId.Count;
        int multiHitEnemies = 0;  // врагов которых ударил >1 разный воин
        int maxHitsPerEnemy = 0;

        foreach (var kv in _hitsByEnemyId)
        {
            totalHits += kv.Value.Count;
            HashSet<int> uniqueWarriors = new(kv.Value);
            if (uniqueWarriors.Count > 1) multiHitEnemies++;
            if (kv.Value.Count > maxHitsPerEnemy) maxHitsPerEnemy = kv.Value.Count;
        }

        Debug.Log(
            "═══════ ИТОГИ ═══════\n" +
            $"Find вернул кандидатов: {_findHits} раз\n" +
            $"Find вернул пусто:      {_findEmpty} раз\n" +
            $"Коллизий при Claim:     {_claimCollisions}\n" +
            $"Всего ударов:           {totalHits}\n" +
            $"Уникальных врагов бито: {uniqueEnemies}\n" +
            $"Из них били 2+ воина:   {multiHitEnemies}\n" +
            $"Макс ударов по 1 врагу: {maxHitsPerEnemy}\n" +
            "═════════════════════"
        );

        _hitsByEnemyId.Clear();
        _claimCollisions = 0;
        _findEmpty = 0;
        _findHits = 0;
    }
}
