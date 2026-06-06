using UnityEngine;

/// <summary>
/// Данные врага. Один файл SO на каждый тип врага.
/// Пример: Enemy_Basic_Data, Enemy_Elite_Data, Boss_Data.
/// </summary>
[CreateAssetMenu(fileName = "Enemy_Data", menuName = "MGR/Enemy Definition")]
public class EnemyDefinitionSO : ScriptableObject
{
    [Header("Основные параметры")]
    public string EnemyName   = "Basic Enemy";
    public int    MaxHP        = 50;

    [Header("Атака")]
    // TODO: заменить на Remote Config когда подключим LiveOps
    public int   Damage        = 10;
    public float AttackRange   = 1.5f;
    public float AttackSpeed   = 1f;   // атак в секунду

    [Header("Спавн")]
    [Tooltip("Высота (Y), на которой враг появляется. Подбери так, чтобы снаряд попадал в тело.")]
    public float SpawnHeight   = 0.5f;
}
