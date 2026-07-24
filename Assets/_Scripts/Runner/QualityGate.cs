using UnityEngine;

/// <summary>
/// Ворота качества: повышает ранг всех юнитов.
/// Пока просто логирует — ранги добавим позже.
/// </summary>
public class QualityGate : BaseGate
{
    protected override string GetLabel() => "UPGRADE";

    protected override void ApplyEffect(SquadController squad)
    {
        // TODO: повысить ранг юнитов когда добавим систему рангов
        {}
    }
}
