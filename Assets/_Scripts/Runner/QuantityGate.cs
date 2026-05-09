using UnityEngine;
using TMPro;

/// <summary>
/// Универсальные ворота количества.
/// Один prefab — настраивается через Inspector.
/// + добавляет, × умножает, - убирает юнитов.
/// Цвет и текст обновляются автоматически при изменении настроек.
/// </summary>
public class QuantityGate : BaseGate
{
    public enum OperationType { Add, Multiply }

    [Header("Настройки ворот")]
    [SerializeField] private OperationType _operation = OperationType.Add;
    [SerializeField] private int           _value     = 2;
    [SerializeField] private HeroType      _heroType  = HeroType.Mage;

    [Header("Визуал (обновляется автоматически)")]
    [SerializeField] private MeshRenderer _leftGlass;
    [SerializeField] private MeshRenderer _rightGlass;
    [SerializeField] private TMPro.TextMeshPro _gateLabel;

    // Цвета стекла в зависимости от типа ворот
    private static readonly Color COLOR_POSITIVE = new Color(0.2f, 0.8f, 0.2f, 0.7f); // зелёный
    private static readonly Color COLOR_NEGATIVE = new Color(0.8f, 0.1f, 0.1f, 0.7f); // красный
    private static readonly Color COLOR_MULTIPLY = new Color(0.9f, 0.7f, 0.1f, 0.7f); // золотой

    protected override string GetLabel()
    {
        string typeStr = _heroType.ToString();
        return _operation switch
        {
            OperationType.Add      => _value >= 0 ? $"+{_value} {typeStr}" : $"{_value} {typeStr}",
            OperationType.Multiply => $"×{_value} {typeStr}",
            _                      => $"+{_value} {typeStr}",
        };
    }

    protected override void ApplyEffect(SquadController squad)
    {
        if (_operation == OperationType.Add)
        {
            if (_value >= 0)
                for (int i = 0; i < _value; i++)
                    squad.AddUnit(_heroType);
            else
                squad.RemoveUnits(_heroType, Mathf.Abs(_value));
        }
        else
        {
            int currentCount = squad.GetUnitCountByType(_heroType);
            int toAdd = currentCount * (_value - 1);
            for (int i = 0; i < toAdd; i++)
                squad.AddUnit(_heroType);
        }
    }

    /// <summary>
    /// Настраивает ворота на лету при процедурной генерации.
    /// Вызывается LevelGenerator-ом сразу после Instantiate.
    /// </summary>
    public void SetupForGenerator(HeroType heroType, bool isMultiply, int value)
    {
        Debug.Log($"[Setup] {gameObject.name} ДО: heroType={_heroType} value={_value}");
        
        _heroType  = heroType;
        _operation = isMultiply ? OperationType.Multiply : OperationType.Add;
        _value     = value;

        Debug.Log($"[Setup] {gameObject.name} ПОСЛЕ: heroType={_heroType} value={_value}");

        OnValidate();
    }

    /// <summary>
    /// Обновляет цвет стёкол в зависимости от типа ворот.
    /// Вызывается автоматически в редакторе при изменении настроек.
    /// </summary>
    private void UpdateVisual()
    {
        Color targetColor = _operation == OperationType.Multiply
            ? COLOR_MULTIPLY
            : _value >= 0 ? COLOR_POSITIVE : COLOR_NEGATIVE;

        if (_leftGlass  != null) SetGlassColor(_leftGlass,  targetColor);
        if (_rightGlass != null) SetGlassColor(_rightGlass, targetColor);
    }

    private void SetGlassColor(MeshRenderer rend, Color color)
    {
        if (rend == null) return;  // защита от пустой ссылки

#if UNITY_EDITOR
        Material mat = rend.sharedMaterial;
        if (mat == null) return;   // защита от пустого материала

        Material newMat = new Material(mat);
        newMat.color = color;
        if (newMat.HasProperty("_BaseColor"))
            newMat.SetColor("_BaseColor", color);
        rend.sharedMaterial = newMat;
#else
        Material mat = rend.material;
        if (mat == null) return;

        mat.color = color;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
#endif
    }

    private void OnValidate()
    {
        UpdateVisual();

        if (_gateLabel != null)
            _gateLabel.text = GetLabel();
    }

    private void Start()
    {
        UpdateVisual();
        if (_gateLabel != null)
            _gateLabel.text = GetLabel();
    }
}
