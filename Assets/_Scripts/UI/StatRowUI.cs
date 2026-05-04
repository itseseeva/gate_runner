using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Один ряд стата в панели прокачки.
/// Показывает: "Сила   5  [-] [+]"
/// Кнопка [+] добавляет временное очко, [-] убирает.
/// Изменения применяются только после нажатия кнопки "Принять".
/// </summary>
public class StatRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _valueText;
    [SerializeField] private Button          _minusButton;
    [SerializeField] private Button          _plusButton;

    private string _statKey;
    private string _heroId;
    private HeroEquipUIV2 _parentUI;

    /// <summary>
    /// Заполняет строку данными.
    /// </summary>
    /// <param name="displayName">Отображаемое название (например "Сила")</param>
    /// <param name="statKey">Ключ стата ("strength", "agility", и т.д.)</param>
    /// <param name="baseValue">Сохранённое значение из SaveData</param>
    /// <param name="tempValue">Временное значение из _tempAllocation</param>
    /// <param name="heroId">ID героя</param>
    /// <param name="parentUI">Ссылка на HeroEquipUIV2 для вызова методов</param>
    public void Setup(string displayName, string statKey, int baseValue, 
                      int tempValue, string heroId, HeroEquipUIV2 parentUI)
    {
        _statKey  = statKey;
        _heroId   = heroId;
        _parentUI = parentUI;

        if (_nameText != null) 
            _nameText.text = displayName;

        // Отображаем значение
        if (_valueText != null)
        {
            int totalValue = baseValue + tempValue;
            _valueText.text = totalValue.ToString();
        }

        // Подписываем кнопки
        if (_minusButton != null)
        {
            _minusButton.onClick.RemoveAllListeners();
            _minusButton.onClick.AddListener(OnMinusClicked);
        }

        if (_plusButton != null)
        {
            _plusButton.onClick.RemoveAllListeners();
            _plusButton.onClick.AddListener(OnPlusClicked);
        }
    }

    /// <summary>
    /// Блокирует/разблокирует кнопку [+].
    /// Вызывается когда нет свободных очков.
    /// </summary>
    public void SetPlusInteractable(bool interactable)
    {
        if (_plusButton != null) _plusButton.interactable = interactable;
    }

    /// <summary>
    /// Блокирует/разблокирует кнопку [-].
    /// Вызывается когда нет временных очков для отмены.
    /// </summary>
    public void SetMinusInteractable(bool interactable)
    {
        if (_minusButton != null) _minusButton.interactable = interactable;
    }

    private void OnPlusClicked()
    {
        if (_parentUI != null)
            _parentUI.OnStatPlusClicked(_statKey);
    }

    private void OnMinusClicked()
    {
        if (_parentUI != null)
            _parentUI.OnStatMinusClicked(_statKey);
    }
}