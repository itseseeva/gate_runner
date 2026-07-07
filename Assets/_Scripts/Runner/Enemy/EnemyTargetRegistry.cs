using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Реестр целей врагов — сколько врагов направлено на каждого юнита отряда.
/// Позволяет распределять новых врагов равномерно, а не всем в одну цель.
/// 
/// Логика: враг при выборе цели вызывает GetLeastAttacked(),
/// получает юнита с минимальным счётчиком, и вызывает Register(target).
/// При смене цели или смерти — Unregister(target).
/// 
/// Статический класс — единая точка на всю сцену, все враги смотрят сюда.
/// </summary>
public static class EnemyTargetRegistry
{
    // Счётчики: сколько врагов направлено на каждого юнита.
    private static readonly Dictionary<Unit, int> _claimCount = new();

    // Счётчики: сколько врагов на каждой стороне атаки.
    private static readonly Dictionary<AttackSide, int> _sideCount = new()
    {
        { AttackSide.Front,      0 },
        { AttackSide.LeftFlank,  0 },
        { AttackSide.RightFlank, 0 },
    };

    // Веса — сколько врагов ХОТИМ на каждой стороне (пропорция).
    // Front=3, Left=1, Right=1 → 60/20/20 = стандарт жанра.
    // TODO: вынести в GameSettingsSO для Remote Config
    private static readonly Dictionary<AttackSide, int> _sideWeights = new()
    {
        { AttackSide.Front,      3 },
        { AttackSide.LeftFlank,  1 },
        { AttackSide.RightFlank, 1 },
    };

    /// <summary>Регистрирует что ещё один враг идёт на этого юнита.</summary>
    public static void Register(Unit target)
    {
        if (target == null) return;
        if (_claimCount.ContainsKey(target))
            _claimCount[target]++;
        else
            _claimCount[target] = 1;
    }

    /// <summary>Снимает регистрацию — враг больше не идёт к этому юниту.</summary>
    public static void Unregister(Unit target)
    {
        if (target == null) return;
        if (!_claimCount.ContainsKey(target)) return;
        
        _claimCount[target]--;
        if (_claimCount[target] <= 0)
            _claimCount.Remove(target);
    }

    /// <summary>
    /// Возвращает юнита, к которому идёт МЕНЬШЕ ВСЕГО врагов.
    /// При равенстве счётчиков — берёт ближайшего по XZ к позиции врага.
    /// Возвращает null если живых юнитов нет.
    /// </summary>
    public static Unit GetLeastAttacked(Vector3 fromPosition, SquadController squad)
    {
        if (squad == null) return null;

        IReadOnlyList<Unit> units = squad.AllUnits;
        if (units == null || units.Count == 0) return null;

        Unit best = null;
        int minClaims = int.MaxValue;
        float minDistSqr = float.MaxValue;

        foreach (Unit u in units)
        {
            if (u == null || u.IsDead) continue;
            if (!u.gameObject.activeSelf) continue;

            int claims = _claimCount.TryGetValue(u, out int c) ? c : 0;
            float distSqr = SqrDistanceXZ(fromPosition, u.transform.position);

            // Правило: меньше клеймов важнее чем меньше дистанция.
            // При равных клеймах — ближайший.
            if (claims < minClaims || (claims == minClaims && distSqr < minDistSqr))
            {
                minClaims = claims;
                minDistSqr = distSqr;
                best = u;
            }
        }

        return best;
    }

    /// <summary>
    /// Регистрирует что ещё один враг идёт по этой стороне.
    /// </summary>
    public static void RegisterSide(AttackSide side)
    {
        _sideCount[side]++;
    }

    /// <summary>
    /// Снимает регистрацию — враг больше не занимает эту сторону.
    /// </summary>
    public static void UnregisterSide(AttackSide side)
    {
        if (_sideCount[side] > 0)
            _sideCount[side]--;
    }

    /// <summary>
    /// Возвращает наименее занятую сторону с учётом весов.
    /// Front имеет вес 3, фланги по 1 — значит на Front идёт втрое больше врагов.
    /// Формула: занятость / вес. Кто меньше — тому и идти.
    /// </summary>
    public static AttackSide GetLeastCrowdedSide()
    {
        AttackSide best = AttackSide.Front;
        float bestScore = float.MaxValue;

        foreach (var kv in _sideCount)
        {
            AttackSide side = kv.Key;
            int count = kv.Value;
            int weight = _sideWeights[side];

            // Занятость на единицу веса. Front с 6 врагами и весом 3 = 2.
            // Left с 3 врагами и весом 1 = 3. Значит Front менее занят → туда.
            float score = (float)count / weight;

            if (score < bestScore)
            {
                bestScore = score;
                best = side;
            }
        }

        return best;
    }

    /// <summary>Полный сброс — на случай перезапуска уровня.</summary>
    public static void Clear()
    {
        _claimCount.Clear();
        _sideCount[AttackSide.Front]      = 0;
        _sideCount[AttackSide.LeftFlank]  = 0;
        _sideCount[AttackSide.RightFlank] = 0;
    }

    private static float SqrDistanceXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }
}
