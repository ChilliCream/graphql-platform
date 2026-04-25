namespace HotChocolate.Fusion.Suites.Mutations.B;

/// <summary>
/// The <c>Product</c> projection used by the <c>b</c> subgraph. The
/// <c>Price</c> field is marked external in the SDL: it travels in via the
/// entity reference so <c>@requires</c> can use it. <see cref="Price"/>
/// uses a public setter so the federation runtime can populate it from the
/// representation.
/// </summary>
public sealed class Product
{
    public string Id { get; set; } = default!;

    public double? Price { get; set; }

    public bool IsAvailable { get; set; }
}
