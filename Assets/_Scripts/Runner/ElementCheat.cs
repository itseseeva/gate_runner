using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ТЕСТОВЫЙ чит.
/// 1 = Fire, 2 = Ice, 3 = Lightning — ставит стихию отряду И включает постоянную стрельбу.
/// 0 = None — сбрасывает стихию и выключает стрельбу.
/// По умолчанию стрельба ВЫКЛЮЧЕНА.
/// УДАЛИТЬ перед релизом.
/// </summary>
public class ElementCheat : MonoBehaviour
{
    private bool _forceFire = false;

    private void Update()
    {
        HandleElementKeys();

        if (_forceFire)
            ForceRangedFire();
    }

    private void HandleElementKeys()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame)      { ApplyToSquad(ElementType.Fire);      _forceFire = true; }
        else if (kb.digit2Key.wasPressedThisFrame) { ApplyToSquad(ElementType.Ice);       _forceFire = true; }
        else if (kb.digit3Key.wasPressedThisFrame) { ApplyToSquad(ElementType.Lightning); _forceFire = true; }
        else if (kb.digit0Key.wasPressedThisFrame) { ApplyToSquad(ElementType.None);       _forceFire = false; }
    }

    private void ApplyToSquad(ElementType element)
    {
        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (Unit u in units)
        {
            if (u == null || !u.gameObject.activeSelf) continue;
            if (!u.gameObject.scene.IsValid()) continue; // пропускаем префабы-ассеты
            u.SetElement(element);
        }
        {}
    }

    private void ForceRangedFire()
    {
        RangedAutoAttack[] ranged = FindObjectsByType<RangedAutoAttack>(FindObjectsSortMode.None);
        foreach (RangedAutoAttack r in ranged)
        {
            if (r == null || !r.gameObject.activeSelf) continue;
            if (!r.IsReady) continue;

            r.ForceShoot();
        }
    }
}