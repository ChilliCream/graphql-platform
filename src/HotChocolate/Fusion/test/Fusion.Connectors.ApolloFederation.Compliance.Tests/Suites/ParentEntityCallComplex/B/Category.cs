namespace HotChocolate.Fusion.Suites.ParentEntityCallComplex.B;

/// <summary>
/// The <c>Category</c> value type as projected by the <c>b</c> subgraph
/// (<c>type Category { id: ID @shareable }</c>). Not an entity in this
/// subgraph: it has no <c>@key</c> here and is only ever produced inline
/// by the parent <c>Product.category</c> field. The shareable <c>id</c>
/// flows through the supergraph and acts as the entity key for the
/// <c>c</c> subgraph's <c>Category @key("id")</c> lookup.
/// </summary>
public sealed class Category
{
    public string? Id { get; init; }
}
