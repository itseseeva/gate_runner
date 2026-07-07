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

    /// <summary>Полный сброс — на случай перезапуска уровня.</summary>
    public static void Clear()
    {
        _claimCount.Clear();
    }

    private static float SqrDistanceXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }
}
