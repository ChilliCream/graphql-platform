namespace HotChocolate.Fusion.Suites.RequiresWithFragments.A;

/// <summary>
/// Marker interface for the <c>Bar</c> interface hierarchy in subgraph <c>a</c>.
/// <c>Bar</c> implements <c>Foo</c>.
/// </summary>
public interface IBar : IFoo
{
    string Bar { get; }
}
