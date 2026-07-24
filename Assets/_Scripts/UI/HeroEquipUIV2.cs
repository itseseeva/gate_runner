using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Новый экран героев с экипировкой (V2).
/// Структура: roster слева, герой+слоты в центре, статы/инвентарь справа, скиллы снизу.
/// </summary>
public class HeroEquipUIV2 : MonoBehaviour
{
    [Header("Главная панель")]
    [SerializeField] private GameObject _screen;

    [Header("Зоны экрана")]
    [SerializeField] private Transform _rosterPanel;
    [SerializeField] private Transform _leftSlotsParent;
    [SerializeField] private Transform _rightSlotsParent;
    [SerializeField] private Transform _skillsBar;

    [Header("Правая панель — режимы")]
    [SerializeField] private GameObject _statsView;
    [SerializeField] private GameObject _inventoryView;
    [SerializeField] private GameObject _artifactInfoView;

    [Header("Правая панель — инфа артефакта")]
    [SerializeField] private Image _artifactIcon;
    [SerializeField] private TextMeshProUGUI _artifactNameText;
    [SerializeField] private TextMeshProUGUI _rarityText;
    [SerializeField] private TextMeshProUGUI _bonusesText;
    [SerializeField] private Button _equipButton;
    [SerializeField] private Button _upgradeButton;

    [Header("Правая панель — статы")]
    [SerializeField] private TextMeshProUGUI _heroNameText;
    [SerializeField] private TextMeshProUGUI _statsText;

    [Header("Правая панель — инвентарь")]
    [SerializeField] private Transform _inventoryGrid;

    [Header("Префабы")]
    [SerializeField] private GameObject _itemElementPrefab;
    [SerializeField] private GameObject _heroButtonPrefab;
    [SerializeField] private GameObject _skillButtonPrefab;

    [Header("Данные")]
    [SerializeField] private HeroDefinitionSO[] _allHeroes;
    [SerializeField] private List<ArtifactDefinitionSO> _allArtifacts;

    [Header("Кнопки")]
    [SerializeField] private Button _equipBestButton;

    [Header("Upgrade View — панель прокачки (верхняя)")]
    [SerializeField] private GameObject _upgradeView;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private Slider _xpBar;
    [SerializeField] private TextMeshProUGUI _xpText;
    [SerializeField] private TextMeshProUGUI _statPointsText;
    [SerializeField] private Transform _statRowsParent;
    [SerializeField] private GameObject _statRowPrefab;
    [SerializeField] private Button _toggleUpgradeButton;
    [SerializeField] private Button _giveXPTestButton;
    [SerializeField] private Button _acceptButton;
    [SerializeField] private Button _resetButton;
    [SerializeField] private Button _fullResetButton;

    [Header("Stats Panel — итоговые статы (нижняя)")]
    [Tooltip("Отдельная панель снизу — дочерний объект RightPanel, НЕ UpgradeView")]
    [SerializeField] private GameObject _derivedStatsPanel;
    [SerializeField] private TextMeshProUGUI _derivedStatsText;

    [Header("Попап скилла")]
    [SerializeField] private GameObject _skillPopup;
    [SerializeField] private TextMeshProUGUI _skillNameText;
    [SerializeField] private TextMeshProUGUI _skillDescriptionText;
    [SerializeField] private Button _popupCloseButton;

    private int _currentHeroIndex = 0;
    private bool _upgradeViewActive = false;
    private TempStatAllocation _tempAllocation = new TempStatAllocation();

    [Header("Layout — разделение правой панели")]
    [Tooltip("Где делится панель: 0 = всё внизу, 1 = всё вверх. 0.35 = 65% прокачка / 35% статы")]
    [Range(0.2f, 0.6f)]
    [SerializeField] private float _upgradeSplitY = 0.35f;

