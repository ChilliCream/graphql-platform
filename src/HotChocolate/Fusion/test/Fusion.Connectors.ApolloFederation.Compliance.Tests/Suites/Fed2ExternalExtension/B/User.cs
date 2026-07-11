namespace HotChocolate.Fusion.Suites.Fed2ExternalExtension.B;

/// <summary>
/// The <c>User</c> entity as projected by the <c>b</c> subgraph
/// (<c>@key(fields: "id")</c>). Owns <c>id</c>, <c>name</c> (shareable),
/// and <c>nickname</c>.
/// </summary>
public sealed record User(string Id, string Name, string? Nickname);
