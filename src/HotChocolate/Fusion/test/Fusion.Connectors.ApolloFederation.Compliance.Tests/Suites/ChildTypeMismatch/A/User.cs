namespace HotChocolate.Fusion.Suites.ChildTypeMismatch.A;

/// <summary>
/// The <c>User</c> type as projected by the <c>a</c> subgraph.
/// This subgraph does not declare <c>@key</c> on User; it only
/// contributes the nullable shareable <c>id: ID</c> field from the official fixture.
/// </summary>
public sealed class User
{
    public string Id { get; init; } = default!;
}
