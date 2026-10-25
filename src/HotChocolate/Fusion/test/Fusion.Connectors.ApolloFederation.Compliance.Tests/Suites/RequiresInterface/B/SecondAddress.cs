namespace HotChocolate.Fusion.Suites.RequiresInterface.B;

/// <summary>
/// The <c>SecondAddress</c> entity in subgraph <c>b</c>.
/// </summary>
public sealed class SecondAddress : IAddress
{
    public string Id { get; init; } = default!;
    public string? City { get; init; }
}
