using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Пул VFX эффектов. Один на сцену.
/// Спавнит эффект, ждёт окончания Particle System, возвращает в пул.
/// </summary>
public class VfxPool : MonoBehaviour
{
    public static VfxPool Instance { get; private set; }

    [SerializeField] private GameObject _prefab;
    [SerializeField] private int _preloadCount = 10;

    private readonly Queue<ParticleSystem> _pool = new();

    private void Awake()
    {
        Instance = this;
        Preload();
    }

    private void Preload()
    {
        for (int i = 0; i < _preloadCount; i++)
        {
            ParticleSystem ps = CreateOne();
            ps.gameObject.SetActive(false);
            _pool.Enqueue(ps);
        }
    }

    private ParticleSystem CreateOne()
    {
        GameObject go = Instantiate(_prefab, transform);
        ParticleSystem ps = go.GetComponent<ParticleSystem>();

        // Отключаем авто-уничтожение — пул сам управляет
        var main = ps.main;
        main.stopAction = ParticleSystemStopAction.None;

        return ps;
    }

    /// <summary>Спавнит VFX в точке. Автоматически возвращает в пул.</summary>
    public void Spawn(Vector3 position, Quaternion rotation)
    {
        ParticleSystem ps;

        if (_pool.Count > 0)
            ps = _pool.Dequeue();
        else
            ps = CreateOne();

        ps.gameObject.SetActive(true);
        ps.transform.position = position;
        ps.transform.rotation = rotation;
        ps.Play();

        StartCoroutine(ReturnWhenDone(ps));
    }

    private IEnumerator ReturnWhenDone(ParticleSystem ps)
    {
        // Ждём пока эффект закончится
        yield return new WaitUntil(() => !ps.IsAlive(true));

        ps.gameObject.SetActive(false);
        _pool.Enqueue(ps);
    }
}