    void Start()
    {
        _screen.SetActive(false);
        if (_skillPopup != null) _skillPopup.SetActive(false);
        if (_popupCloseButton != null)
            _popupCloseButton.onClick.AddListener(() => _skillPopup.SetActive(false));
        if (_equipBestButton != null)
            _equipBestButton.onClick.AddListener(EquipBest);
        if (_upgradeView != null) _upgradeView.SetActive(false);
        if (_derivedStatsPanel != null) _derivedStatsPanel.SetActive(false);
        if (_toggleUpgradeButton != null)
            _toggleUpgradeButton.onClick.AddListener(ToggleUpgradeView);
        if (_giveXPTestButton != null)
            _giveXPTestButton.onClick.AddListener(GiveTestXP);
        if (_acceptButton != null)
            _acceptButton.onClick.AddListener(AcceptStatChanges);
        if (_resetButton != null)
            _resetButton.onClick.AddListener(ResetStatChanges);
        if (_fullResetButton != null)
            _fullResetButton.onClick.AddListener(FullResetStats);

        ApplySplitLayout();
    }

    [ContextMenu("Apply Split Layout")]
    private void ApplySplitLayout()
    {
        if (_upgradeView != null)
        {
            RectTransform rt = _upgradeView.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, _upgradeSplitY);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            {}
        }

