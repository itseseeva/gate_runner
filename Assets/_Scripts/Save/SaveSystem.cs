using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance;

    private string _savePath;
    private PlayerSaveData _currentSave;

    void Awake()
    {
        Instance = this;
        _savePath = Application.persistentDataPath + "/save.json";
        Load();
    }

    /// <summary>
    /// Сохраняет данные игрока на диск.
    /// </summary>
    public void Save()
    {
        string json = JsonUtility.ToJson(_currentSave, true);
        File.WriteAllText(_savePath, json);
        Debug.Log($"[Save] Сохранено → {_savePath}");
    }

    /// <summary>
    /// Загружает данные игрока с диска.
    /// </summary>
    public void Load()
    {
        if (File.Exists(_savePath))
        {
            string json = File.ReadAllText(_savePath);
            _currentSave = JsonUtility.FromJson<PlayerSaveData>(json);
            Debug.Log("[Save] Загружено успешно!");
        }
        else
        {
            // Первый запуск — создаём новое сохранение
            _currentSave = CreateNewSave();
            Save();
            Debug.Log("[Save] Новая игра создана!");
        }
    }

    /// <summary>
    /// Создаёт сохранение для новой игры.
    /// </summary>
    private PlayerSaveData CreateNewSave()
    {
        return new PlayerSaveData
        {
            gold = 1000,
            crystals = 50
        };
    }

    public PlayerSaveData GetSaveData() => _currentSave;

    /// <summary>
    /// Добавляет золото и сохраняет.
    /// </summary>
    public void AddGold(int amount)
    {
        _currentSave.gold += amount;
        Save();
        Debug.Log($"[Save] Золото: {_currentSave.gold}");
    }

    /// <summary>
    /// Тратит золото. Возвращает false если не хватает.
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (_currentSave.gold < amount)
        {
            Debug.Log("[Save] Недостаточно золота!");
            return false;
        }
        _currentSave.gold -= amount;
        Save();
        return true;
    }

    /// <summary>
    /// Добавляет кристаллы и сохраняет.
    /// </summary>
    public void AddCrystals(int amount)
    {
        _currentSave.crystals += amount;
        Save();
        Debug.Log($"[Save] Кристаллы: {_currentSave.crystals}");
    }

    /// <summary>
    /// Тратит кристаллы. Возвращает false если не хватает.
    /// </summary>
    public bool SpendCrystals(int amount)
    {
        if (_currentSave.crystals < amount)
        {
            Debug.Log("[Save] Недостаточно кристаллов!");
            return false;
        }
        _currentSave.crystals -= amount;
        Save();
        return true;
    }

    /// <summary>
    /// Добавляет артефакт в инвентарь.
    /// </summary>
    public void AddArtifactToInventory(string artifactId)
    {
        _currentSave.ownedArtifactIds.Add(artifactId);
        Save();
        Debug.Log($"[Save] Артефакт добавлен: {artifactId}");
    }

    /// <summary>
    /// Получить ВСЕ артефакты героев (все что надето).
    /// </summary>
    public List<string> GetAllEquippedArtifacts()
    {
        List<string> equipped = new List<string>();
        foreach (var hero in _currentSave.heroes)
        {
            equipped.AddRange(hero.equippedArtifactIds);
        }
        return equipped;
    }

    /// <summary>
    /// Получить свободные артефакты (в инвентаре, не надетые).
    /// </summary>
    public List<string> GetFreeArtifacts()
    {
        List<string> all = new List<string>(_currentSave.ownedArtifactIds);
        List<string> equipped = GetAllEquippedArtifacts();

        // Убираем из всех те что надеты
        foreach (string id in equipped)
        {
            all.Remove(id);
        }

        return all;
    }

    /// <summary>
    /// Получить сохранённые данные героя по ID. Создаёт если не было.
    /// </summary>
    public HeroSaveData GetOrCreateHeroData(string heroId)
    {
        foreach (var hero in _currentSave.heroes)
        {
            if (hero.heroId == heroId)
                return hero;
        }

        // Создаём нового
        HeroSaveData newHero = new HeroSaveData
        {
            heroId = heroId,
            level = 1
        };
        _currentSave.heroes.Add(newHero);
        Save();
        return newHero;
    }

    /// <summary>
    /// Надевает артефакт на героя.
    /// </summary>
    public bool EquipArtifact(string heroId, string artifactId)
    {
        HeroSaveData hero = GetOrCreateHeroData(heroId);

        // Максимум 6 слотов
        if (hero.equippedArtifactIds.Count >= 6)
        {
            Debug.Log($"[Save] У {heroId} все слоты заняты!");
            return false;
        }

        // Проверяем что артефакт есть в инвентаре и свободен
        if (!GetFreeArtifacts().Contains(artifactId))
        {
            Debug.Log($"[Save] Артефакт {artifactId} недоступен!");
            return false;
        }

        hero.equippedArtifactIds.Add(artifactId);
        Save();
        Debug.Log($"[Save] {heroId} надел: {artifactId}");
        return true;
    }

    /// <summary>
    /// Снимает артефакт с героя (возвращает в инвентарь).
    /// </summary>
    public bool UnequipArtifact(string heroId, string artifactId)
    {
        HeroSaveData hero = GetOrCreateHeroData(heroId);

        if (!hero.equippedArtifactIds.Contains(artifactId))
        {
            Debug.Log($"[Save] У {heroId} нет {artifactId}");
            return false;
        }

        hero.equippedArtifactIds.Remove(artifactId);
        Save();
        Debug.Log($"[Save] {heroId} снял: {artifactId}");
        return true;
    }
    /// <summary>
    /// Снимает все артефакты с героя за один вызов (для "Надеть лучшее").
    /// </summary>
    public void UnequipAllArtifacts(string heroId)
    {
        HeroSaveData hero = GetOrCreateHeroData(heroId);
        hero.equippedArtifactIds.Clear();
        Save();
        Debug.Log($"[Save] {heroId}: все артефакты сняты");
    }

    // ═══════════════════════════════════════════════════════════════
    //  ПРОКАЧКА ГЕРОЯ
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Начисляет опыт герою. Автоматически повышает уровень и даёт
    /// 3 очка статов за каждый новый уровень.
    /// </summary>
    public void AddXP(string heroId, int amount)
    {
        HeroSaveData hero = GetOrCreateHeroData(heroId);

        // Если уже максимальный уровень — XP не начисляем
        if (hero.level >= HeroStatsCalculator.MaxLevel)
        {
            Debug.Log($"[Save] {heroId}: максимальный уровень достигнут!");
            return;
        }

        hero.experience += amount;

        // Проверяем не поднялся ли уровень
        int newLevel = HeroStatsCalculator.GetLevel(hero.experience);
        if (newLevel > hero.level)
        {
            int levelsGained  = newLevel - hero.level;
            int pointsGranted = levelsGained * 3;
            hero.level      = newLevel;
            hero.statPoints += pointsGranted;

            Debug.Log($"[Save] {heroId} ▲ Уровень {newLevel}! " +
                      $"+{pointsGranted} очков статов. " +
                      $"Итого очков: {hero.statPoints}", this);
        }
        else
        {
            Debug.Log($"[Save] {heroId}: +{amount} XP " +
                      $"(итого {hero.experience})", this);
        }

        Save();
    }

    /// <summary>
    /// Тратит 1 очко стата на выбранный стат героя.
    /// statName: "strength" | "agility" | "intellect" | "endurance"
    /// Возвращает true если успешно потрачено.
    /// </summary>
    public bool SpendStatPoint(string heroId, string statName)
    {
        HeroSaveData hero = GetOrCreateHeroData(heroId);

        if (hero.statPoints <= 0)
        {
            Debug.Log($"[Save] {heroId}: нет очков статов!", this);
            return false;
        }

        switch (statName)
        {
            case "strength":  hero.strength++;  break;
            case "agility":   hero.agility++;   break;
            case "intellect": hero.intellect++;  break;
            case "endurance": hero.endurance++; break;
            default:
                Debug.Log($"[Save] Неизвестный стат: {statName}", this);
                return false;
        }

        hero.statPoints--;
        Save();
        Debug.Log($"[Save] {heroId}: +1 {statName} " +
                  $"(осталось очков: {hero.statPoints})", this);
        return true;
    }
    /// <summary>
    /// Возвращает количество свободных очков статов у героя.
    /// </summary>
    public int GetStatPoints(string heroId)
    {
        return GetOrCreateHeroData(heroId).statPoints;
    }

    /// <summary>
    /// Полный сброс всех вкачанных статов героя.
    /// Возвращает все потраченные очки обратно.
    /// </summary>
    public void ResetAllStats(string heroId)
    {
        HeroSaveData hero = GetOrCreateHeroData(heroId);

        // Считаем сколько очков было потрачено
        int spentPoints = hero.strength + hero.agility +
                          hero.intellect + hero.endurance;

        // Возвращаем очки
        hero.statPoints += spentPoints;

        // Обнуляем все статы
        hero.strength  = 0;
        hero.agility   = 0;
        hero.intellect = 0;
        hero.endurance = 0;

        Save();
        Debug.Log($"[Save] {heroId}: ВСЕ СТАТЫ СБРОШЕНЫ! " +
                  $"Возвращено очков: {spentPoints}. " +
                  $"Итого очков: {hero.statPoints}", this);
    }
}