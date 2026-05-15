using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Всплывашка "Уровень 2!" при level-up аккаунта.
/// Используется на сцене MainMenu (и где угодно где есть Canvas).
///
/// Поведение:
/// 1. При вызове Show() — масштаб 0 → 1.2 → 1 (easeOutBack)
/// 2. Альфа 0 → 1
/// 3. Пауза 1.5 сек
/// 4. Альфа 1 → 0, scale 1 → 0.8
/// 5. Скрытие
/// </summary>
public class LevelUpPopup : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup      _canvasGroup;
    [SerializeField] private RectTransform    _content;
    [SerializeField] private TextMeshProUGUI  _levelLabel;

    [Header("Анимация")]
    [SerializeField] private float _appearDuration  = 0.4f;
    [SerializeField] private float _holdDuration    = 1.5f;
    [SerializeField] private float _disappearDuration = 0.3f;

    private void Awake()
    {
        // По умолчанию скрыто
        if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        if (_content != null)     _content.localScale = Vector3.zero;
    }


    /// <summary>Показывает всплывашку с новым уровнем.</summary>
    public void Show(int newLevel)
    {
        if (_levelLabel != null)
            _levelLabel.text = $"Уровень {newLevel}!";

        // Гасим любые предыдущие анимации
        if (_canvasGroup != null) _canvasGroup.DOKill();
        if (_content != null)     _content.DOKill();

        // Сбрасываем стартовое состояние
        if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        if (_content != null)     _content.localScale = Vector3.zero;

        // Sequence — последовательность анимаций
        Sequence seq = DOTween.Sequence();

        // Появление: scale + fade in одновременно
        if (_content != null)
            seq.Join(_content.DOScale(1f, _appearDuration).SetEase(Ease.OutBack));

        if (_canvasGroup != null)
            seq.Join(_canvasGroup.DOFade(1f, _appearDuration));

        // Пауза
        seq.AppendInterval(_holdDuration);

        // Скрытие
        if (_canvasGroup != null)
            seq.Append(_canvasGroup.DOFade(0f, _disappearDuration));

        if (_content != null)
            seq.Join(_content.DOScale(0.8f, _disappearDuration).SetEase(Ease.InQuad));
    }
}
