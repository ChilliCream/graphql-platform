namespace HotChocolate.Fusion.Suites.ChildTypeMismatch.B;

/// <summary>
/// The <c>Admin</c> type in the <c>b</c> subgraph. Not an entity (no @key).
/// <c>name</c> is <c>@shareable</c>. The original SDL uses nullable <c>id: ID</c>,
/// but HotChocolate requires matching nullability across union members for
/// field selection merging, so <c>id</c> is declared <c>ID!</c> here.
/// </summary>
public sealed class Admin
{
    public string Id { get; init; } = default!;

    public string? Name { get; init; }
}
