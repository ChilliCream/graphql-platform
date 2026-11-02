namespace HotChocolate.Fusion.Suites.SimpleInaccessible.Age;

/// <summary>
/// The <c>User</c> entity as projected by the <c>age</c> subgraph.
/// </summary>
public sealed class User
{
    public string? Id { get; init; }

    public int? Age { get; init; }
}
