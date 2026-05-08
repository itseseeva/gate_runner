using UnityEngine;
using TMPro;

/// <summary>
/// Ворота меняющие стихию всего отряда при прохождении.
/// Один префаб GatePair_Element — настраивается через Inspector.
/// </summary>
public class ElementGate : BaseGate
{
    [Header("Стихия")]
    [SerializeField] private ElementType _element = ElementType.Fire;

    [Header("Визуал (обновляется автоматически)")]
    [SerializeField] private MeshRenderer _leftGlass;
    [SerializeField] private MeshRenderer _rightGlass;
    [SerializeField] private TextMeshPro  _gateLabel;

    // Цвета стекла по стихии
    private static readonly Color COLOR_FIRE      = new Color(1f, 0.4f, 0.1f, 0.7f);
    private static readonly Color COLOR_ICE       = new Color(0.4f, 0.8f, 1f, 0.7f);
    private static readonly Color COLOR_LIGHTNING = new Color(1f, 0.95f, 0.3f, 0.7f);
    private static readonly Color COLOR_NONE      = new Color(0.7f, 0.7f, 0.7f, 0.7f);

    protected override string GetLabel()
    {
        return _element switch
        {
            ElementType.Fire      => "🔥 FIRE",
            ElementType.Ice       => "❄ ICE",
            ElementType.Lightning => "⚡ LIGHTNING",
            _                     => "× NEUTRAL",
        };
    }

    protected override void ApplyEffect(SquadController squad)
    {
        squad.SetSquadElement(_element);
    }

    private void UpdateVisual()
    {
        Color color = _element switch
        {
            ElementType.Fire      => COLOR_FIRE,
            ElementType.Ice       => COLOR_ICE,
            ElementType.Lightning => COLOR_LIGHTNING,
            _                     => COLOR_NONE,
        };

        if (_leftGlass  != null) SetGlassColor(_leftGlass,  color);
        if (_rightGlass != null) SetGlassColor(_rightGlass, color);
    }

    private void SetGlassColor(MeshRenderer rend, Color color)
    {
        if (rend == null) return;

#if UNITY_EDITOR
        Material mat = rend.sharedMaterial;
        if (mat == null) return;

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

    protected override void Start()
    {
        base.Start();
        UpdateVisual();
    }
}
