namespace HotChocolate.Fusion.Suites.ChildTypeMismatch.B;

/// <summary>
/// The <c>Admin</c> type in the <c>b</c> subgraph. Not an entity (no @key).
/// <c>name</c> is <c>@shareable</c>, and <c>id: ID</c> is nullable.
/// </summary>
public sealed class Admin
{
    public string Id { get; init; } = default!;

    public string? Name { get; init; }
}
