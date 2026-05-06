using UnityEngine;

/// <summary>
/// Двигает объект "на отряд" по -Z с глобальной скоростью.
/// Висит на врагах, воротах и любых других объектах мира.
/// Останавливается автоматически при Game Over / Victory.
/// </summary>
public class WorldScroller : MonoBehaviour
{
    /// <summary>
    /// Глобальная скорость "бега мира" в м/сек.
    /// TODO: Вынести в GameSettingsSO когда добавим Remote Config
    /// </summary>
    public static float WorldSpeed = 4.2f;

    [SerializeField] private bool _isMoving = true;

    private void OnEnable()
    {
        // Подписываемся на смену состояния игры
        GameStateManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        // Отписываемся — иначе утечка памяти
        GameStateManager.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState newState)
    {
        // Двигаемся только в Playing
        _isMoving = (newState == GameState.Playing);
    }

    private void Update()
    {
        if (!_isMoving) return;
        transform.position += Vector3.back * WorldSpeed * Time.deltaTime;
    }

    public void Stop()   => _isMoving = false;
    public void Resume() => _isMoving = true;
}
