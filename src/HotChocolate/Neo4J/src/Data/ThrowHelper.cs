using System;

namespace HotChocolate.Data.Neo4J
{
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
                    .SetCode(ErrorCodes.Data.NoPagingationProviderFound)
                    .Build());
        }
    }
}
