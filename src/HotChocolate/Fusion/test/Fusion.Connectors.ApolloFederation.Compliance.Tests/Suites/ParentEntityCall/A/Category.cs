namespace HotChocolate.Fusion.Suites.ParentEntityCall.A;

/// <summary>
/// The <c>Category</c> entity as projected by the <c>a</c> subgraph
/// (<c>type Category @key(fields: "id") { id, name }</c>). The
/// <c>name</c> field is shareable across the audit's <c>a</c> and <c>b</c>
/// subgraphs.
/// </summary>
public sealed class Category
{
    public string Id { get; init; } = default!;

    public string Name { get; init; } = default!;
}
