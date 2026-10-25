namespace HotChocolate.Fusion.Suites.ParentEntityCall.B;

/// <summary>
/// The <c>Product</c> entity as projected by the <c>b</c> subgraph
/// (<c>type Product @key(fields: "id pid") { id, pid, category }</c>). The
/// subgraph contributes the same shareable <c>category</c> field as
/// <c>a</c> via the compound <c>id pid</c> entity call.
/// </summary>
public sealed class Product
{
    public string Id { get; init; } = default!;

    public string Pid { get; init; } = default!;

    public Category? Category { get; init; }
}
