namespace HotChocolate.Fusion.Suites.RequiresWithFragments.B;

/// <summary>
/// The <c>Qux</c> type in subgraph <c>b</c>.
/// </summary>
public sealed class Qux : IBar
{
    public string Foo { get; init; } = default!;
    public string Bar { get; init; } = default!;
    public string QuxValue { get; init; } = default!;
}
