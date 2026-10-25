namespace HotChocolate.Fusion.Suites.OverrideWithRequires.A;

/// <summary>
/// The <c>User</c> entity as projected by subgraph <c>a</c>: <c>id</c> is
/// owned, <c>name</c> is external (populated by the federation external setter
/// when the gateway threads the <c>@requires</c> dependency), and
/// <c>aName</c> is computed from <c>name</c>.
/// </summary>
public sealed class User
{
    public string? Id { get; init; }

    public string? Name { get; init; }
}
