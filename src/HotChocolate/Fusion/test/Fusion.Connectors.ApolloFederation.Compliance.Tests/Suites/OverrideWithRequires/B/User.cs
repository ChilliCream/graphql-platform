namespace HotChocolate.Fusion.Suites.OverrideWithRequires.B;

/// <summary>
/// The <c>User</c> entity as projected by subgraph <c>b</c>: <c>id</c> is the
/// key and <c>name</c> is the canonical override owner (overrides
/// <c>c.name</c>).
/// </summary>
public sealed class User
{
    public string? Id { get; init; }

    public string? Name { get; init; }
}
