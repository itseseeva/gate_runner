/// <summary>
/// Направление подхода врага к отряду.
/// Front — прямо по центру, наиболее плотное направление.
/// LeftFlank / RightFlank — заход по бокам, обход.
/// Назначается один раз при спавне, не меняется в течение жизни врага.
/// </summary>
public enum AttackSide
{
    Front,
    LeftFlank,
    RightFlank,
}
