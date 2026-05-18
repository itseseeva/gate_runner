using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Чит-кнопки для разработки на BaseScene.
/// TODO: убрать или скрыть за паролем в релизной версии.
/// </summary>
public class BaseCheatPanel : MonoBehaviour
{
    [Header("Кнопки")]
    [SerializeField] private Button _addGoldButton;
    [SerializeField] private Button _addIronButton;
    [SerializeField] private Button _finishUpgradeButton;
    [SerializeField] private Button _activateBuffButton;
    [SerializeField] private Button _resetBaseButton;

    private void Start()
    {
        if (_addGoldButton != null)
            _addGoldButton.onClick.AddListener(() =>
            {
                if (PlayerDataManager.Instance != null)
                {
                    PlayerDataManager.Instance.AddGold(1000);
                    Debug.Log("[Cheat] +1000 Gold");
                }
            });

        if (_addIronButton != null)
            _addIronButton.onClick.AddListener(() =>
            {
                if (ResourceManager.Instance != null)
                {
                    ResourceManager.Instance.AddIron(1000);
                    Debug.Log("[Cheat] +1000 Iron");
                }
            });

        if (_finishUpgradeButton != null)
            _finishUpgradeButton.onClick.AddListener(FinishCurrentUpgrade);

        if (_activateBuffButton != null)
            _activateBuffButton.onClick.AddListener(() =>
            {
                if (ResourceManager.Instance != null)
                {
                    ResourceManager.Instance.ActivateProductionBuff(60f);
                    Debug.Log("[Cheat] Бафф активирован на 60с");
                }
            });

        if (_resetBaseButton != null)
            _resetBaseButton.onClick.AddListener(() =>
            {
                if (BaseManager.Instance != null)
                {
                    BaseManager.Instance.ResetBase();
                    Debug.Log("[Cheat] База сброшена");
                }
            });
    }

    private void FinishCurrentUpgrade()
    {
        if (BaseManager.Instance == null) return;

        // Находим здание которое сейчас апгрейдится и завершаем
        foreach (var b in BaseManager.Instance.Buildings)
        {
            if (b.IsUpgrading)
            {
                // Сдвигаем UpgradeEndTime в прошлое — таймер сам завершит
                b.UpgradeEndTime = DateTime.UtcNow.AddSeconds(-1);
                Debug.Log($"[Cheat] Принудительное завершение апгрейда {b.Type}");
                return;
            }
        }

        Debug.Log("[Cheat] Нет активных апгрейдов");
    }
}
