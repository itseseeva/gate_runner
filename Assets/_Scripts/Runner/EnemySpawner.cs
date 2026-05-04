using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Расставляет волны врагов и босса по уровню заранее.
/// Враги стоят на месте и ждут отряд.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Враги")]
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private GameObject _bossPrefab;

    [Header("Настройки уровня")]
    [SerializeField] private int   _waveCount      = 3;   // количество волн
    [SerializeField] private int   _enemiesPerWave  = 4;   // врагов в волне
    [SerializeField] private float _waveSpacing     = 15f; // расстояние между волнами
    [SerializeField] private float _firstWaveZ      = 30f; // Z первой волны
    [SerializeField] private float _enemySpreadX    = 1.5f; // разброс по X

    private void Start()
    {
        SpawnWaves();
        SpawnBoss();
    }

    private void SpawnWaves()
    {
        for (int w = 0; w < _waveCount; w++)
        {
            float waveZ = _firstWaveZ + w * _waveSpacing;

            for (int i = 0; i < _enemiesPerWave; i++)
            {
                float totalWidth = (_enemiesPerWave - 1) * _enemySpreadX;
                float x = -totalWidth / 2f + i * _enemySpreadX;

                GameObject go = Instantiate(_enemyPrefab,
                    new Vector3(x, 1f, waveZ),
                    Quaternion.identity);

                Debug.Log($"[EnemySpawner] Волна {w+1}, враг {i+1} на Z={waveZ}", this);
            }
        }
    }

    private void SpawnBoss()
    {
        float bossZ = _firstWaveZ + _waveCount * _waveSpacing + 10f;
        Instantiate(_bossPrefab,
            new Vector3(0f, 1.5f, bossZ),
            Quaternion.identity);
        Debug.Log($"[EnemySpawner] Босс заспавнен на Z={bossZ}", this);
    }
}
