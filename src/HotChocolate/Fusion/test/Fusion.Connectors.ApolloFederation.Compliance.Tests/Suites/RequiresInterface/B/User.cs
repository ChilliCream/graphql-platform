namespace HotChocolate.Fusion.Suites.RequiresInterface.B;

/// <summary>
/// The <c>User</c> entity in subgraph <c>b</c>. Stores the address id
/// as a string reference, resolved to an <c>Address</c> at query time.
/// </summary>
public sealed class User
{
    public string Id { get; init; } = default!;
    public string? Name { get; init; }
    public string? AddressId { get; init; }
}
