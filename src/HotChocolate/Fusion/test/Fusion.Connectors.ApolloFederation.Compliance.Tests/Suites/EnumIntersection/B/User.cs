namespace HotChocolate.Fusion.Suites.EnumIntersection.B;

/// <summary>
/// The <c>User</c> entity as projected by the <c>b</c> subgraph.
/// </summary>
public sealed class User
{
    public string? Id { get; init; }

    public UserTypeEnum? Type { get; init; }
}
