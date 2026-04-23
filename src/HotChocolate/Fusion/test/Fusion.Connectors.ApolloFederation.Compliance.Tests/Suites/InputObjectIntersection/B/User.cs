namespace HotChocolate.Fusion.Suites.InputObjectIntersection.B;

/// <summary>
/// The <c>User</c> entity in the <c>b</c> subgraph.
/// </summary>
public sealed class User
{
    public string Id { get; init; } = default!;

    public string Name { get; init; } = default!;
}
