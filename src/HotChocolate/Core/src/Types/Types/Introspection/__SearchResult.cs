#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Types.Descriptors.TypeReference;

namespace HotChocolate.Types.Introspection;

[Introspection]
// ReSharper disable once InconsistentNaming
internal sealed class __SearchResult : ObjectType
{
    protected override ObjectTypeConfiguration CreateConfiguration(ITypeDiscoveryContext context)
    {
        var nonNullStringType = Parse($"{ScalarNames.String}!");
        var floatType = Create(ScalarNames.Float);
        var nonNullSchemaDefinitionType = Parse($"{nameof(__SchemaDefinition)}!");
        var nonNullStringListListType = Parse($"[[{ScalarNames.String}!]!]!");

        return new ObjectTypeConfiguration(
            Names.__SearchResult,
            description: "A search result representing a matched schema element.",
            typeof(SearchResultInfo))
        {
            Fields =
            {
                new(Names.Cursor,
                    "An opaque cursor for pagination.",
                    nonNullStringType,
                    pureResolver: Resolvers.Cursor),
                new(Names.Coordinate,
                    "The schema coordinate of the matched element.",
                    nonNullStringType,
                    pureResolver: Resolvers.Coordinate),
                new(Names.Definition,
                    "The matched schema definition.",
                    nonNullSchemaDefinitionType,
                    pureResolver: Resolvers.Definition),
                new(Names.PathsToRoot,
                    "Paths from this element to a root type, each as a list of schema coordinates.",
                    nonNullStringListListType,
                    pureResolver: Resolvers.PathsToRoot),
                new(Names.Score,
                    "The relevance score of the match, or null if scoring is not supported.",
                    floatType,
                    pureResolver: Resolvers.Score)
            }
        };
    }

    private static class Resolvers
    {
        public static object Cursor(IResolverContext context)
            => context.Parent<SearchResultInfo>().Cursor;

        public static object Coordinate(IResolverContext context)
            => context.Parent<SearchResultInfo>().Coordinate.ToString();

        public static object Definition(IResolverContext context)
            => context.Parent<SearchResultInfo>().Definition;

        public static object PathsToRoot(IResolverContext context)
            => context.Parent<SearchResultInfo>().PathsToRoot;

        public static object? Score(IResolverContext context)
            => context.Parent<SearchResultInfo>().Score;
    }

    public static class Names
    {
        // ReSharper disable once InconsistentNaming
        public const string __SearchResult = "__SearchResult";
        public const string Cursor = "cursor";
        public const string Coordinate = "coordinate";
        public const string Definition = "definition";
        public const string PathsToRoot = "pathsToRoot";
        public const string Score = "score";
    }
}
#pragma warning restore IDE1006 // Naming Styles
