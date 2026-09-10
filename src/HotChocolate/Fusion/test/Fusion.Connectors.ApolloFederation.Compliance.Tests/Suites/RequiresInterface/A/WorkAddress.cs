namespace HotChocolate.Fusion.Suites.RequiresInterface.A;

/// <summary>
/// The <c>WorkAddress</c> entity in subgraph <c>a</c>.
/// </summary>
public sealed class WorkAddress : IAddress
{
    public string Id { get; init; } = default!;
    public string? City { get; init; }
    public string? Country { get; init; }
}
