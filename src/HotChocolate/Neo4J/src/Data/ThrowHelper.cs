using System.Globalization;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Neo4J.Filtering;

namespace HotChocolate.Data.Neo4J;

internal static class ThrowHelper
{
    public static GraphQLException ValueMapper_CypherValueIsNotAListAndCannotBeMapped(
        Type underlyingType)
    {
        return new(
            ErrorBuilder.New()
                .SetMessage(
                    Neo4JResources.ValueMapper_CypherValueIsNotAListAndCannotBeMapped,
                    underlyingType.FullName ?? underlyingType.Name)
                .Build());
    }

    public static GraphQLException ValueMapper_CypherValueIsAListAndCannotBeMapped(
        Type underlyingType)
    {
        return new(
            ErrorBuilder.New()
                .SetMessage(
                    Neo4JResources.ValueMapper_CypherValueIsAListAndCannotBeMapped,
                    underlyingType.FullName ?? underlyingType.Name)
                .Build());
    }

    public static GraphQLException PagingTypeNotSupported(Type type)
    {
        return new GraphQLException(
            ErrorBuilder.New()
                .SetMessage(
                    "The provided source is not supported for Neo4j paging",
                    type.FullName ?? type.Name)
                .SetCode(ErrorCodes.Data.NoPaginationProviderFound)
                .Build());
    }

    public static InvalidOperationException Filtering_Neo4JFilterCombinator_QueueEmpty(
        Neo4JFilterCombinator combinator) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            Neo4JResources.Filtering_Neo4JCombinator_QueueEmpty,
            combinator.GetType()));

    public static InvalidOperationException Filtering_Neo4JFilterCombinator_InvalidCombinator(
        Neo4JFilterCombinator combinator,
        FilterCombinator operation) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            Neo4JResources.Filtering_Neo4JCombinator_InvalidCombinator,
            combinator.GetType(),
            operation.ToString()));

}
