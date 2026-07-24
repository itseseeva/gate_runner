using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

/// <summary>
/// Экран Победы с анимациями DOTween.
/// Поток:
/// 1. Появление панели + текст "Уровень X пройден!"
/// 2. Цифры наград "+N Gold, +N XP"
/// 3. Анимация полоски XP с тикающими цифрами
/// 4. Если был level-up — попап "Уровень N!"
/// 5. Кнопка "Продолжить" доступна по завершении анимации
/// </summary>
public class VictoryUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject       _panel;
    [SerializeField] private Button           _continueButton;
    [SerializeField] private TextMeshProUGUI  _levelLabel;
    [SerializeField] private TextMeshProUGUI  _rewardsLabel;

    [Header("XP-полоска")]
    [SerializeField] private Slider           _xpBar;
    [SerializeField] private TextMeshProUGUI  _xpBarText;
    [SerializeField] private TextMeshProUGUI  _accountLevelText;

    [Header("Level-Up Popup (опционально, на той же сцене)")]
    [SerializeField] private LevelUpPopup _levelUpPopup;

    [Header("Тайминги")]
    [SerializeField] private float _delayBeforeXP = 0.5f;
    [SerializeField] private float _xpFillDuration = 1.5f;

    private void Start()
    {
        if (_panel != null) _panel.SetActive(false);

        if (_continueButton != null)
            _continueButton.onClick.AddListener(OnContinue);

        GameStateManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        GameStateManager.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState newState)
    {
        if (newState != GameState.Victory) return;
        ShowVictory();
    }

    private void ShowVictory()
    {
        if (_panel != null) _panel.SetActive(true);

        var launcher = LevelLauncher.Instance;
        var pdm      = PlayerDataManager.Instance;

        // Защита если играем напрямую со SampleScene
        if (launcher == null || pdm == null || launcher.SelectedBiome == null)
        {
            Debug.LogWarning("[VictoryUI] Нет launcher/pdm — анимации не показываются", this);
            if (_continueButton != null) _continueButton.interactable = true;
            return;
        }

        var biome = launcher.SelectedBiome;
        int idx   = launcher.SelectedLevelIndex;
        int gold  = biome.GetLevelRewardGold(idx);
        int xp    = biome.GetLevelRewardXP(idx);
        int iron  = biome.GetLevelRewardIron(idx);

        // Имя уровня
        if (_levelLabel != null)
            _levelLabel.text = $"{biome.DisplayName} — {biome.GetLevelDisplayName(idx)} пройден!";

        // Запоминаем СТАРЫЕ значения до начисления
        int oldGoldTotal = pdm.Gold;
        int oldIronTotal = ResourceManager.Instance != null ? ResourceManager.Instance.Iron : 0;

        // Начисляем
        pdm.AddGold(gold);
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddIron(iron);
            ResourceManager.Instance.ActivateProductionBuff(60f); // 1 минута
            {}
        }
        else
        {
            Debug.LogError("[VictoryUI] ResourceManager.Instance null!");
        }

        // Анимируем плавный переход от старого к новому
        if (_rewardsLabel != null)
        {
            AnimateRewards(oldGoldTotal, oldGoldTotal + gold,
                           0, xp,  // XP покажем отдельно через прогресс-полоску, тут просто прирост
                           oldIronTotal, oldIronTotal + iron);
        }

        // Кнопку временно отключаем — пока идут анимации
        if (_continueButton != null) _continueButton.interactable = false;

        // Запоминаем XP "до" для анимации полоски
        int oldXP    = pdm.XP;
        int oldLevel = pdm.AccountLevel;

        // Запускаем анимацию полоски XP
        AnimateXPBar(oldXP, oldLevel, xp);
    }

    /// <summary>
    /// Анимирует полоску XP от старого значения к новому.
    /// Если по пути level-up — переключает полоску на новый уровень и показывает попап.
    /// </summary>
    private void AnimateXPBar(int startXP, int startLevel, int xpToAdd)
    {
        var pdm = PlayerDataManager.Instance;
        if (pdm == null) return;

        int    currentLevel = startLevel;
        int    currentXP    = startXP;
        int    xpForLevel   = 100 * currentLevel;

        // Стартовое состояние полоски
        UpdateBarVisual(currentXP, xpForLevel, currentLevel);

        // Sequence для анимации
        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(_delayBeforeXP);

        int xpLeft = xpToAdd;
        while (xpLeft > 0)
        {
            int xpToFill = Mathf.Min(xpLeft, xpForLevel - currentXP);
            int targetXP = currentXP + xpToFill;

            // Локальные копии для замыкания
            int capturedStart = currentXP;
            int capturedEnd   = targetXP;
            int capturedMax   = xpForLevel;
            int capturedLvl   = currentLevel;

            float fillDuration = _xpFillDuration * ((float)xpToFill / xpToAdd);

            seq.Append(DOVirtual.Float(capturedStart, capturedEnd, fillDuration, value =>
            {
                int xpNow = Mathf.RoundToInt(value);
                UpdateBarVisual(xpNow, capturedMax, capturedLvl);
            }));

            currentXP = targetXP;
            xpLeft -= xpToFill;

            // Достигли максимума — level-up
            if (currentXP >= xpForLevel)
            {
                int newLevel = currentLevel + 1;
                seq.AppendCallback(() => ShowLevelUpPopup(newLevel));
                seq.AppendInterval(1.0f); // пауза для попапа

                currentLevel = newLevel;
                currentXP    = 0;
                xpForLevel   = 100 * currentLevel;

                // Сбрасываем полоску визуально на 0
                int capLvl = currentLevel;
                int capMax = xpForLevel;
                seq.AppendCallback(() => UpdateBarVisual(0, capMax, capLvl));
            }
        }

        // В конце анимации — реально начисляем XP
        seq.OnComplete(() =>
        {
            pdm.AddXP(xpToAdd);
            if (_continueButton != null) _continueButton.interactable = true;
        });
    }

    private void UpdateBarVisual(int currentXP, int maxXP, int level)
    {
        if (_xpBar != null)
        {
            _xpBar.maxValue = maxXP;
            _xpBar.value    = currentXP;
        }
        if (_xpBarText != null)
            _xpBarText.text = $"{currentXP} / {maxXP}";
        if (_accountLevelText != null)
            _accountLevelText.text = $"Уровень {level}";
    }

    private void ShowLevelUpPopup(int newLevel)
    {
        if (_levelUpPopup != null)
            _levelUpPopup.Show(newLevel);
        else
            {}
    }

    /// <summary>
    /// Анимирует переход цифр ресурсов от старых значений к новым.
    /// Каждый ресурс получает 0.5 сек на анимацию, идут параллельно.
    /// </summary>
    private void AnimateRewards(int oldGold, int newGold,
                                int oldXP,   int newXP,
                                int oldIron, int newIron)
    {
        if (_rewardsLabel == null) return;

        int curGold = oldGold;
        int curXP   = oldXP;
        int curIron = oldIron;

        // Начальное состояние
        UpdateRewardsText(curGold, curXP, curIron);

        _rewardsLabel.transform.localScale = Vector3.zero;

        Sequence seq = DOTween.Sequence();
        seq.Append(_rewardsLabel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));

        // Все три ресурса анимируются параллельно
        if (newGold != oldGold)
        {
            seq.Join(DOVirtual.Float(oldGold, newGold, 1.5f, val =>
            {
                curGold = Mathf.RoundToInt(val);
                UpdateRewardsText(curGold, curXP, curIron);
            }).SetEase(Ease.Linear));
        }

        if (newXP != oldXP)
        {
            seq.Join(DOVirtual.Float(oldXP, newXP, 1.5f, val =>
            {
                curXP = Mathf.RoundToInt(val);
                UpdateRewardsText(curGold, curXP, curIron);
            }).SetEase(Ease.Linear));
        }

        if (newIron != oldIron)
        {
            seq.Join(DOVirtual.Float(oldIron, newIron, 1.5f, val =>
            {
                curIron = Mathf.RoundToInt(val);
                UpdateRewardsText(curGold, curXP, curIron);
            }).SetEase(Ease.Linear));
        }
    }

    private void UpdateRewardsText(int gold, int xp, int iron)
    {
        if (_rewardsLabel != null)
            _rewardsLabel.text = $"Gold: {gold}\n+{xp} XP\n Iron: {iron}";
    }

    private void OnContinue()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
