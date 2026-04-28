namespace HotChocolate.Fusion.Suites.RequiresWithFragments.B;

/// <summary>
/// The <c>Entity</c> type in subgraph <c>b</c>. The <c>Data</c> property
/// holds the resolved Foo object when populated via @requires.
/// </summary>
public sealed class Entity
{
    public string Id { get; init; } = default!;
    public IFoo? Data { get; set; }
}
