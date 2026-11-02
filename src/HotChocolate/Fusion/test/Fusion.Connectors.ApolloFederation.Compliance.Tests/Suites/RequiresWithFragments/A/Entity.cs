namespace HotChocolate.Fusion.Suites.RequiresWithFragments.A;

/// <summary>
/// The <c>Entity</c> type in subgraph <c>a</c> with a <c>data: Foo</c> field.
/// Stores a reference to the data id which resolves to Baz or Qux.
/// </summary>
public sealed class Entity
{
    public string Id { get; init; } = default!;
    public string? DataId { get; init; }
}
