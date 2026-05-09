using UnityEngine;
using TMPro;

/// <summary>
/// Показывает количество живых врагов в текущем уровне.
/// Учитывает не только заспавненных, но и тех кого ещё надо заспавнить.
/// </summary>
public class EnemyCounterUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _label;

    private LevelGenerator _generator;

    private void Start()
    {
        _generator = FindAnyObjectByType<LevelGenerator>();
        if (_generator == null)
            Debug.LogError("[EnemyCounter] LevelGenerator не найден!", this);

        if (_label == null)
            _label = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (_generator == null || _label == null) return;

        int total = _generator.GetTotalEnemiesRemaining();
        _label.text = $"Врагов: {total}";
    }
}
