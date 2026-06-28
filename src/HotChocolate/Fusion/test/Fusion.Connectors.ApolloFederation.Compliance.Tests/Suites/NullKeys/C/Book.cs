namespace HotChocolate.Fusion.Suites.NullKeys.C;

/// <summary>
/// The <c>Book</c> entity as projected by the <c>c</c> subgraph
/// (<c>@key(fields: "id")</c>). Owns the <c>author</c> link.
/// </summary>
public sealed class Book
{
    public string Id { get; init; } = default!;

    public Author? Author { get; init; }
}
