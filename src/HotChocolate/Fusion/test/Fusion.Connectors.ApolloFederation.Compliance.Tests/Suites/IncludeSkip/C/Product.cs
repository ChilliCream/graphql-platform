namespace HotChocolate.Fusion.Suites.IncludeSkip.C;

/// <summary>
/// The <c>Product</c> entity in the <c>c</c> subgraph
/// (<c>@key(fields: "id")</c>). Owns <c>include</c>, <c>skip</c>,
/// <c>neverCalledInclude</c>, <c>neverCalledSkip</c>; <c>isExpensive</c>
/// is external.
/// </summary>
public sealed class Product
{
    public string Id { get; set; } = default!;

    public bool? IsExpensive { get; set; }
}
