namespace HotChocolate.Fusion.Suites.ChildTypeMismatch.A;

/// <summary>
/// The <c>User</c> type as projected by the <c>a</c> subgraph.
/// This subgraph does not declare <c>@key</c> on User; it only
/// contributes a shareable <c>id</c> field. The original SDL uses
/// nullable <c>id: ID</c>, but HotChocolate's field selection merging
/// requires matching nullability across union members, so <c>id</c>
/// is declared <c>ID!</c> here to match subgraph B's User entity.
/// </summary>
public sealed class User
{
    public string Id { get; init; } = default!;
}
