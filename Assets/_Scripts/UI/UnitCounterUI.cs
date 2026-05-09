using UnityEngine;
using TMPro;

/// <summary>
/// Показывает количество юнитов в отряде. Текст вида "Юнитов: 12".
/// Обновляется каждый кадр (дёшево — просто чтение int).
/// </summary>
public class UnitCounterUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _label;

    private SquadController _squad;

    private void Start()
    {
        _squad = FindAnyObjectByType<SquadController>();
        if (_squad == null)
            Debug.LogError("[UnitCounter] SquadController не найден!", this);

        if (_label == null)
            _label = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (_squad == null || _label == null) return;
        _label.text = $"Юнитов: {_squad.UnitCount}";
    }
}
