namespace HotChocolate.Fusion.Suites.SimpleEntityCall.Email;

/// <summary>
/// The <c>User</c> entity as projected by the <c>email</c> subgraph
/// (<c>@key(fields: "id")</c>).
/// </summary>
public sealed record User(string Id, string Email);
