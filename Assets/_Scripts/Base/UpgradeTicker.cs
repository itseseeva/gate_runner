using UnityEngine;

/// <summary>
/// Тикер апгрейдов. Каждый Update проверяет здания которые апгрейдятся.
/// Если таймер истёк — завершает апгрейд.
///
/// Висит на BaseManager или отдельном объекте на сцене BaseScene.
/// </summary>
public class UpgradeTicker : MonoBehaviour
{
    private void Update()
    {
        if (BaseManager.Instance == null) return;
        BaseManager.Instance.CheckUpgradeTimers();
    }
}
