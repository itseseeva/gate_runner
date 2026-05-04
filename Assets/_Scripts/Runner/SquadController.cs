using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет отрядом: хранит список юнитов,
/// расставляет их в шеренгу вокруг SquadLeader.
/// </summary>
public class SquadController : MonoBehaviour
{
    [Header("Настройки шеренги")]
    [SerializeField] private float _spacing     = 0.8f;
    [SerializeField] private float _followSpeed = 10f;

    [Header("Стартовый отряд")]
    [SerializeField] private HeroType _startHeroType = HeroType.Warrior;
    [SerializeField] private int      _startCount    = 1;

    private readonly List<Unit> _units = new();
    public int UnitCount => _units.Count;

    private void Start()
    {
        for (int i = 0; i < _startCount; i++)
            AddUnit(_startHeroType);
    }

    private void Update()
    {
        UpdateFormation();
    }

    private void UpdateFormation()
    {
        int count = _units.Count;
        if (count == 0) return;

        float totalWidth = (count - 1) * _spacing;
        float startX     = -totalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            if (_units[i] == null) continue;

            Vector3 offset = new Vector3(startX + i * _spacing, 0f, 0f);

            MeleeUnitController meleeCtrl = _units[i].GetComponent<MeleeUnitController>();
            if (meleeCtrl != null)
            {
                // Всегда обновляем offset (нужен и в Targeting/Combat для возврата)
                meleeCtrl.FormationOffset = offset;

                // Двигаем только если юнит в строю (Follow)
                if (meleeCtrl.IsInFormation)
                {
                    // ЖЁСТКАЯ привязка — без Lerp, ноль отставания
                    _units[i].transform.position = transform.position + offset;
                }
            }
            else
            {
                // Range-юнит (Mage/Archer) — оставляем Lerp как было
                Vector3 target = transform.position + offset;
                _units[i].transform.position = Vector3.Lerp(
                    _units[i].transform.position,
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
            // Вычисляем offset позиции в шеренге для этого юнита
            Vector3 offset = CalculateFormationOffset(_units.Count - 1);
            meleeCtrl.Initialize(transform, offset);
        }

        Debug.Log($"[SquadController] +1 {type}. Всего: {_units.Count}", this);
    }

    /// <summary>Считает offset юнита в шеренге по индексу.</summary>
    private Vector3 CalculateFormationOffset(int index)
    {
        int count = _units.Count;
        float totalWidth = (count - 1) * _spacing;
        float startX     = -totalWidth / 2f;
        return new Vector3(startX + index * _spacing, 0f, 0f);
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