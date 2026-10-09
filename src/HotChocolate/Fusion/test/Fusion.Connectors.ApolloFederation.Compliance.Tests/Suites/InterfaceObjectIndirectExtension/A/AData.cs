namespace HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.A;

/// <summary>
/// Seed data for the <c>a</c> subgraph, transcribed from
/// <c>graphql-hive/federation-gateway-audit/src/test-suites/interface-object-indirect-extension/media.subgraph.ts</c>.
/// </summary>
internal static class AData
{
    public static IMedia DefaultMedia()
        => new Video { Id = "1", Title = "title for 1", Duration = 100 };

    public static Video VideoById(string id)
        => new Video
        {
            Id = id,
            Title = $"title for {id}",
            Duration = 100 * int.Parse(id)
        };

    public static Article ArticleById(string id)
        => new Article
        {
            Id = id,
            Title = $"title for {id}",
            WordCount = 900 * int.Parse(id)
        };
}
