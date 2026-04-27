namespace HotChocolate.Fusion.Suites.SimpleRequiresProvides.Accounts;

/// <summary>
/// The <c>User</c> entity as projected by the <c>accounts</c> subgraph
/// (<c>@key(fields: "id")</c>). Owns <c>name</c> and <c>username</c>
/// (the latter is shareable so other subgraphs may also expose it).
/// </summary>
public sealed class User
{
    public string Id { get; init; } = default!;

    public string? Name { get; init; }

    public string? Username { get; init; }
}
