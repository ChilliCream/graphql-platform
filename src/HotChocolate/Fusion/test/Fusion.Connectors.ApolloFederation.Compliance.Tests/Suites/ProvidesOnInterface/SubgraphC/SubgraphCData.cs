namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphC;

/// <summary>
/// Seed data for the <c>subgraph-c</c> subgraph. This is the "owning"
/// subgraph for <c>Dog.name</c>, <c>Cat.name</c>, <c>Dog.age</c>, and
/// <c>Cat.age</c>.
/// </summary>
internal static class SubgraphCData
{
    /// <summary>
    /// Dog entities indexed by their <c>id</c>.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Dog> DogsById =
        new Dictionary<string, Dog>(StringComparer.Ordinal)
        {
            ["a1"] = new Dog { Id = "a1", Name = "Fido", Age = 3 }
        };

    /// <summary>
    /// Cat entities indexed by their <c>id</c>.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Cat> CatsById =
        new Dictionary<string, Cat>(StringComparer.Ordinal)
        {
            ["a2"] = new Cat { Id = "a2", Name = "Whiskers", Age = 6 }
        };

    /// <summary>
    /// Book entities indexed by their <c>id</c>, with animals resolved.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, Book> BooksById =
        new Dictionary<string, Book>(StringComparer.Ordinal)
        {
            ["m1"] = new Book
            {
                Id = "m1",
                Animals =
                [
                    DogsById["a1"],
                    CatsById["a2"]
                ]
            }
        };
}