        if (_derivedStatsPanel != null)
        {
            RectTransform rt = _derivedStatsPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, _upgradeSplitY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            {}
        }
    }

    public void OpenScreen()
    {
        _screen.SetActive(true);
        Refresh();
        ShowStats();
        {}
    }

    public void CloseScreen()
    {
        _screen.SetActive(false);
        {}
    }

    public void SelectHero(int index)
    {
        if (index < 0 || index >= _allHeroes.Length) return;
        _currentHeroIndex = index;
        Refresh();
        ShowStats();
    }

    private void Refresh()
    {
        if (_allHeroes == null || _allHeroes.Length == 0)
        {
            {}
            return;
        }

        HeroDefinitionSO hero = _allHeroes[_currentHeroIndex];
        string heroId = hero.HeroName;
        HeroSaveData saveData = SaveSystem.Instance.GetOrCreateHeroData(heroId);

        RefreshRoster();
        RefreshSlots(heroId, saveData);
        RefreshStats(hero, saveData);
        RefreshInventory(heroId);
        RefreshSkills(hero);
    }

    private void RefreshRoster()
    {
        foreach (Transform child in _rosterPanel)
            Destroy(child.gameObject);

        for (int i = 0; i < _allHeroes.Length; i++)
        {
            int indexCopy = i;
            HeroDefinitionSO hero = _allHeroes[i];
            GameObject btn = Instantiate(_heroButtonPrefab, _rosterPanel);
            TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = hero.HeroName;
            btn.GetComponent<Button>().onClick.AddListener(() => SelectHero(indexCopy));
        }
    }

    private void RefreshSlots(string heroId, HeroSaveData saveData)
    {
        foreach (Transform child in _leftSlotsParent) Destroy(child.gameObject);
        foreach (Transform child in _rightSlotsParent) Destroy(child.gameObject);

        for (int i = 0; i < 6; i++)
        {
            Transform parent = i < 3 ? _leftSlotsParent : _rightSlotsParent;
            bool hasArtifact = i < saveData.equippedArtifactIds.Count;
            string artifactId = hasArtifact ? saveData.equippedArtifactIds[i] : null;
            GameObject slot = Instantiate(_itemElementPrefab, parent);
            ArtifactSlotUI slotUI = slot.GetComponent<ArtifactSlotUI>();

            if (hasArtifact)
            {
                ArtifactDefinitionSO artifact = FindArtifact(artifactId);
                if (slotUI != null)
                    slotUI.Setup(artifact);
                else if (slot.GetComponentInChildren<TextMeshProUGUI>() is TextMeshProUGUI lbl)
                    lbl.text = artifact != null ? artifact.artifactName : artifactId;

                string idCopy = artifactId;
                string heroIdCopy = heroId;
                slot.GetComponent<Button>().onClick.AddListener(() =>
                {
                    SaveSystem.Instance.UnequipArtifact(heroIdCopy, idCopy);
                    Refresh();
                });
            }
            else
            {
                if (slotUI != null)
                    slotUI.SetEmpty($"Слот {i + 1}");
                else if (slot.GetComponentInChildren<TextMeshProUGUI>() is TextMeshProUGUI lbl)
                    lbl.text = $"Слот {i + 1}";
                slot.GetComponent<Button>().onClick.AddListener(ShowInventory);
            }
        }
    }

    private void RefreshStats(HeroDefinitionSO hero, HeroSaveData saveData)
    {
        if (_heroNameText != null)
            _heroNameText.text = hero.HeroName;

        if (_statsText != null)
        {
            HeroStats s = HeroStatsCalculator.Compute(hero, saveData);
            _statsText.text =
                $"Уровень: {saveData.level}\n" +
                $"──────────────\n" +
                $"HP:       {s.MaxHP}\n" +
                $"Мана:     {s.MaxMana}\n" +
                $"Физ.ATK:  {s.PhysATK}\n" +
                $"Маг.ATK:  {s.MagATK}\n" +
                $"Защита:   {s.DEF}\n" +
                $"Скорость: {s.Speed:F0}\n" +
                $"Крит:     {s.CritChance:F1}%\n" +
                $"Сопр.:    {s.Resistance:F0}";
        }
    }

    private void RefreshInventory(string heroId)
    {
        if (_inventoryGrid == null) return;
        foreach (Transform child in _inventoryGrid) Destroy(child.gameObject);
        List<string> freeIds = SaveSystem.Instance.GetFreeArtifacts();

        if (freeIds.Count == 0)
        {
            GameObject empty = Instantiate(_itemElementPrefab, _inventoryGrid);
            empty.GetComponentInChildren<TextMeshProUGUI>().text = "Инвентарь пуст";
            empty.GetComponent<Button>().interactable = false;
            return;
        }

        foreach (string artifactId in freeIds)
        {
            ArtifactDefinitionSO artifact = FindArtifact(artifactId);
            string displayName = artifact != null ? artifact.artifactName : artifactId;
            string idCopy = artifactId;
            string heroIdCopy = heroId;
            GameObject card = Instantiate(_itemElementPrefab, _inventoryGrid);
            ArtifactSlotUI slotUI = card.GetComponent<ArtifactSlotUI>();
            if (slotUI != null)
                slotUI.Setup(artifact);
            else if (card.GetComponentInChildren<TextMeshProUGUI>() is TextMeshProUGUI lbl)
                lbl.text = displayName;
            card.GetComponent<Button>().onClick.AddListener(() =>
            {
                ShowArtifactInfo(idCopy, heroIdCopy);
            });
        }
    }

    public void ShowStats()
    {
        if (_inventoryView != null) _inventoryView.SetActive(true);
        if (_statsView != null) _statsView.SetActive(true);
        if (_artifactInfoView != null) _artifactInfoView.SetActive(false);
        if (_upgradeView != null) _upgradeView.SetActive(false);
        if (_derivedStatsPanel != null) _derivedStatsPanel.SetActive(false);
        _upgradeViewActive = false;
    }

    public void ShowInventory()
    {
        ShowStats();
    }

    private ArtifactDefinitionSO FindArtifact(string artifactId)
    {
        for (int i = 0; i < _allArtifacts.Count; i++)
            if (_allArtifacts[i].artifactName == artifactId)
                return _allArtifacts[i];
        return null;
    }

    private void ShowArtifactInfo(string artifactId, string heroId)
    {
        ArtifactDefinitionSO artifact = FindArtifact(artifactId);
        if (artifact == null) return;

        if (_statsView != null) _statsView.SetActive(false);
        if (_artifactInfoView != null) _artifactInfoView.SetActive(true);
        if (_artifactIcon != null && artifact.icon != null)
            _artifactIcon.sprite = artifact.icon;
        if (_artifactNameText != null)
            _artifactNameText.text = artifact.artifactName;
        if (_rarityText != null)
        {
            _rarityText.text = RarityColorHelper.GetRarityLabel(artifact.rarity);
            _rarityText.color = RarityColorHelper.GetBorderColor(artifact.rarity);
        }
        if (_bonusesText != null)
            _bonusesText.text =
                $"Атака: +{artifact.bonusAttack}\n" +
                $"Защита: +{artifact.bonusDefense}\n" +
                $"HP: +{artifact.bonusHP}";

        if (_equipButton != null)
        {
            _equipButton.onClick.RemoveAllListeners();
            _equipButton.onClick.AddListener(() =>
            {
                if (SaveSystem.Instance.EquipArtifact(heroId, artifactId))
                {
                    Refresh();
                    ShowStats();
                }
            });
        }

        if (_upgradeButton != null)
        {
            _upgradeButton.onClick.RemoveAllListeners();
            _upgradeButton.onClick.AddListener(() =>
            {
                {}
            });
        }
    }

    private void RefreshSkills(HeroDefinitionSO hero)
    {
        // TODO: Переделать под систему BaseSpell для раннера
        // Пока скиллы отключены — старая система AbilityEffect не перенесена
        if (_skillsBar == null) return;
        foreach (Transform child in _skillsBar)
            Destroy(child.gameObject);
    }

