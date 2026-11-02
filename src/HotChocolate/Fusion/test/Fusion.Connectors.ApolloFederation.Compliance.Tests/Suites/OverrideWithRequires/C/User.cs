namespace HotChocolate.Fusion.Suites.OverrideWithRequires.C;

/// <summary>
/// The <c>User</c> entity as projected by subgraph <c>c</c>: <c>id</c> is
/// owned, <c>name</c> is external, and <c>cName</c> is computed from
/// <c>name</c>.
/// </summary>
public sealed class User
{
    public string? Id { get; init; }

    public string? Name { get; init; }
}
