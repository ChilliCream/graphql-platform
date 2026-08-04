namespace HotChocolate.Fusion.Suites.EnumIntersection.B;

/// <summary>
/// The <c>UserType</c> enum as projected by the <c>b</c> subgraph.
/// <c>ANONYMOUS</c> is marked <c>@inaccessible</c> so the supergraph
/// only exposes <c>REGULAR</c>.
/// </summary>
public enum UserTypeEnum
{
    ANONYMOUS,
    REGULAR
}
