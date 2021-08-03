using System;

namespace HotChocolate.Data.SqlKata
{
    internal static class ThrowHelper
    {
        public static GraphQLException PagingTypeNotSupported(Type type)
        {
            return new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(
                        SqlKataResources.Paging_SourceIsNotSupported,
                        type.FullName ?? type.Name)
                    .SetCode(ErrorCodes.Data.NoPagingationProviderFound)
                    .Build());
        }
    }
}
