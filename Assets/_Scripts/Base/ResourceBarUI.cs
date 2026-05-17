using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Верхний бар ресурсов на BaseScene.
/// Обновляется при изменении Gold/Iron через события менеджеров.
/// Когда число растёт — лёгкий "punch" эффект (масштабирование).
/// </summary>
public class ResourceBarUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _goldText;
    [SerializeField] private TextMeshProUGUI _ironText;

    private int _lastGold;
    private int _lastIron;

    private void Start()
    {
        // Подписываемся на события
        PlayerDataManager.OnDataChanged += UpdateUI;
        ResourceManager.OnResourcesChanged += UpdateUI;

        UpdateUI();
    }

    private void OnDestroy()
    {
        PlayerDataManager.OnDataChanged -= UpdateUI;
        ResourceManager.OnResourcesChanged -= UpdateUI;
    }

    private void UpdateUI()
    {
        if (PlayerDataManager.Instance != null && _goldText != null)
        {
            int gold = PlayerDataManager.Instance.Gold;
            _goldText.text = $"Gold: {gold}";
            if (gold > _lastGold) PunchScale(_goldText.transform);
            _lastGold = gold;
        }

        if (ResourceManager.Instance != null && _ironText != null)
        {
            int iron = ResourceManager.Instance.Iron;
            _ironText.text = $"Iron: {iron}";
            if (iron > _lastIron) PunchScale(_ironText.transform);
            _lastIron = iron;
        }
    }

    private void PunchScale(Transform t)
    {
        if (t == null) return;
        t.DOKill();
        t.localScale = Vector3.one;
        t.DOPunchScale(Vector3.one * 0.15f, 0.25f, 1, 0.3f);
    }
}
