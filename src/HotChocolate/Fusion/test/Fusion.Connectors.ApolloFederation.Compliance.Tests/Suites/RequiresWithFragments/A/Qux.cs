namespace HotChocolate.Fusion.Suites.RequiresWithFragments.A;

/// <summary>
/// The <c>Qux</c> type implementing <c>Foo</c> and <c>Bar</c> in subgraph <c>a</c>.
/// </summary>
public sealed class Qux : IBar
{
    public string Foo { get; init; } = default!;
    public string Bar { get; init; } = default!;
    public string QuxValue { get; init; } = default!;
}
