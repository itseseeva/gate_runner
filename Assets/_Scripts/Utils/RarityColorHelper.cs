using UnityEngine;

/// <summary>
/// Централизованный источник цветов для каждого уровня редкости.
/// Common — серый, Rare — синий, Epic — фиолетовый, Legendary — золотой.
/// </summary>
public static class RarityColorHelper
{
    // Цвета рамок/фона слотов
    public static readonly Color Common   = new Color(0.55f, 0.55f, 0.55f, 1f); // серый
    public static readonly Color Rare     = new Color(0.20f, 0.55f, 1.00f, 1f); // синий
    public static readonly Color Epic     = new Color(0.65f, 0.20f, 1.00f, 1f); // фиолетовый
    public static readonly Color Legendary= new Color(1.00f, 0.78f, 0.10f, 1f); // золотой

    // Более тёмные варианты для фона (10 % opacity эффект)
    public static readonly Color CommonBg    = new Color(0.55f, 0.55f, 0.55f, 0.15f);
    public static readonly Color RareBg      = new Color(0.20f, 0.55f, 1.00f, 0.15f);
    public static readonly Color EpicBg      = new Color(0.65f, 0.20f, 1.00f, 0.15f);
    public static readonly Color LegendaryBg = new Color(1.00f, 0.78f, 0.10f, 0.15f);

    /// <summary>
    /// Возвращает основной цвет рамки для указанной редкости.
    /// </summary>
    public static Color GetBorderColor(ArtifactRarity rarity)
    {
        return rarity switch
        {
            ArtifactRarity.Common    => Common,
            ArtifactRarity.Rare      => Rare,
            ArtifactRarity.Epic      => Epic,
            ArtifactRarity.Legendary => Legendary,
            _                        => Common,
        };
    }

    /// <summary>
    /// Возвращает цвет фона (полупрозрачный) для указанной редкости.
    /// </summary>
    public static Color GetBackgroundColor(ArtifactRarity rarity)
    {
        return rarity switch
        {
            ArtifactRarity.Common    => CommonBg,
            ArtifactRarity.Rare      => RareBg,
            ArtifactRarity.Epic      => EpicBg,
            ArtifactRarity.Legendary => LegendaryBg,
            _                        => CommonBg,
        };
    }

    /// <summary>
    /// Возвращает локализованное название редкости на русском.
    /// </summary>
    public static string GetRarityLabel(ArtifactRarity rarity)
    {
        return rarity switch
        {
            ArtifactRarity.Common    => "Обычный",
            ArtifactRarity.Rare      => "Редкий",
            ArtifactRarity.Epic      => "Эпический",
            ArtifactRarity.Legendary => "Легендарный",
            _                        => "Неизвестно",
        };
    }
}
