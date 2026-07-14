using UnityEngine;

/// <summary>
/// Двигает объект "на отряд" по -Z с глобальной скоростью.
/// Висит на врагах, воротах и любых других объектах мира.
/// Останавливается автоматически при Game Over / Victory.
/// SpeedMultiplier позволяет локально замедлить объект (Frozen-статус).
/// </summary>
public class WorldScroller : MonoBehaviour
{
    /// <summary>
    /// Глобальная скорость "бега мира" в м/сек.
    /// TODO: Вынести в GameSettingsSO когда добавим Remote Config
    /// </summary>
    // TODO: вынести в GameSettingsSO когда добавим Remote Config.
    // Пока хардкод — 3.5 m/s (было 7, снижено в 2 раза для более комфортного темпа).
    /// <summary>Базовая (не-замедленная) скорость мира. Используется другими системами
    /// для расчёта относительных множителей (Chase-дистанции, агрессии врагов и т.п.).</summary>
    public const float BaseWorldSpeed = 3.5f;

    public static float WorldSpeed = BaseWorldSpeed;

    /// <summary>
    /// Локальный множитель скорости (1.0 = норма, 0.5 = в 2 раза медленнее).
    /// Используется статусом Frozen.
    /// </summary>
    public float SpeedMultiplier { get; set; } = 1f;

    [SerializeField] private bool _isMoving = true;

    private void OnEnable()
    {
        GameStateManager.OnStateChanged += HandleStateChanged;
        SpeedMultiplier = 1f;  // сбрасываем при возврате из пула
    }

    private void OnDisable()
    {
        GameStateManager.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState newState)
    {
        _isMoving = (newState == GameState.Playing);
    }

    private void Update()
    {
        if (!_isMoving) return;
        transform.position += Vector3.back * WorldSpeed * SpeedMultiplier * Time.deltaTime;
    }

    public void Stop()   => _isMoving = false;
    public void Resume() => _isMoving = true;
}
