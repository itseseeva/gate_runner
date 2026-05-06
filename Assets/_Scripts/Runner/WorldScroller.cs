using UnityEngine;

/// <summary>
/// Двигает объект "на отряд" по -Z с глобальной скоростью.
/// Висит на врагах, воротах и любых других объектах мира.
/// Скорость одна на всех — задаётся статически.
/// </summary>
public class WorldScroller : MonoBehaviour
{
    /// <summary>
    /// Глобальная скорость "бега мира" в м/сек.
    /// Все объекты с этим компонентом едут с этой скоростью.
    /// TODO: Вынести в GameSettingsSO когда добавим Remote Config
    /// </summary>
    public static float WorldSpeed = 5f;

    /// <summary>Можно временно остановить конкретный объект (например, бой с боссом).</summary>
    [SerializeField] private bool _isMoving = true;

    private void Update()
    {
        if (!_isMoving) return;

        // Двигаемся по -Z (на отряд) со скоростью мира
        transform.position += Vector3.back * WorldSpeed * Time.deltaTime;
    }

    /// <summary>Останавливает движение (для босс-файтов и т.п.)</summary>
    public void Stop()  => _isMoving = false;

    /// <summary>Возобновляет движение.</summary>
    public void Resume() => _isMoving = true;
}
