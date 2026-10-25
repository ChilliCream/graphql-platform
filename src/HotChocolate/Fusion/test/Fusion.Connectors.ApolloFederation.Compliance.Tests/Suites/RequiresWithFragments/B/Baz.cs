namespace HotChocolate.Fusion.Suites.RequiresWithFragments.B;

/// <summary>
/// The <c>Baz</c> type in subgraph <c>b</c>. Marked inaccessible.
/// </summary>
public sealed class Baz : IBar
{
    public string Foo { get; init; } = default!;
    public string Bar { get; init; } = default!;
    public string BazValue { get; init; } = default!;
}
