using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Компонент карточки артефакта в инвентаре/слоте экипировки.
/// Принимает ArtifactDefinitionSO и автоматически окрашивает рамку и фон
/// в цвет редкости. Для Legendary добавляется анимация мерцания.
/// </summary>
[RequireComponent(typeof(Button))]
public class ArtifactSlotUI : MonoBehaviour
{
    [Header("UI-элементы слота")]
    [SerializeField] private Image _borderImage;      // Image рамки (Frame объект)
    [SerializeField] private Image _backgroundImage;  // Image фона (опционально)
    [SerializeField] private Image _iconImage;         // Иконка артефакта (опционально)
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _rarityText;

    [Header("Спрайты рамок по редкости")]
    [SerializeField] private Sprite _frameCommon;     // Серая рамка
    [SerializeField] private Sprite _frameRare;       // Синяя рамка
    [SerializeField] private Sprite _frameEpic;       // Фиолетовая рамка
    [SerializeField] private Sprite _frameLegendary;  // Золотая рамка

    [Header("Смещение рамки по Y (пиксели)")]
    [SerializeField] private float _commonFrameYOffset    =   0f; // Common: сдвиг вниз
    [SerializeField] private float _rareFrameYOffset      =   0f;
    [SerializeField] private float _epicFrameYOffset      =   0f;
    [SerializeField] private float _legendaryFrameYOffset =   0f;

    [Header("Анимация Legendary")]
    [SerializeField] private Image _glintImage;             // маленький блик (дочерний объект Frame)
    [SerializeField] private float _glintSpeed          = 0.4f; // оборотов в секунду
    [SerializeField] private float _legendaryPulseSpeed = 3f;   // мигание самого блика
    [SerializeField] private float _legendaryPulseMin   = 0.5f;
    [SerializeField] private float _legendaryPulseMax   = 1.0f;

    private ArtifactRarity _rarity;
    private bool _isLegendary;

    // ─────────────────────────────────────────────────────────────────
    /// <summary>
    /// Настраивает слот под конкретный артефакт: спрайт рамки, цвет, иконка, текст.
    /// </summary>
    public void Setup(ArtifactDefinitionSO artifact)
    {
        if (artifact == null)
        {
            SetEmpty();
            return;
        }

        _rarity      = artifact.rarity;
        _isLegendary = _rarity == ArtifactRarity.Legendary;

        // -- Спрайт рамки --
        if (_borderImage != null)
        {
            Sprite frameSprite = GetFrameSprite(_rarity);
            if (frameSprite != null)
            {
                // Есть кастомный спрайт - используем его, цвет белый чтобы не перекрасить
                _borderImage.sprite  = frameSprite;
                _borderImage.color   = Color.white;
            }
            else
            {
                // Нет спрайта - откат к цветной заливке
                _borderImage.sprite = null;
                _borderImage.color  = RarityColorHelper.GetBorderColor(_rarity);
            }
            _borderImage.enabled = true;
        }

        // -- Фон --
        if (_backgroundImage != null)
            _backgroundImage.color = RarityColorHelper.GetBackgroundColor(_rarity);

        // -- Иконка артефакта --
        if (_iconImage != null)
        {
            _iconImage.enabled = artifact.icon != null;
            if (artifact.icon != null)
                _iconImage.sprite = artifact.icon;
        }

        // -- Смещение рамки по Y --
        ApplyFrameOffset();

        // -- Текст --
        if (_nameText != null)
            _nameText.text = artifact.artifactName;

        if (_rarityText != null)
        {
            _rarityText.text  = RarityColorHelper.GetRarityLabel(_rarity);
            _rarityText.color = RarityColorHelper.GetBorderColor(_rarity);
        }

        {}
    }

    /// <summary>
    /// Устанавливает слот в пустое состояние (нет артефакта).
    /// </summary>
    public void SetEmpty(string slotLabel = "Пусто")
    {
        _rarity      = ArtifactRarity.Common;
        _isLegendary = false;

        if (_borderImage != null)
        {
            _borderImage.sprite  = null;
            _borderImage.color   = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }
        if (_backgroundImage != null) _backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.1f);
        if (_iconImage       != null) _iconImage.enabled      = false;
        if (_nameText        != null) _nameText.text          = slotLabel;
        if (_rarityText      != null) _rarityText.text        = "";
    }

    // ─────────────────────────────────────────────────────────────────
    /// <summary>
    /// Сдвигает рамку по Y в зависимости от редкости.
    /// Положительное значение = вверх, отрицательное = вниз.
    /// </summary>
    private void ApplyFrameOffset()
    {
        if (_borderImage == null) return;

        float yOffset = _rarity switch
        {
            ArtifactRarity.Common    => _commonFrameYOffset,
            ArtifactRarity.Rare      => _rareFrameYOffset,
            ArtifactRarity.Epic      => _epicFrameYOffset,
            ArtifactRarity.Legendary => _legendaryFrameYOffset,
            _                        => 0f,
        };

        RectTransform rt = _borderImage.rectTransform;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, yOffset);
    }

    // ─────────────────────────────────────────────────────────────────
    /// <summary>
    /// Возвращает спрайт рамки для редкости. Null если поле не заполнено.
    /// </summary>
    private Sprite GetFrameSprite(ArtifactRarity rarity)
    {
        return rarity switch
        {
            ArtifactRarity.Common    => _frameCommon,
            ArtifactRarity.Rare      => _frameRare,
            ArtifactRarity.Epic      => _frameEpic,
            ArtifactRarity.Legendary => _frameLegendary,
            _                        => null,
        };
    }

    // ─────────────────────────────────────────────────────────────────
    // Блик бежит по периметру рамки по часовой стрелке.
    // Ранний выход если не Legendary — не нагружаем Update().
    private void Update()
    {
        if (!_isLegendary || _glintImage == null || _borderImage == null) return;

        // Получаем реальный размер рамки в пикселях
        Rect rect = _borderImage.rectTransform.rect;
        float w = rect.width;
        float h = rect.height;
        if (w <= 0 || h <= 0) return;

        // t = 0..1 за один оборот
        float t     = Mathf.Repeat(Time.time * _glintSpeed, 1f);
        float perim = 2f * (w + h);
        float d     = t * perim;

        float hw = w * 0.5f;
        float hh = h * 0.5f;

        // Позиция вдоль периметра (по часовой: верх → право → низ → лево)
        Vector2 pos;
        float angle;

        if (d < w)                    // верхний край: слева → направо
        {
            pos   = new Vector2(-hw + d, hh);
            angle = 0f;
        }
        else if (d < w + h)           // правый край: сверху → вниз
        {
            pos   = new Vector2(hw, hh - (d - w));
            angle = -90f;
        }
        else if (d < 2f * w + h)      // нижний край: справа → налево
        {
            pos   = new Vector2(hw - (d - w - h), -hh);
            angle = 180f;
        }
        else                          // левый край: снизу → вверх
        {
            pos   = new Vector2(-hw, -hh + (d - 2f * w - h));
            angle = 90f;
        }

        _glintImage.rectTransform.anchoredPosition = pos;
        _glintImage.rectTransform.localRotation    = Quaternion.Euler(0f, 0f, angle);

        // Небольшое мигание блика
        float pulse = (Mathf.Sin(Time.time * _legendaryPulseSpeed * 3f) + 1f) * 0.5f;
        Color c = _glintImage.color;
        c.a = Mathf.Lerp(0.5f, 1f, pulse);
        _glintImage.color = c;
    }
}


