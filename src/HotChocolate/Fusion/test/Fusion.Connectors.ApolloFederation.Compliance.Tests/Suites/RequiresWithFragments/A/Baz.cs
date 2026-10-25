namespace HotChocolate.Fusion.Suites.RequiresWithFragments.A;

/// <summary>
/// The <c>Baz</c> type implementing <c>Foo</c> and <c>Bar</c> in subgraph <c>a</c>.
/// </summary>
public sealed class Baz : IBar
{
    public string Foo { get; init; } = default!;
    public string Bar { get; init; } = default!;
    public string BazValue { get; init; } = default!;
}
