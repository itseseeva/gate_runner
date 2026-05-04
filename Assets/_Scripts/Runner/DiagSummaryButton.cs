using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Висит на любом объекте сцены. По нажатию L печатает сводку в Console.
/// </summary>
public class DiagSummaryButton : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
        {
            DiagLogger.PrintSummary();
        }
    }

    private void OnApplicationQuit()
    {
        DiagLogger.PrintSummary();
    }
}
