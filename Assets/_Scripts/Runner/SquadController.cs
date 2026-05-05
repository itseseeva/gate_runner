using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Пара "тип героя + количество" для настройки стартового отряда.
/// </summary>
[System.Serializable]
public class StartUnitEntry
{
    public HeroType heroType = HeroType.Mage;
    [Min(0)] public int count = 1;
}

/// <summary>
/// Управляет отрядом: хранит список юнитов,
/// расставляет их в шеренгу вокруг SquadLeader.
/// </summary>
public class SquadController : MonoBehaviour
{
    [Header("Настройки формации")]
    [SerializeField] private float _spacing     = 0.8f;
    [SerializeField] private float _rowSpacing  = 0.8f;  // расстояние между рядами по Z
    [SerializeField] private float _followSpeed = 10f;

    [Header("Стартовый отряд")]
    [SerializeField] private List<StartUnitEntry> _startUnits = new()
    {
        new StartUnitEntry { heroType = HeroType.Tank, count = 1 },
        new StartUnitEntry { heroType = HeroType.Mage, count = 5 },
    };

    private readonly List<Unit> _units = new();
    public int UnitCount => _units.Count;

    private void Start()
    {
        foreach (StartUnitEntry entry in _startUnits)
        {
            for (int i = 0; i < entry.count; i++)
                AddUnit(entry.heroType);
        }
    }

    private void Update()
    {
        UpdateFormation();
    }

    private void UpdateFormation()
    {
        int count = _units.Count;
        if (count == 0) return;

        // ─── Раскладываем юнитов по 4 рядам ───────────────
        List<Unit>[] rows = new List<Unit>[4]
        {
            new List<Unit>(),  // 0 = Front (Tank)
            new List<Unit>(),  // 1 = Mid   (Warrior, Assassin)
            new List<Unit>(),  // 2 = Back  (Mage, Archer)
            new List<Unit>(),  // 3 = Rear  (Healer, Support)
        };

        foreach (Unit u in _units)
        {
            if (u == null) continue;
            int rowIndex = HeroDefinitionSO.GetFormationRow(u.HeroType);
            rows[rowIndex].Add(u);
        }

        // ─── Расставляем непустые ряды последовательно, без дырок ───────────────
        // Считаем сколько рядов с юнитами
        int activeRows = 0;
        for (int r = 0; r < 4; r++)
            if (rows[r].Count > 0) activeRows++;

        // Расставляем непустые ряды последовательно
        int slot = activeRows - 1;
        for (int r = 0; r < 4; r++)
        {
            if (rows[r].Count == 0) continue;
            float zOffset = slot * _rowSpacing;
            PlaceRow(rows[r], zOffset);
            slot--;
        }
    }

    /// <summary>
    /// Расставляет ряд юнитов центрированно по X на нужном Z-смещении от лидера.
    /// </summary>
    private void PlaceRow(List<Unit> row, float zOffset)
    {
        int count = row.Count;
        if (count == 0) return;

        float totalWidth = (count - 1) * _spacing;
        float startX     = -totalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            Unit u = row[i];
            Vector3 offset = new Vector3(startX + i * _spacing, 0f, zOffset);

            MeleeUnitController meleeCtrl = u.GetComponent<MeleeUnitController>();
            if (meleeCtrl != null)
            {
                // Легендарка (Warrior/Assassin) — обновляем offset, двигаем только в Follow
                meleeCtrl.FormationOffset = offset;
                if (meleeCtrl.IsInFormation)
                    u.transform.position = transform.position + offset;
            }
            else
            {
                // Обычный юнит (Tank/Mage/Archer/Healer/Support) — Lerp
                Vector3 target = transform.position + offset;
                u.transform.position = Vector3.Lerp(
                    u.transform.position,
                    target,
                    _followSpeed * Time.deltaTime
                );
            }
        }
    }

    /// <summary>Добавляет юнита в отряд из пула.</summary>
    public void AddUnit(HeroType type)
    {
        Unit unit = UnitPool.Instance.Get(type);
        _units.Add(unit);

        // Если это милишник — инициализируем его контроллер
        MeleeUnitController meleeCtrl = unit.GetComponent<MeleeUnitController>();
        if (meleeCtrl != null)
        {
            // Стартовый offset = ноль, реальный пересчитается через UpdateFormation в этом же кадре
            meleeCtrl.Initialize(transform, Vector3.zero);
        }

        Debug.Log($"[SquadController] +1 {type}. Всего: {_units.Count}", this);
    }



    /// <summary>Убирает последнего юнита в пул.</summary>
    public void RemoveLastUnit()
    {
        if (_units.Count == 0) return;
        Unit unit = _units[^1];
        _units.RemoveAt(_units.Count - 1);
        UnitPool.Instance.Return(unit);
        Debug.Log($"[SquadController] -1 юнит. Всего: {_units.Count}", this);
    }

    /// <summary>Тест через кнопку в редакторе — добавить воина.</summary>
    [ContextMenu("Тест — добавить воина")]
    private void TestAddWarrior() => AddUnit(HeroType.Warrior);

    /// <summary>Тест через кнопку в редакторе — убрать юнита.</summary>
    [ContextMenu("Тест — убрать юнита")]
    private void TestRemove() => RemoveLastUnit();
}