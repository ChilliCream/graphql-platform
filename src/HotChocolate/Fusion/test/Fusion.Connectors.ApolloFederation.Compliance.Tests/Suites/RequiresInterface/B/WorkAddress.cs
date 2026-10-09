namespace HotChocolate.Fusion.Suites.RequiresInterface.B;

/// <summary>
/// The <c>WorkAddress</c> entity in subgraph <c>b</c>.
/// </summary>
public sealed class WorkAddress : IAddress
{
    public string Id { get; init; } = default!;
    public string? City { get; init; }
}
