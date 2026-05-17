using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Синглтон хранения прогресса игрока. DontDestroyOnLoad.
/// Сохраняет/загружает данные через PlayerPrefs.
///
/// Хранит: Gold, XP, AccountLevel, список пройденных уровней.
/// Меняется только через публичные методы — они сами сохраняют.
///
/// TODO: при подключении UGS заменить PlayerPrefs на Cloud Save.
/// </summary>
public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    // ─── Ключи PlayerPrefs ────────────────────────────────────────
    private const string KEY_GOLD      = "mgr_gold";
    private const string KEY_XP        = "mgr_xp";
    private const string KEY_ACC_LEVEL = "mgr_acc_level";
    private const string KEY_COMPLETED = "mgr_completed_levels"; // через запятую

    // ─── Состояние ────────────────────────────────────────────────
    public int Gold         { get; private set; }
    public int XP           { get; private set; }
    public int AccountLevel { get; private set; }

    /// <summary>Пройденные уровни. Ключ = LevelData.Id.</summary>
    private readonly HashSet<string> _completedLevels = new();

    // ─── События для UI ───────────────────────────────────────────
    public static event System.Action OnDataChanged;

    /// <summary>Срабатывает при level-up. Параметр — новый уровень.</summary>
    public static event System.Action<int> OnLevelUp;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    // ─── Загрузка / сохранение ────────────────────────────────────

    private void Load()
    {
        Gold         = PlayerPrefs.GetInt(KEY_GOLD, 0);
        XP           = PlayerPrefs.GetInt(KEY_XP, 0);
        AccountLevel = PlayerPrefs.GetInt(KEY_ACC_LEVEL, 1);

        string completedStr = PlayerPrefs.GetString(KEY_COMPLETED, "");
        _completedLevels.Clear();
        if (!string.IsNullOrEmpty(completedStr))
        {
            foreach (string id in completedStr.Split(','))
            {
                if (!string.IsNullOrEmpty(id))
                    _completedLevels.Add(id);
            }
        }

        Debug.Log($"[Player] Прогресс загружен: Gold={Gold}, XP={XP}, Level={AccountLevel}, Пройдено={_completedLevels.Count} [{string.Join(", ", _completedLevels)}]", this);
    }

    private void Save()
    {
        PlayerPrefs.SetInt(KEY_GOLD, Gold);
        PlayerPrefs.SetInt(KEY_XP, XP);
        PlayerPrefs.SetInt(KEY_ACC_LEVEL, AccountLevel);
        PlayerPrefs.SetString(KEY_COMPLETED, string.Join(",", _completedLevels));
        PlayerPrefs.Save();
    }

    // ─── Публичные методы ─────────────────────────────────────────

    /// <summary>Добавляет золото и сохраняет.</summary>
    public void AddGold(int amount)
    {
        if (amount == 0) return;
        Gold = Mathf.Max(0, Gold + amount);
        Save();
        OnDataChanged?.Invoke();
        Debug.Log($"[Player] {(amount > 0 ? "+" : "")}{amount} gold. Всего: {Gold}", this);
    }

    /// <summary>Добавляет опыт и сохраняет. Проверяет level-up.</summary>
    public void AddXP(int amount)
    {
        if (amount <= 0) return;
        XP += amount;

        // Проверка level-up (формула: XPToNext = 100 × AccountLevel)
        while (XP >= GetXPForNextLevel())
        {
            XP -= GetXPForNextLevel();
            AccountLevel++;
            Debug.Log($"[Player] LEVEL UP! Теперь уровень {AccountLevel}", this);

            OnLevelUp?.Invoke(AccountLevel);
        }

        Save();
        OnDataChanged?.Invoke();
    }

    /// <summary>Сколько XP нужно с текущего уровня до следующего.</summary>
    public int GetXPForNextLevel() => 100 * AccountLevel;

    /// <summary>Помечает уровень как пройденный.</summary>
    public void MarkLevelComplete(string levelId)
    {
        if (string.IsNullOrEmpty(levelId))
        {
            Debug.LogError("[Player] MarkLevelComplete: levelId пустой!", this);
            return;
        }

        bool isNew = _completedLevels.Add(levelId);
        Save();
        OnDataChanged?.Invoke();

        if (isNew)
            Debug.Log($"[Player] Уровень '{levelId}' пройден впервые! Всего пройдено: {_completedLevels.Count}", this);
        else
            Debug.Log($"[Player] Уровень '{levelId}' уже был пройден ранее", this);
    }

    /// <summary>Пройден ли уровень?</summary>
    public bool IsLevelCompleted(string levelId)
    {
        return _completedLevels.Contains(levelId);
    }

    /// <summary>
    /// Разблокирован ли уровень? Уровень доступен если это первый в биоме
    /// или предыдущий уже пройден.
    /// </summary>
    public bool IsLevelUnlocked(BiomeDataSO biome, int levelIndex)
    {
        if (biome == null || levelIndex < 0 || levelIndex >= biome.LevelCount)
            return false;

        if (levelIndex == 0) return true;

        // Предыдущий уровень пройден?
        string prevId = biome.GetLevelId(levelIndex - 1);
        return IsLevelCompleted(prevId);
    }

    // ─── Отладка ──────────────────────────────────────────────────

    [ContextMenu("Сбросить весь прогресс")]
    public void ResetProgress()
    {
        Gold = 0;
        XP = 0;
        AccountLevel = 1;
        _completedLevels.Clear();
        Save();
        OnDataChanged?.Invoke();
        Debug.Log("[Player] Прогресс полностью сброшен", this);
    }

    [ContextMenu("Добавить 100 gold")]
    public void Debug_AddGold100() => AddGold(100);

    [ContextMenu("Добавить 50 XP")]
    public void Debug_AddXP50() => AddXP(50);
}
