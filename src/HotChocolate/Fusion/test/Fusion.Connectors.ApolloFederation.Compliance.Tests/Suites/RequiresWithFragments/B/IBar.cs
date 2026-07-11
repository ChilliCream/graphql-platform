namespace HotChocolate.Fusion.Suites.RequiresWithFragments.B;

/// <summary>
/// Marker interface for the <c>Bar</c> interface hierarchy in subgraph <c>b</c>.
/// <c>Bar</c> implements <c>Foo</c>.
/// </summary>
public interface IBar : IFoo
{
    string Bar { get; }
}
