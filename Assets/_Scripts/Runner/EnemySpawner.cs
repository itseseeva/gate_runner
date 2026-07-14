using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Расставляет волны врагов и босса по уровню заранее.
/// Враги стоят на месте и ждут отряд.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Враги")]
    [Tooltip("Список префабов врагов. Если здесь несколько, спавнер будет выбирать их случайно.")]
    [SerializeField] private GameObject[] _enemyPrefabs;
    
    [Tooltip("Старое поле. Если массив выше пуст, будет спавниться этот враг.")]
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

                GameObject prefabToSpawn = _enemyPrefab;
                if (_enemyPrefabs != null && _enemyPrefabs.Length > 0)
                {
                    prefabToSpawn = _enemyPrefabs[Random.Range(0, _enemyPrefabs.Length)];
                }

                if (prefabToSpawn != null)
                {
                    Instantiate(prefabToSpawn, new Vector3(x, 0f, waveZ), Quaternion.identity);
                }
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
