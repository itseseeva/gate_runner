using UnityEngine;

/// <summary>
/// Система полос дороги (как в Last War). Делит ширину дороги на N полос,
/// отдаёт X-центр и границы каждой. Единственный источник правды про геометрию полос —
/// спавнер и враги спрашивают отсюда, не зная конкретных координат.
/// TODO: RoadWidth и LaneCount вынести в GameSettingsSO для Remote Config.
/// </summary>
public static class LaneSystem
{
    // Ширина дороги по X (от -RoadWidth/2 до +RoadWidth/2).
    // Согласовано с _roadHalfWidth=2.5 в DecorSpawner → полная ширина 5.
    public static float RoadWidth { get; private set; } = 5f;

    // Сколько полос. 3 = левая / центр / правая, как в Last War.
    public static int LaneCount { get; private set; } = 3;

    /// <summary>Ширина одной полосы.</summary>
    public static float LaneWidth => RoadWidth / LaneCount;

    /// <summary>X-центр полосы по индексу (0 = самая левая).</summary>
    public static float GetLaneCenterX(int laneIndex)
    {
        laneIndex = Mathf.Clamp(laneIndex, 0, LaneCount - 1);
        // Левый край дороги + половина полосы + индекс * ширину полосы.
        float leftEdge = -RoadWidth * 0.5f;
        return leftEdge + LaneWidth * (laneIndex + 0.5f);
    }

    /// <summary>Левая граница полосы по X.</summary>
    public static float GetLaneMinX(int laneIndex) => GetLaneCenterX(laneIndex) - LaneWidth * 0.5f;

    /// <summary>Правая граница полосы по X.</summary>
    public static float GetLaneMaxX(int laneIndex) => GetLaneCenterX(laneIndex) + LaneWidth * 0.5f;

    /// <summary>Случайный индекс полосы — для спавна волн.</summary>
    public static int RandomLane() => Random.Range(0, LaneCount);
}
