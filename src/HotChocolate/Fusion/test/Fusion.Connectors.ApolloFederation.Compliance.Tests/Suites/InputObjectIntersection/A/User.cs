namespace HotChocolate.Fusion.Suites.InputObjectIntersection.A;

/// <summary>
/// The <c>User</c> entity in the <c>a</c> subgraph.
/// </summary>
public sealed class User
{
    public string Id { get; init; } = default!;

    public string Name { get; init; } = default!;
}
