namespace HotChocolate.Fusion.Suites.KeysMashup.B;

/// <summary>
/// The <c>A</c> entity in the <c>b</c> subgraph (multiple keys; only
/// <c>id compositeId { two three }</c> is resolvable).
/// </summary>
public sealed class A
{
    public string Id { get; set; } = default!;

    public string PId { get; set; } = default!;

    public CompositeID CompositeId { get; set; } = default!;

    public string? Name { get; set; }
}
