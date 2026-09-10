namespace HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.B;

/// <summary>
/// Seed data for the <c>b</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/interface-object-indirect-extension/author.subgraph.ts</c>.
/// </summary>
internal static class BData
{
    public const string AuthorName = "John Doe";

    public static Author DefaultAuthor()
        => new Author { Id = "1", Name = "name for 1" };

    public static Author AuthorById(string id)
        => new Author { Id = id, Name = $"name for {id}" };
}
