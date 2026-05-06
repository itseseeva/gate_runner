using System;
using UnityEngine;

/// <summary>
/// Состояния игры. Расширяем когда понадобится.
/// </summary>
public enum GameState
{
    Playing,
    GameOver,
    Victory
}

/// <summary>
/// Центральный контроллер состояния игры.
/// Singleton — один на всю сцену. Доступ через GameStateManager.Instance.
/// 
/// Другие классы подписываются на OnStateChanged чтобы реагировать
/// на смену состояния (остановиться, показать UI и т.д.).
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    /// <summary>Событие при смене состояния. Подписчики получают новое состояние.</summary>
    public static event Action<GameState> OnStateChanged;

    [SerializeField] private GameState _currentState = GameState.Playing;

    /// <summary>Текущее состояние игры.</summary>
    public GameState CurrentState => _currentState;

    public bool IsPlaying  => _currentState == GameState.Playing;
    public bool IsGameOver => _currentState == GameState.GameOver;
    public bool IsVictory  => _currentState == GameState.Victory;

    private void Awake()
    {
        // Singleton — гарантируем что в сцене один экземпляр
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _currentState = GameState.Playing;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Переключает состояние и оповещает подписчиков.</summary>
    public void SetState(GameState newState)
    {
        if (_currentState == newState) return;

        _currentState = newState;
        Debug.Log($"[GameState] Состояние изменено: {newState}", this);

        OnStateChanged?.Invoke(newState);
    }

    /// <summary>Удобный метод — переходим в Game Over.</summary>
    public void SetGameOver() => SetState(GameState.GameOver);

    /// <summary>Удобный метод — переходим в Victory.</summary>
    public void SetVictory() => SetState(GameState.Victory);
}
