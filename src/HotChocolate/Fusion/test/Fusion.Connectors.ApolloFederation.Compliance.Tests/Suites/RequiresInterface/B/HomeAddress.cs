namespace HotChocolate.Fusion.Suites.RequiresInterface.B;

/// <summary>
/// The <c>HomeAddress</c> entity in subgraph <c>b</c>.
/// </summary>
public sealed class HomeAddress : IAddress
{
    public string Id { get; init; } = default!;
    public string? City { get; init; }
}
