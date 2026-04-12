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
            typeof(SchemaSearchResult))
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
                    resolver: Resolvers.PathsToRootAsync),
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
            => context.Parent<SchemaSearchResult>().Cursor;

        public static object Coordinate(IResolverContext context)
            => context.Parent<SchemaSearchResult>().Coordinate.ToString();

        public static object Definition(IResolverContext context)
        {
            var result = context.Parent<SchemaSearchResult>();

            if (!context.Schema.TryGetMember(result.Coordinate, out var member))
            {
                throw new InvalidOperationException(
                    $"Failed to resolve schema coordinate '{result.Coordinate}'.");
            }

            return member;
        }

        public static async ValueTask<object?> PathsToRootAsync(IResolverContext context)
        {
            var result = context.Parent<SchemaSearchResult>();
            var provider = context.Schema.Services.GetRequiredService<ISchemaSearchProvider>();
            var paths = await provider.GetPathsToRootAsync(
                result.Coordinate,
                maxPaths: 5,
                context.RequestAborted)
                .ConfigureAwait(false);

            var pathsToRoot = new IReadOnlyList<string>[paths.Count];

            for (var i = 0; i < paths.Count; i++)
            {
                pathsToRoot[i] = paths[i].ToStringArray();
            }

            return pathsToRoot;
        }

        public static object? Score(IResolverContext context)
            => context.Parent<SchemaSearchResult>().Score;
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
