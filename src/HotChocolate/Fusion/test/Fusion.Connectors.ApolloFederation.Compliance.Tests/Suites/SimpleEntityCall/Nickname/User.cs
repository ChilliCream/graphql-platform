namespace HotChocolate.Fusion.Suites.SimpleEntityCall.Nickname;

/// <summary>
/// The <c>User</c> entity as projected by the <c>nickname</c> subgraph
/// (<c>@key(fields: "email")</c>, <c>email</c> is external).
/// </summary>
public sealed record User(string Email, string Nickname);
