namespace HotChocolate.Fusion.Suites.RequiresInterface.A;

/// <summary>
/// The <c>User</c> entity in subgraph <c>a</c>. The <c>Address</c> property
/// holds the resolved address object when populated via @requires.
/// </summary>
public sealed class User
{
    public string Id { get; init; } = default!;
    public string? Name { get; init; }
    public IAddress? Address { get; set; }
}
