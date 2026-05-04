using UnityEngine;

/// <summary>
/// Ворота количества: +N или ×N юнитов.
/// </summary>
public class QuantityGate : BaseGate
{
    public enum OperationType { Add, Multiply }

    [Header("Настройки")]
    [SerializeField] private OperationType _operation = OperationType.Add;
    [SerializeField] private int           _value     = 2;
    [SerializeField] private HeroType      _heroType  = HeroType.Warrior;

    protected override string GetLabel()
    {
        return _operation == OperationType.Add ? $"+{_value}" : $"×{_value}";
    }

    protected override void ApplyEffect(SquadController squad)
    {
        int count = _operation == OperationType.Add
            ? _value
            : squad.UnitCount * (_value - 1);

        for (int i = 0; i < count; i++)
            squad.AddUnit(_heroType);
    }
}
