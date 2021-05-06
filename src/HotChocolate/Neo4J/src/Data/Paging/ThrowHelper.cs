using System;

namespace HotChocolate.Data.Neo4J.Paging
{
    internal static class ThrowHelper
    {
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
