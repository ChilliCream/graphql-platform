namespace HotChocolate.Fusion.Suites.ChildTypeMismatch.B;

/// <summary>
/// The <c>User</c> entity as projected by the <c>b</c> subgraph
/// (<c>@key(fields: "id")</c>). Owns <c>id</c>, <c>name</c>, and
/// <c>similarAccounts</c>.
/// </summary>
public sealed class User
{
    public string Id { get; init; } = default!;

    public string? Name { get; init; }
}
