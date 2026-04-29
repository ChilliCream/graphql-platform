namespace HotChocolate.Fusion.Suites.Fed2ExternalExtension.A;

/// <summary>
/// The <c>User</c> entity as projected by the <c>a</c> subgraph
/// (<c>extend type User @key(fields: "id")</c>). Subgraph <c>a</c> only
/// owns the <c>rid</c> field; <c>id</c> and <c>name</c> are external.
/// Carries an optional <see cref="Name"/> so the
/// <c>@provides(fields: "name")</c> path on <c>providedRandomUser</c>
/// can ship the value alongside the entity reference.
/// </summary>
public sealed class User
{
    public string Id { get; init; } = default!;

    public string? Rid { get; init; }

    public string? Name { get; init; }
}
