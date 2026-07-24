using UnityEngine;
using System;

/// <summary>
/// Хранение ресурсов базы. Сейчас — Gold (синхронизируется с PlayerDataManager) и Iron.
/// В Дне 8 расширим на 3 ресурса (Food/Gems).
///
/// Singleton, DontDestroyOnLoad. Сохранение через PlayerPrefs.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Production Buff")]
    [Tooltip("Множитель производства когда бафф активен")]
    [SerializeField] private float _buffMultiplier = 2f;

    // Время окончания баффа (UTC ticks)
    private long _buffEndTicks;

    public float BuffMultiplier => _buffMultiplier;
    public bool IsBuffActive => DateTime.UtcNow.Ticks < _buffEndTicks;

    /// <summary>Сколько секунд осталось до конца баффа (0 если неактивен).</summary>
    public float BuffRemainingSeconds
    {
        get
        {
            if (!IsBuffActive) return 0f;
            var end = new DateTime(_buffEndTicks, DateTimeKind.Utc);
            var remaining = end - DateTime.UtcNow;
            return (float)remaining.TotalSeconds;
        }
    }

    private const string KEY_IRON = "mgr_iron";
    private const string KEY_BUFF_END = "mgr_buff_end";

    public int Iron { get; private set; }

    /// <summary>Gold берём из PlayerDataManager — у нас единый источник правды.</summary>
    public int Gold => PlayerDataManager.Instance != null ? PlayerDataManager.Instance.Gold : 0;

    public static event Action OnResourcesChanged;

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

    private void Load()
    {
        Iron = PlayerPrefs.GetInt(KEY_IRON, 0);
        _buffEndTicks = long.Parse(PlayerPrefs.GetString(KEY_BUFF_END, "0"));
        {}
    }

    private void Save()
    {
        PlayerPrefs.SetInt(KEY_IRON, Iron);
        PlayerPrefs.SetString(KEY_BUFF_END, _buffEndTicks.ToString());
        PlayerPrefs.Save();
    }

    // ─── Iron ───────────────────────────────────────────────

    public void AddIron(int amount)
    {
        if (amount <= 0) return;
        Iron += amount;
        Save();
        OnResourcesChanged?.Invoke();
    }

    public bool TrySpendIron(int amount)
    {
        if (Iron < amount) return false;
        Iron -= amount;
        Save();
        OnResourcesChanged?.Invoke();
        return true;
    }

    // ─── Gold (через PlayerDataManager) ─────────────────────

    public bool TrySpendGold(int amount)
    {
        if (Gold < amount) return false;
        // У PlayerDataManager нет TrySpend, поэтому хардкод — вычитаем через AddGold с минусом
        // (или добавим SpendGold в PlayerDataManager позже)
        PlayerDataManager.Instance.AddGold(-amount);
        OnResourcesChanged?.Invoke();
        return true;
    }

    // ─── Проверка можем ли позволить ────────────────────────

    public bool CanAfford(int gold, int iron, int food = 0)
    {
        return Gold >= gold && Iron >= iron;
    }

    /// <summary>Списывает gold + iron. Возвращает true если получилось.</summary>
    public bool TrySpend(int gold, int iron, int food = 0)
    {
        if (!CanAfford(gold, iron, food)) return false;

        if (gold > 0) PlayerDataManager.Instance.AddGold(-gold);
        if (iron > 0) { Iron -= iron; Save(); }

        OnResourcesChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Активирует бафф производства. Если уже активен — продлевает (стек).
    /// </summary>
    public void ActivateProductionBuff(float seconds)
    {
        DateTime newEnd;
        if (IsBuffActive)
        {
            // Продлеваем от текущего конца
            var currentEnd = new DateTime(_buffEndTicks, DateTimeKind.Utc);
            newEnd = currentEnd.AddSeconds(seconds);
        }
        else
        {
            newEnd = DateTime.UtcNow.AddSeconds(seconds);
        }

        _buffEndTicks = newEnd.Ticks;
        Save();
        OnResourcesChanged?.Invoke();

        {}
    }

    // ─── Отладка ────────────────────────────────────────────

    [ContextMenu("Добавить 1000 Iron")]
    public void Debug_AddIron1000() => AddIron(1000);
}
