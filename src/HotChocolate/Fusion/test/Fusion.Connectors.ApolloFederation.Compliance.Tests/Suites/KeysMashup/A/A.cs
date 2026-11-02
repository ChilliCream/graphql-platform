namespace HotChocolate.Fusion.Suites.KeysMashup.A;

/// <summary>
/// The <c>A</c> entity in the <c>a</c> subgraph (multiple keys, only
/// <c>id</c> is resolvable).
/// </summary>
public sealed class A
{
    public string Id { get; init; } = default!;

    public string PId { get; init; } = default!;

    public CompositeID CompositeId { get; init; } = default!;

    public string Name { get; init; } = default!;
}
