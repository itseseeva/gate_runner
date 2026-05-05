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
    [Tooltip("Дефолтное расстояние между юнитами в ряду по X")]
    [SerializeField] private float _spacing     = 0.4f;

    [Tooltip("Минимальное расстояние между юнитами при тесноте")]
    [SerializeField] private float _minSpacing  = 0.2f;

    [Tooltip("Расстояние между рядами разных ролей по Z")]
    [SerializeField] private float _rowSpacing  = 1.2f;

    [Tooltip("Расстояние между подрядами одного ряда (когда юнитов слишком много)")]
    [SerializeField] private float _subRowSpacing = 0.4f;

    [Tooltip("Полуширина дорожки — макс смещение по X от центра")]
    [SerializeField] private float _trackHalfWidth = 2f;

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
    /// Расставляет ряд юнитов центрированно по X на нужном Z-смещении.
    /// Если юнитов слишком много — разбивает на подряды (несколько слоёв за основным).
    /// Spacing адаптивный: от дефолтного к минимуму при увеличении плотности.
    /// </summary>
    private void PlaceRow(List<Unit> row, float zOffset)
    {
        int total = row.Count;
        if (total == 0) return;

        // ─── Сколько юнитов максимум влезает в один подряд ───────
        // При минимальном spacing
        float maxWidth      = _trackHalfWidth * 2f;
        int   maxPerSubrow  = Mathf.FloorToInt(maxWidth / _minSpacing) + 1;
        if (maxPerSubrow < 1) maxPerSubrow = 1;

        // ─── Сколько подрядов нужно ───────────────────────────────
        int subrows = Mathf.CeilToInt(total / (float)maxPerSubrow);

        // ─── Размещаем юнитов по подрядам ─────────────────────────
        int placed = 0;
        for (int sub = 0; sub < subrows; sub++)
        {
            // Сколько юнитов в этом подряде
            int countInSub = Mathf.Min(maxPerSubrow, total - placed);

            // Адаптивный spacing — чем больше юнитов, тем плотнее
            float spacing;
            if (countInSub <= 1)
            {
                spacing = _spacing;
            }
            else
            {
                float wantedWidth = (countInSub - 1) * _spacing;
                if (wantedWidth <= maxWidth)
                {
                    // Влезают по дефолтному spacing
                    spacing = _spacing;
                }
                else
                {
                    // Не влезают — ужимаем
                    spacing = maxWidth / (countInSub - 1);
                    if (spacing < _minSpacing) spacing = _minSpacing;
                }
            }

            float totalRowWidth = (countInSub - 1) * spacing;
            float startX        = -totalRowWidth / 2f;
            float subZ          = zOffset + sub * _subRowSpacing;

            for (int i = 0; i < countInSub; i++)
            {
                Unit u = row[placed + i];
                Vector3 offset = new Vector3(startX + i * spacing, 0f, subZ);

                MeleeUnitController meleeCtrl = u.GetComponent<MeleeUnitController>();
                if (meleeCtrl != null)
                {
                    meleeCtrl.FormationOffset = offset;
                    if (meleeCtrl.IsInFormation)
                        u.transform.position = transform.position + offset;
                }
                else
                {
                    Vector3 target = transform.position + offset;
                    u.transform.position = Vector3.Lerp(
                        u.transform.position,
                        target,
                        _followSpeed * Time.deltaTime
                    );
                }
            }

            placed += countInSub;
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