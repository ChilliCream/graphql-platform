namespace HotChocolate.Fusion.Suites.ParentEntityCall.B;

/// <summary>
/// The <c>Category</c> entity as projected by the <c>b</c> subgraph
/// (<c>type Category @key(fields: "id") { id, name }</c>). Both
/// <c>id</c> and <c>name</c> are shareable with subgraph <c>a</c>.
/// </summary>
public sealed class Category
{
    public string Id { get; init; } = default!;

    public string Name { get; init; } = default!;
}