//     private void ShowSkillPopup(AbilityEffect ability)
//     {
//         if (_skillPopup == null) return;
//         _skillPopup.SetActive(true);
//         if (_skillNameText != null)
//             _skillNameText.text = ability.AbilityName;
//         if (_skillDescriptionText != null)
//             _skillDescriptionText.text = ability.Description;
//     }

    public void EquipBest()
    {
        if (_allHeroes == null || _allHeroes.Length == 0) return;
        HeroDefinitionSO hero = _allHeroes[_currentHeroIndex];
        string heroId = hero.HeroName;
        HeroSaveData saveData = SaveSystem.Instance.GetOrCreateHeroData(heroId);

        List<string> pool = new List<string>(saveData.equippedArtifactIds);
        pool.AddRange(SaveSystem.Instance.GetFreeArtifacts());
        List<ArtifactDefinitionSO> candidates = new List<ArtifactDefinitionSO>();
        for (int i = 0; i < pool.Count; i++)
        {
            ArtifactDefinitionSO art = FindArtifact(pool[i]);
            if (art != null) candidates.Add(art);
        }

        for (int i = 0; i < candidates.Count - 1; i++)
        {
            for (int j = i + 1; j < candidates.Count; j++)
            {
                int powerI = candidates[i].bonusAttack + candidates[i].bonusDefense + candidates[i].bonusHP;
                int powerJ = candidates[j].bonusAttack + candidates[j].bonusDefense + candidates[j].bonusHP;
                if (powerJ > powerI)
                {
                    (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
                }
            }
        }

        SaveSystem.Instance.UnequipAllArtifacts(heroId);
        int equipped = 0;
        for (int i = 0; i < candidates.Count && equipped < 6; i++)
        {
            if (SaveSystem.Instance.EquipArtifact(heroId, candidates[i].artifactName))
                equipped++;
        }

        {}
        Refresh();
        ShowStats();
    }

    public void ToggleUpgradeView()
    {
        _upgradeViewActive = !_upgradeViewActive;

        if (_upgradeView != null)
        {
            Transform rightPanel = _upgradeView.transform.parent;
            foreach (Transform child in rightPanel)
            {
                bool isUpgradePart =
                    child.gameObject == _upgradeView ||
                    (_derivedStatsPanel != null && child.gameObject == _derivedStatsPanel);

                if (_upgradeViewActive)
                    child.gameObject.SetActive(isUpgradePart);
                else
                    child.gameObject.SetActive(!isUpgradePart);
            }
        }

        if (_upgradeViewActive) RefreshUpgradeView();
        {}
    }

    private void RefreshUpgradeView()
    {
        if (_allHeroes == null || _allHeroes.Length == 0) return;
        HeroDefinitionSO hero = _allHeroes[_currentHeroIndex];
        string heroId = hero.HeroName;
        HeroSaveData saveData = SaveSystem.Instance.GetOrCreateHeroData(heroId);
        bool isMax = saveData.level >= HeroStatsCalculator.MaxLevel;

        if (_levelText != null)
            _levelText.text = isMax ? $"Уровень {saveData.level} (MAX)" : $"Уровень {saveData.level}";

        float progress = HeroStatsCalculator.GetLevelProgress(saveData.experience, saveData.level);
        if (_xpBar != null)
        {
            _xpBar.minValue = 0f;
            _xpBar.maxValue = 1f;
            _xpBar.value = progress;
            _xpBar.interactable = false;
        }

        if (_xpText != null)
        {
            if (isMax)
            {
                _xpText.text = "MAX LEVEL";
            }
            else
            {
                int inLevel = HeroStatsCalculator.GetXPWithinLevel(saveData.experience, saveData.level);
                int needed = HeroStatsCalculator.GetXPNeededForCurrentLevel(saveData.level);
                _xpText.text = $"{inLevel} / {needed} XP";
            }
        }

        int usedPoints = _tempAllocation.GetTotal();
        int freePoints = saveData.statPoints - usedPoints;
        if (_statPointsText != null)
            _statPointsText.text = $"Очки: {freePoints}";

        if (_statRowsParent == null || _statRowPrefab == null) return;
        foreach (Transform child in _statRowsParent)
            Destroy(child.gameObject);

        bool canAddPoints = freePoints > 0;
        SpawnStatRow("Сила", "strength", saveData.strength, _tempAllocation.strength, heroId, canAddPoints);
        SpawnStatRow("Ловкость", "agility", saveData.agility, _tempAllocation.agility, heroId, canAddPoints);
        SpawnStatRow("Интеллект", "intellect", saveData.intellect, _tempAllocation.intellect, heroId, canAddPoints);
        SpawnStatRow("Выносливость", "endurance", saveData.endurance, _tempAllocation.endurance, heroId, canAddPoints);

        bool hasChanges = _tempAllocation.GetTotal() > 0;
        if (_acceptButton != null) _acceptButton.interactable = hasChanges;
        if (_resetButton != null) _resetButton.interactable = hasChanges;

        // Кнопка "Полный сброс" активна если есть что сбрасывать (вкачанные статы)
        int totalSpentStats = saveData.strength + saveData.agility +
                              saveData.intellect + saveData.endurance;
        if (_fullResetButton != null) _fullResetButton.interactable = totalSpentStats > 0;

        RefreshDerivedStats(hero, saveData);
    }

    private void SpawnStatRow(string displayName, string statKey, int baseValue,
                              int tempValue, string heroId, bool canAddPoints)
    {
        GameObject row = Instantiate(_statRowPrefab, _statRowsParent);
        StatRowUI rowUI = row.GetComponent<StatRowUI>();
        if (rowUI != null)
        {
            rowUI.Setup(displayName, statKey, baseValue, tempValue, heroId, this);
            rowUI.SetPlusInteractable(canAddPoints);
            rowUI.SetMinusInteractable(tempValue > 0);
        }
    }

    private void RefreshDerivedStats(HeroDefinitionSO hero, HeroSaveData saveData)
    {
        if (_derivedStatsText == null) return;

        HeroSaveData tempSave = new HeroSaveData
        {
            heroId = saveData.heroId,
            level = saveData.level,
            experience = saveData.experience,
            statPoints = saveData.statPoints,
            strength = saveData.strength + _tempAllocation.strength,
            agility = saveData.agility + _tempAllocation.agility,
            intellect = saveData.intellect + _tempAllocation.intellect,
            endurance = saveData.endurance + _tempAllocation.endurance,
            equippedArtifactIds = saveData.equippedArtifactIds
        };

        HeroStats s = HeroStatsCalculator.Compute(hero, tempSave);

        _derivedStatsText.text =
            "<b>── СТАТЫ ──</b>\n" +
            $"HP        <b>{s.MaxHP}</b>\n" +
            $"Мана      <b>{s.MaxMana}</b>\n" +
            $"Физ.ATK   <b>{s.PhysATK}</b>\n" +
            $"Маг.ATK   <b>{s.MagATK}</b>\n" +
            $"Защита    <b>{s.DEF}</b>\n" +
            $"Скорость  <b>{s.Speed:F0}</b>\n" +
            $"Крит      <b>{s.CritChance:F1}%</b>\n" +
            $"Сопр.     <b>{s.Resistance:F0}</b>";
    }

    public void RefreshAfterStatSpend()
    {
        HeroDefinitionSO hero = _allHeroes[_currentHeroIndex];
        HeroSaveData saveData = SaveSystem.Instance.GetOrCreateHeroData(hero.HeroName);
        RefreshStats(hero, saveData);
        RefreshUpgradeView();
    }

    private void GiveTestXP()
    {
        if (_allHeroes == null || _allHeroes.Length == 0) return;
        string heroId = _allHeroes[_currentHeroIndex].HeroName;
        SaveSystem.Instance.AddXP(heroId, 100);
        Refresh();
        if (_upgradeViewActive) RefreshUpgradeView();
        {}
    }

    public void OnStatPlusClicked(string statKey)
    {
        HeroDefinitionSO hero = _allHeroes[_currentHeroIndex];
        HeroSaveData saveData = SaveSystem.Instance.GetOrCreateHeroData(hero.HeroName);
        int usedPoints = _tempAllocation.GetTotal();
        if (saveData.statPoints <= usedPoints)
        {
            {}
            return;
        }

        switch (statKey)
        {
            case "strength": _tempAllocation.strength++; break;
            case "agility": _tempAllocation.agility++; break;
            case "intellect": _tempAllocation.intellect++; break;
            case "endurance": _tempAllocation.endurance++; break;
        }

        {}
        RefreshUpgradeView();
    }

    public void OnStatMinusClicked(string statKey)
    {
        int currentTemp = statKey switch
        {
            "strength" => _tempAllocation.strength,
            "agility" => _tempAllocation.agility,
            "intellect" => _tempAllocation.intellect,
            "endurance" => _tempAllocation.endurance,
            _ => 0
        };

        if (currentTemp <= 0)
        {
            {}
            return;
        }

        switch (statKey)
        {
            case "strength": _tempAllocation.strength--; break;
            case "agility": _tempAllocation.agility--; break;
            case "intellect": _tempAllocation.intellect--; break;
            case "endurance": _tempAllocation.endurance--; break;
        }

        {}
        RefreshUpgradeView();
    }

    private void AcceptStatChanges()
    {
        if (_tempAllocation.GetTotal() <= 0)
        {
            {}
            return;
        }

        HeroDefinitionSO hero = _allHeroes[_currentHeroIndex];
        string heroId = hero.HeroName;
        HeroSaveData saveData = SaveSystem.Instance.GetOrCreateHeroData(heroId);

        if (_tempAllocation.strength > 0)
        {
            saveData.strength += _tempAllocation.strength;
            saveData.statPoints -= _tempAllocation.strength;
        }
        if (_tempAllocation.agility > 0)
        {
            saveData.agility += _tempAllocation.agility;
            saveData.statPoints -= _tempAllocation.agility;
        }
        if (_tempAllocation.intellect > 0)
        {
            saveData.intellect += _tempAllocation.intellect;
            saveData.statPoints -= _tempAllocation.intellect;
        }
        if (_tempAllocation.endurance > 0)
        {
            saveData.endurance += _tempAllocation.endurance;
            saveData.statPoints -= _tempAllocation.endurance;
        }

        SaveSystem.Instance.Save();
        {}

        _tempAllocation.Reset();
        Refresh();
        RefreshUpgradeView();
    }

    /// <summary>
    /// Сбрасывает только временные изменения (которые ещё не нажал Принять).
    /// </summary>
    private void ResetStatChanges()
    {
        if (_tempAllocation.GetTotal() <= 0)
        {
            {}
            return;
        }

        {}

        _tempAllocation.Reset();
        RefreshUpgradeView();
    }

    /// <summary>
    /// ПОЛНЫЙ сброс всех вкачанных статов героя.
    /// Возвращает все потраченные очки обратно.
    /// </summary>
    private void FullResetStats()
    {
        if (_allHeroes == null || _allHeroes.Length == 0) return;

        string heroId = _allHeroes[_currentHeroIndex].HeroName;

        // Очищаем временный буфер если что-то накликано
        _tempAllocation.Reset();

        // Полный сброс через SaveSystem
        SaveSystem.Instance.ResetAllStats(heroId);

        {}

        // Обновляем весь UI
        Refresh();
        RefreshUpgradeView();
    }
}

public class TempStatAllocation
{
    public int strength;
    public int agility;
    public int intellect;
    public int endurance;

    public int GetTotal() => strength + agility + intellect + endurance;

    public void Reset()
    {
        strength = agility = intellect = endurance = 0;
    }
}
    