namespace HotChocolate.Fusion.Suites.RequiresInterface.A;

/// <summary>
/// The <c>HomeAddress</c> entity in subgraph <c>a</c>.
/// </summary>
public sealed class HomeAddress : IAddress
{
    public string Id { get; init; } = default!;
    public string? City { get; init; }
    public string? Country { get; init; }
}
