using UnityEngine;
using TMPro;

/// <summary>
/// Таймер production-баффа сверху BaseScene.
/// Активен только пока бафф активен.
/// </summary>
public class BuffTimerUI : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private TextMeshProUGUI _label;

    private void Update()
    {
        if (ResourceManager.Instance == null) return;

        bool active = ResourceManager.Instance.IsBuffActive;

        if (_root != null && _root.activeSelf != active)
            _root.SetActive(active);

        if (active && _label != null)
        {
            float remaining = ResourceManager.Instance.BuffRemainingSeconds;
            _label.text = $"Buff x{ResourceManager.Instance.BuffMultiplier:F0}: {FormatTime(remaining)}";
        }
    }

    private string FormatTime(float seconds)
    {
        int min = Mathf.FloorToInt(seconds / 60f);
        int sec = Mathf.FloorToInt(seconds % 60f);
        return $"{min:00}:{sec:00}";
    }
}
