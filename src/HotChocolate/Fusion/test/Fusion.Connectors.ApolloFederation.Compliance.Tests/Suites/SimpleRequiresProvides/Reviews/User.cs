namespace HotChocolate.Fusion.Suites.SimpleRequiresProvides.Reviews;

/// <summary>
/// The <c>User</c> entity as projected by the <c>reviews</c> subgraph
/// (<c>@key(fields: "id")</c>). <c>username</c> is external and may be
/// populated when the gateway dispatches an entity reference that already
/// carries the field through <c>@provides(fields: "username")</c>.
/// </summary>
public sealed class User
{
    public string Id { get; set; } = default!;

    public string? Username { get; set; }
}
