namespace HotChocolate.Fusion.Suites.EnumIntersection.A;

/// <summary>
/// The <c>User</c> entity as projected by the <c>a</c> subgraph.
/// </summary>
public sealed class User
{
    public string? Id { get; init; }

    public UserTypeEnum? Type { get; init; }
}
